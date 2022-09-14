using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace tieredsuperiority.src
{
    class TSItemSpear : ItemSpear
    {
        int initAttackDurability = -1;
        int adjustedTier = 1; // ToolTier is 0, so use metal type as a marker instead
        // note, could also use attack tier instead?


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["material"]) // read from tool json
            {
                case "chert": adjustedTier = 1; break;
                case "granite": adjustedTier = 1; break;
                case "andesite": adjustedTier = 1; break;
                case "basalt": adjustedTier = 1; break;
                case "obsidian": adjustedTier = 1; break;
                case "peridotite": adjustedTier = 1; break;
                case "flint": adjustedTier = 1; break;

                case "copper": adjustedTier = 2; break;
                case "scrap": adjustedTier = 2; break;

                case "tinbronze": adjustedTier = 3; break;
                case "bismuthbronze": adjustedTier = 3; break;
                case "blackbronze": adjustedTier = 3; break;
                case "hacking": adjustedTier = 3; break;

                case "ornate-silver": adjustedTier = 4; break;
                case "ornate-gold": adjustedTier = 4; break;
                case "iron": adjustedTier = 4; break;
                case "meteoriciron": adjustedTier = 4; break;

                case "steel": adjustedTier = 5; break;

                default: api.Logger.Warning("[Tiered Superiority] No valid variants found for " + Code + ", defaulting tier to 0"); adjustedTier = 1; break;
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


        // spear throw, still need to figure out how to get thrown spear's durability
        //public override void OnHeldInteractStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        //{
        //    ItemStack itemstack = slot.Itemstack;

        //    int initDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
        //    base.OnHeldInteractStop(secondsPassed, slot, byEntity, blockSelection, entitySel);

        //    // replace blocktier with target's protection tier later
        //    TieredSuperiorityMain.RefundDurability(byEntity.Api.World, byEntity, slot, initDurability, 0, adjustedTier);
        //}
    }
}
