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
                case "copper": adjustedTier = 0; break;
                case "scrap": adjustedTier = 0; break;
                case "silver": adjustedTier = 0; break;
                case "gold": adjustedTier = 0; break;

                case "tinbronze": adjustedTier = 1; break;
                case "bismuthbronze": adjustedTier = 1; break;
                case "blackbronze": adjustedTier = 1; break;

                case "blackguard": adjustedTier = 2; break;
                case "forlorn": adjustedTier = 2; break;
                case "iron": adjustedTier = 2; break;
                case "meteoriciron": adjustedTier = 2; break;

                case "steel": adjustedTier = 3; break;

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
