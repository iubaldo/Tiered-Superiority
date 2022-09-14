using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace tieredsuperiority.src
{
    class TSItemShears : ItemShears
    {
        int adjustedTier = 0; // ToolTier is 0, so use metal type as a marker instead


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["metal"]) // read from tool json
            {
                case "copper": adjustedTier = 2; break;
                case "silver": adjustedTier = 2; break;
                case "gold": adjustedTier = 2; break;

                case "tinbronze": adjustedTier = 3; break;
                case "bismuthbronze": adjustedTier = 3; break;
                case "blackbronze": adjustedTier = 3; break;

                case "iron": adjustedTier = 4; break;
                case "meteoriciron": adjustedTier = 4; break;

                case "steel": adjustedTier = 5; break;

                default: api.Logger.Warning("[Tiered Superiority] No valid variants found for " + Code + ", defaulting tier to 0"); adjustedTier = 0; break;
            }
        }


        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            ItemStack itemstack = itemslot.Itemstack;
            int blocktier = byEntity.World.BlockAccessor.GetBlock(blockSel.Position).RequiredMiningTier;

            int initDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
            bool toReturn = base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);

            if (world.Api.Side == EnumAppSide.Client) // only perform checks serverside                      
                return toReturn;

            TieredSuperiorityMain.RefundDurability(world, byEntity, itemslot, initDurability, blocktier, adjustedTier);

            return toReturn;
        }
    }
}