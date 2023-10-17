using Vintagestory.API.Common;
using Vintagestory.GameContent;

using HarmonyLib;
using System.Linq;

namespace TieredSuperiority.src
{
    [HarmonyPatch]
    public class TSBehaviorHammer : CollectibleBehavior
    {
        public long timeLastCalled = -1;


        public TSBehaviorHammer(CollectibleObject collObj) : base(collObj) { }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemHammer), "OnHeldAttackStop")]
        public static void PostfixHammerOnHeldAttackStop(ItemHammer __instance, float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            if (blockSel == null || secondsPassed < 0.4f) return;

            if (slot.Itemstack == null)
            {
                // TieredSuperiorityMain.sapi.Logger.Notification("item broke before calculation");
                return;
            }


            TSBehaviorHammer behavior = __instance.GetCollectibleBehavior(typeof(TSBehaviorHammer), false) as TSBehaviorHammer;

            if (behavior == null)
                return;

            if (TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds - behavior.timeLastCalled < 0.5)
            {
                behavior.timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;
                return;
            }
            behavior.timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is not BlockAnvil) return;
            BlockEntityAnvil bea = be as BlockEntityAnvil;
            if (bea == null) return;

            int workitemtier = TieredSuperiorityMain.ResolveTier(bea.WorkItemStack.Item.Variant["metal"]);

            TieredSuperiorityMain.RefundDurability(__instance, byEntity, slot, workitemtier, 1);
        }
    }
}
