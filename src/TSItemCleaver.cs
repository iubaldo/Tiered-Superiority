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
    class TSItemCleaver : ItemCleaver
    {
        int adjustedTier = 0; // ToolTier is 0, so use material type as a marker instead

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["material"]) // read from tool json
            {
                case "copper": adjustedTier = 0; break;
                case "silver": adjustedTier = 0; break;
                case "gold": adjustedTier = 0; break;

                case "tinbronze": adjustedTier = 1; break;
                case "bismuthbronze": adjustedTier = 1; break;
                case "blackbronze": adjustedTier = 1; break;

                case "iron": adjustedTier = 2; break;
                case "meteoriciron": adjustedTier = 2; break;

                case "steel": adjustedTier = 3; break;

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
