using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace TieredSuperiority.src
{
    internal class TSBehavior : CollectibleBehavior
    {
        Random rand = new();

        public TSBehavior(CollectibleObject collObj) : base(collObj) { }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier, ref EnumHandling bhHandling)
        {

            int blockTier = byEntity.World.BlockAccessor.GetBlock(blockSel.Position).RequiredMiningTier;

            bool toReturn = base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling); 

            if (world.Api.Side == EnumAppSide.Client)
                return toReturn;

            if (collObj.DamagedBy != null && collObj.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
                RefundDurability(byEntity, itemslot, blockTier);

            return toReturn;
        }


        public void RefundDurability(Entity byEntity, ItemSlot itemslot, int selectionTier)
        {
            if (byEntity.World.Api.Side == EnumAppSide.Client)
                return;

            int refundChance = TieredSuperiorityMain.config.chancePerTier * (collObj.ToolTier - selectionTier); // by default, 10% per tier difference
            
            // TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("Refund Chance: " + refundChance +" x " + "(" + collObj.ToolTier + " - " + selectionTier + ") = " + refundChance + "%", EnumChatType.Notification);

            if (rand.Next(100) < refundChance)
            {
                collObj.Durability++;
                // TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("Refunded tool durability.", EnumChatType.Notification);

                if (TieredSuperiorityMain.config.playSound)
                    TieredSuperiorityMain.sSoundChannel.SendPacket(new SoundMessage() { shouldPlay = true }, (IServerPlayer)((EntityPlayer)byEntity).Player);

                itemslot.MarkDirty();
            }
        }
    }
}
