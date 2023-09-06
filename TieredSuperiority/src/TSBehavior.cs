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
            int initDurability = collObj.Durability;
            int blockTier = byEntity.World.BlockAccessor.GetBlock(blockSel.Position).ToolTier;

            bool toReturn = base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling); // tool loses durability here

            if (world.Api.Side == EnumAppSide.Client)
                return toReturn;

            int durabilityDiff = initDurability - collObj.Durability;

            RefundDurability(byEntity, itemslot, durabilityDiff, blockTier);

            return toReturn;
        }


        public void RefundDurability(Entity byEntity, ItemSlot itemslot, int durabilityDiff, int selectionTier)
        {
            if (durabilityDiff <= 0)
                return;

            bool playOnce = true;
            int refundChance = 15 * (collObj.ToolTier - selectionTier); // 15% per tier difference

            TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("durability diff:" + durabilityDiff, EnumChatType.Notification);
            TieredSuperiorityMain.sapi.BroadcastMessageToAllGroups("Refund Chance: 15 x " + "(" + collObj.ToolTier + " - " + selectionTier + ") = " + refundChance + "%", EnumChatType.Notification);

            for (int i = 0; i < durabilityDiff; i++) // do a refund roll for each point of difference
            {
                if (rand.Next(100) < refundChance)
                {
                    collObj.ToolTier++;
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
}
