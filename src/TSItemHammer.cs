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
    class TSItemHammer : ItemHammer
    {
        int adjustedTier = 0; // ToolTier is 0, so use metal type as a marker instead

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["metal"]) // read from tool json
            {
                case "copper": adjustedTier = 1; break;
                case "silver": adjustedTier = 1; break;
                case "gold": adjustedTier = 1; break;

                case "tinbronze": adjustedTier = 2; break;
                case "bismuthbronze": adjustedTier = 2; break;
                case "blackbronze": adjustedTier = 2; break;

                case "iron": adjustedTier = 3; break;
                case "meteoriciron": adjustedTier = 3; break;

                case "steel": adjustedTier = 4; break;

                default: api.Logger.Warning("[Tiered Superiority] No valid variants found for " + Code + ", defaulting tier to 0"); adjustedTier = 0; break;
            }
        }


        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // sanity checks from base function
            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (!(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockAnvil)) return;
            BlockEntityAnvil bea = be as BlockEntityAnvil;
            if (bea == null) return;

            ItemStack itemstack = slot.Itemstack;
            int initDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSel, entitySel); // applies damage to item

            if (byEntity.Api.Side == EnumAppSide.Client) // only perform checks serverside
                return;
          
            Task.Delay(100).ContinueWith(t => DelayedCheck(initDurability, slot, byEntity, bea)); 
        }


        // need to delay since hammer handles serverside updates uniquely
        private void DelayedCheck(int initDurability, ItemSlot slot, EntityAgent byEntity, BlockEntityAnvil bea)
        {
            // more-or-less copied from worldproperties\block\metal, can't be assed to find a way to read from it
            int workitemtier;
            switch (bea.WorkItemStack.Item.Variant["metal"])
            {
                case "lead": workitemtier = 0; break;
                case "molybdochalkos": workitemtier = 0; break;
                case "bismuth": workitemtier = 1; break;
                case "copper": workitemtier = 1; break;
                case "silver": workitemtier = 1; break;
                case "gold": workitemtier = 1; break;
                case "tin": workitemtier = 1; break; // in properties, it's actually tier 4 because ???, changed it to 1 since pure tin is soft irl
                case "zinc": workitemtier = 1; break;
                case "bismuthbronze": workitemtier = 2; break;
                case "blackbronze": workitemtier = 2; break;
                case "brass": workitemtier = 2; break;
                case "tinbronze": workitemtier = 2; break;
                case "iron": workitemtier = 3; break;
                case "meteoriciron": workitemtier = 3; break;
                case "steel": workitemtier = 4; break;
                case "chromium": workitemtier = 5; break;
                case "stainlesssteel": workitemtier = 5; break;
                case "platinum": workitemtier = 6; break;
                case "titanium": workitemtier = 6; break;
                case "rhodium": workitemtier = 7; break;
                case "uranium": workitemtier = 8; break;

                default: api.Logger.Warning("[Tiered Superiority] No valid variants found, defaulting tier to 0"); workitemtier = 0; break;
            }

            TieredSuperiorityMain.RefundDurability(byEntity.Api.World, byEntity, slot, initDurability, workitemtier, adjustedTier);
        }
    }
}
