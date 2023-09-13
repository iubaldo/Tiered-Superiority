using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("calling refunddurability", EnumChatType.Notification);

            bool playOnce = true;
            int refundChance = 10 * (collObj.ToolTier - selectionTier); // 10% per tier difference
            
            TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("Refund Chance: 10 x " + "(" + collObj.ToolTier + " - " + selectionTier + ") = " + refundChance + "%", EnumChatType.Notification);

            if (rand.Next(100) < refundChance)
            {
                collObj.Durability++;
                TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("Refunded tool durability.", EnumChatType.Notification);

                if (playOnce)
                {
                    TieredSuperiorityMain.sSoundChannel.SendPacket(new SoundMessage() { shouldPlay = true }, (IServerPlayer)((EntityPlayer)byEntity).Player);
                    playOnce = false;
                }

                itemslot.MarkDirty();
            }
        }
    }
}
