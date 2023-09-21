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
        public TSBehaviorHammer(CollectibleObject collObj) : base(collObj) { }


        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling); // hammer loses durability here

            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            if (TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds - timeLastCalled < 0.5)
            {
                timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;
                return;
            }
            timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;

            // TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("calling tsbehaviorhammer onheldattack stop", EnumChatType.Notification);

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSelection.Position);
            if (!(byEntity.World.BlockAccessor.GetBlock(blockSelection.Position) is BlockAnvil)) return;
            BlockEntityAnvil bea = be as BlockEntityAnvil;
            if (bea == null) return;

            int workitemtier = TieredSuperiorityMain.ResolveTier(bea.WorkItemStack.Item.Variant["metal"]);

            TieredSuperiorityMain.RefundDurability(collObj, byEntity, slot, workitemtier, 1);
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
