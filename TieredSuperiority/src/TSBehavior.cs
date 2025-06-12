using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

using HarmonyLib;
using Vintagestory.GameContent;

namespace TieredSuperiority.src
{
    [HarmonyPatch]
    public class TSBehavior : CollectibleBehavior
    {
        protected int initDurability;
        protected long timeLastCalled = -1;
        // verify item hasn't changed
        protected int initItemId;
        protected string initItemCode;


        public TSBehavior(CollectibleObject collObj) : base(collObj) { }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CollectibleObject), "OnBlockBrokenWith")]
        public static void PrefixOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            if (__instance.GetCollectibleBehavior(typeof(TSBehavior), false) is TSBehavior behavior)
            {
                behavior.initDurability = __instance.GetRemainingDurability(itemslot.Itemstack);
                behavior.initItemId = itemslot.Itemstack.Id;
                behavior.initItemCode = itemslot.Itemstack.Item.Code.ToString();
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CollectibleObject), "OnBlockBrokenWith")]
        public static void PostfixOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            if (!ValidateItemStack(itemslot, __instance))
                return;

            if (blockSel == null || blockSel.Block == null)
                return;

            if (__instance.GetCollectibleBehavior(typeof(TSBehavior), false) is not TSBehavior behavior)
                return;

            if (!ValidateItemChange(itemslot, behavior))
                return;

            if (!ValidateRateLimit(behavior))
                return;

            int durabilityDiff = behavior.initDurability - __instance.GetRemainingDurability(itemslot.Itemstack);
            int selectionTier = blockSel.Block.RequiredMiningTier;

            if (TieredSuperiorityMain.debugMode)
            {
                TieredSuperiorityMain.sapi.Logger.Notification("durabilityDiff: " + behavior.initDurability + " - " + __instance.GetRemainingDurability(itemslot.Itemstack) + " = " + durabilityDiff);
            }

            if (durabilityDiff > 0)
                TieredSuperiorityMain.RefundDurability(__instance, byEntity, itemslot, selectionTier, durabilityDiff);
        }


        // Patches for items that don't use base.OnBlockBrokenWith, mostly ones that can multibreak


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemAxe), "OnBlockBrokenWith")]
        public static void PrefixAxeOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot)
        {
            PrefixOnBlockBrokenWith(__instance, byEntity, itemslot);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemAxe), "OnBlockBrokenWith")]
        public static void PostfixAxeOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
        {
            PostfixOnBlockBrokenWith(__instance, byEntity, itemslot, blockSel);
        }


        // Note: ItemScythe extends ItemShears, so only need to patch shears
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemShears), "OnBlockBrokenWith")]
        public static void PrefixShearsOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, ref object __state)
        {
            __state = ((EntityPlayer)byEntity).BlockSelection;
            PrefixOnBlockBrokenWith(__instance, byEntity, itemslot);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemShears), "OnBlockBrokenWith")]
        public static void PostfixShearsOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, object __state)
        {
            PostfixOnBlockBrokenWith(__instance, byEntity, itemslot, __state as BlockSelection);
        }


        // Common validation methods
        protected static bool ValidateItemStack(ItemSlot itemslot, CollectibleObject collectible)
        {
            if (itemslot.Itemstack == null)
            {
                if (TieredSuperiorityMain.debugMode)
                {
                    TieredSuperiorityMain.sapi.Logger.Notification("Item broke before calculation");
                }

                return false;
            }
            
            return true;
        }

        protected static bool ValidateItemChange(ItemSlot itemslot, TSBehavior behavior)
        {
            if (itemslot.Itemstack.Id != behavior.initItemId || 
                itemslot.Itemstack.Item.Code.ToString() != behavior.initItemCode)
            {
                if (TieredSuperiorityMain.debugMode)
                {
                    TieredSuperiorityMain.sapi.Logger.Notification("Item changed between prefix and postfix, skipping refund");
                }

                return false;
            }
            
            return true;
        }

        protected static bool ValidateRateLimit(TSBehavior behavior)
        {
            if (TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds - behavior.timeLastCalled < 0.5)
            {
                behavior.timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;
                return false;
            }

            behavior.timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;
            return true;
        }
    }
}
