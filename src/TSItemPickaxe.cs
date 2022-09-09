using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProtoBuf;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

using tieredsuperiority.src;

namespace tieredsuperiority.src
{
    class TSItemPickaxe : Item
    {
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            ItemStack itemstack = itemslot.Itemstack;
            int blocktier = byEntity.World.BlockAccessor.GetBlock(blockSel.Position).RequiredMiningTier;

            int initDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
            bool toReturn = base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);

            if (world.Api.Side == EnumAppSide.Client) // only perform checks serverside                      
                return toReturn;

            TieredSuperiorityMain.RefundDurability(world, byEntity, itemslot, initDurability, blocktier, ToolTier);           

            return toReturn;
        }
    }
}