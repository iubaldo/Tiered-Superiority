using Vintagestory.API.Common;
using Vintagestory.GameContent;

using HarmonyLib;
using System.Linq;

namespace TieredSuperiority.src
{
    [HarmonyPatch(typeof(ItemHammer))]
    [HarmonyPatch("OnHeldAttackStop")]
    public class TSBehaviorHammer : TSBehavior
    {
        long timeSinceLastCall = -1;


        public TSBehaviorHammer(CollectibleObject collObj) : base(collObj) { }


        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling); // hammer loses durability here

            if (TieredSuperiorityMain.sapi.Side == EnumAppSide.Client)
                return;

            if (TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds - timeSinceLastCall < 0.5)
            {
                timeSinceLastCall = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;
                return;
            }
            timeSinceLastCall = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;

            // TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("calling tsbehaviorhammer onheldattack stop", EnumChatType.Notification);

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSelection.Position);
            if (!(byEntity.World.BlockAccessor.GetBlock(blockSelection.Position) is BlockAnvil)) return;
            BlockEntityAnvil bea = be as BlockEntityAnvil;
            if (bea == null) return;

            // more-or-less copied from worldproperties\block\toolmetal
            // TODO: find a way to read from that file directly
            int workitemtier;
            switch (bea.WorkItemStack.Item.Variant["metal"])
            {
                case "lead": workitemtier = 0; break;
                case "molybdochalkos": workitemtier = 0; break;
                case "bismuth": workitemtier = 1; break;
                case "copper": workitemtier = 1; break;
                case "silver": workitemtier = 1; break;
                case "gold": workitemtier = 1; break;
                case "bismuthbronze": workitemtier = 2; break;
                case "blackbronze": workitemtier = 2; break;
                case "brass": workitemtier = 2; break;
                case "tinbronze": workitemtier = 2; break;
                case "iron": workitemtier = 3; break;
                case "meteoriciron": workitemtier = 3; break;
                case "steel": workitemtier = 4; break;
            
                // unused tool metals, might become relevant in the future?
                // case "tin": workitemtier = 1; break;
                // case "zinc": workitemtier = 1; break;
                // case "chromium": workitemtier = 5; break;
                // case "stainlesssteel": workitemtier = 5; break;
                // case "platinum": workitemtier = 6; break;
                // case "titanium": workitemtier = 6; break;
                // case "rhodium": workitemtier = 7; break;
                // case "uranium": workitemtier = 8; break;

                default: workitemtier = 0; TieredSuperiorityMain.sapi.Logger.Notification("[TieredSuperiority] Could not find match for metal variant, defaulting to workItemTier = 0."); break;
            }

            RefundDurability(byEntity, slot, workitemtier);
        }


        public static void Postfix(ItemHammer __instance, float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || secondsPassed < 0.4f) return;

            EnumHandHandling handling = EnumHandHandling.Handled;
            TSBehaviorHammer behavior = __instance.CollectibleBehaviors.OfType<TSBehaviorHammer>().DefaultIfEmpty(null).FirstOrDefault();
            if (behavior != null)
                behavior.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSel, entitySel, ref handling);
        }
    }
}
