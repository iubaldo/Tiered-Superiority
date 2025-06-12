using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using HarmonyLib;

namespace TieredSuperiority.src
{
    [HarmonyPatch]
    public class TSBehaviorHammer : TSBehavior
    {
        public TSBehaviorHammer(CollectibleObject collObj) : base(collObj) { }
        

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemHammer), "OnHeldAttackStart")]
        public static void PrefixOnHeldAttackStart(ItemHammer __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            if (__instance.GetCollectibleBehavior(typeof(TSBehaviorHammer), false) is TSBehaviorHammer behavior)
            {
                behavior.initDurability = __instance.GetRemainingDurability(slot.Itemstack);
                behavior.initItemId = slot.Itemstack.Id;
                behavior.initItemCode = slot.Itemstack.Item.Code.ToString();
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemHammer), "strikeAnvil")]
        public static void PostfixStrikeAnvil(EntityAgent byEntity, ItemSlot slot)
        {
            if (byEntity.World.Api.Side == EnumAppSide.Client)
                return;

            if (!ValidateItemStack(slot, slot.Itemstack.Collectible))
                return;

            // Get the block selection from the player
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (byPlayer == null) 
                return;

            BlockSelection blockSel = byPlayer.CurrentBlockSelection;
            if (blockSel == null) 
                return;

            // Get the anvil block entity
            BlockEntityAnvil anvil = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityAnvil;
            if (anvil == null) 
                return;

            if (slot.Itemstack.Collectible.GetCollectibleBehavior(typeof(TSBehaviorHammer), false) is not TSBehaviorHammer behavior)
                return;

            if (!ValidateItemChange(slot, behavior))
                return;

            if (!ValidateRateLimit(behavior))
                return;

            int durabilityDiff = behavior.initDurability - slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
            int workItemTier = GetWorkItemTier(anvil, blockSel);

            if (TieredSuperiorityMain.debugMode)
            {
                TieredSuperiorityMain.sapi.Logger.Notification("durabilityDiff: " + behavior.initDurability + " - " + slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) + " = " + durabilityDiff);
            }

            if (durabilityDiff > 0)
                TieredSuperiorityMain.RefundDurability(slot.Itemstack.Collectible, byEntity, slot, workItemTier, durabilityDiff);
        }


        private static int GetWorkItemTier(BlockEntityAnvil anvil, BlockSelection blockSel)
        {
            if (anvil == null || blockSel == null)
            {
                if (TieredSuperiorityMain.debugMode)
                    TieredSuperiorityMain.sapi.Logger.Notification("GetWorkItemTier: anvil or blockSel is null");
                return 0;
            }

            // Get the voxel material at the hit position
            Block block = anvil.Api.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block == null)
            {
                if (TieredSuperiorityMain.debugMode)
                    TieredSuperiorityMain.sapi.Logger.Notification("GetWorkItemTier: block is null");
                return 0;
            }

            Cuboidf[] selectionBoxes = block.GetSelectionBoxes(anvil.Api.World.BlockAccessor, blockSel.Position);
            if (selectionBoxes == null || selectionBoxes.Length == 0 || blockSel.SelectionBoxIndex >= selectionBoxes.Length)
            {
                if (TieredSuperiorityMain.debugMode)
                    TieredSuperiorityMain.sapi.Logger.Notification($"GetWorkItemTier: invalid selection boxes (null: {selectionBoxes == null}, length: {selectionBoxes?.Length ?? 0}, index: {blockSel.SelectionBoxIndex})");
                return 0;
            }

            Cuboidf box = selectionBoxes[blockSel.SelectionBoxIndex];
            Vec3i voxelPos = new(
                (int)(16 * box.X1),
                (int)(16 * box.Y1) - 10,
                (int)(16 * box.Z1)
            );

            // Ensure voxel position is within bounds
            voxelPos.X = Math.Clamp(voxelPos.X, 0, 15);
            voxelPos.Y = Math.Clamp(voxelPos.Y, 0, 5);
            voxelPos.Z = Math.Clamp(voxelPos.Z, 0, 15);

            byte voxelMaterial = anvil.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z];

            if (TieredSuperiorityMain.debugMode)
            {
                TieredSuperiorityMain.sapi.Logger.Notification($"GetWorkItemTier: voxelPos=({voxelPos.X}, {voxelPos.Y}, {voxelPos.Z}), material={voxelMaterial}");
            }

            // Get the work item tier
            if (anvil.WorkItemStack != null && anvil.WorkItemStack.Collectible != null)
            {
                int tier;
                if (anvil.WorkItemStack.Collectible is ItemIronBloom) // For iron blooms, check if the voxel is slag
                {
                    tier = (voxelMaterial == (byte)EnumVoxelMaterial.Slag) ? 0 : 4;
                    if (TieredSuperiorityMain.debugMode)
                        TieredSuperiorityMain.sapi.Logger.Notification($"GetWorkItemTier: iron bloom with voxel material {voxelMaterial}, tier={tier}");
                }
                else
                {
                    tier = anvil.WorkItemStack.Collectible.ToolTier;
                    if (TieredSuperiorityMain.debugMode)
                        TieredSuperiorityMain.sapi.Logger.Notification($"GetWorkItemTier: work item {anvil.WorkItemStack.Collectible.Code}, tier={tier}");
                }
                return tier;
            }

            if (TieredSuperiorityMain.debugMode)
                TieredSuperiorityMain.sapi.Logger.Notification("GetWorkItemTier: no work item, returning tier 0");

            return 0;
        }
    }
}
