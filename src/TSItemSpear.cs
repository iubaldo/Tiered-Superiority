using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace tieredsuperiority.src
{
    // For pickaxe, shovel, or other basic tools with no additional functionality
    class TSItemSpear : ItemSpear
    {
        int initAttackDurability = -1;
        int adjustedTier = 0; // ToolTier is 0, so use metal type as a marker instead

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["material"]) // read from tool json
            {
                case "chert": adjustedTier = 0; break;
                case "granite": adjustedTier = 0; break;
                case "andesite": adjustedTier = 0; break;
                case "basalt": adjustedTier = 0; break;
                case "obsidian": adjustedTier = 0; break;
                case "peridotite": adjustedTier = 0; break;
                case "flint": adjustedTier = 0; break;

                case "copper": adjustedTier = 1; break;
                case "scrap": adjustedTier = 1; break;

                case "tinbronze": adjustedTier = 2; break;
                case "bismuthbronze": adjustedTier = 2; break;
                case "blackbronze": adjustedTier = 2; break;
                case "hacking": adjustedTier = 2; break;
                case "ornate-silver": adjustedTier = 2; break;
                case "ornate-gold": adjustedTier = 2; break;

                case "iron": adjustedTier = 3; break;
                case "meteoriciron": adjustedTier = 3; break;

                case "steel": adjustedTier = 4; break;

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
