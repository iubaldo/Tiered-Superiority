using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace tieredsuperiority.src
{
    class TSItemCleaver : ItemCleaver
    {
        int adjustedTier = 0; // ToolTier is 0, so use material type as a marker instead


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


        public override void OnHeldInteractStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            ItemStack itemstack = slot.Itemstack;

            int initDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
            base.OnHeldInteractStop(secondsPassed, slot, byEntity, blockSelection, entitySel);

            if (byEntity.World.Api.Side == EnumAppSide.Client) // only perform checks serverside                      
                return;

            // replace targettier with target's protection tier later
            TieredSuperiorityMain.RefundDurability(byEntity.Api.World, byEntity, slot, initDurability, 0, adjustedTier);
        }
    }
}
