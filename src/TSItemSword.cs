using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace tieredsuperiority.src
{
    class TSItemSword : ItemSword
    {
        int initAttackDurability = -1;
        int adjustedTier = 0; // ToolTier is 0, so use metal type as a marker instead
        // note, could also use attack tier instead?

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["metal"]) // read from tool json
            {
                case "copper": adjustedTier = 2; break;
                case "scrap": adjustedTier = 2; break;
                case "silver": adjustedTier = 2; break;
                case "gold": adjustedTier = 2; break;

                case "tinbronze": adjustedTier = 3; break;
                case "bismuthbronze": adjustedTier = 3; break;
                case "blackbronze": adjustedTier = 3; break;

                case "blackguard": adjustedTier = 4; break;
                case "forlorn": adjustedTier = 4; break;
                case "iron": adjustedTier = 4; break;
                case "meteoriciron": adjustedTier = 4; break;

                case "steel": adjustedTier = 5; break;

                case "admin": adjustedTier = 99; break;

                default: api.Logger.Warning("[Tiered Superiority] No valid variants found for " + Code + ", defaulting tier to 0"); adjustedTier = 0; break;
            }
        }


        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);

            ItemStack itemstack = slot.Itemstack;
            initAttackDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
        }


        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel);

            if (byEntity.World.Api.Side == EnumAppSide.Client) // only perform checks serverside                      
                return;

            // replace targettier with target's protection tier later
            TieredSuperiorityMain.RefundDurability(byEntity.Api.World, byEntity, slot, initAttackDurability, 0, adjustedTier);
        }
    }
}
