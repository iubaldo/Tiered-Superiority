﻿using System;
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
    class TSItemKnife: ItemKnife
    {
        int adjustedTier = 0; // ToolTier is 0, so use material type as a marker instead

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["material"]) // read from tool json
            {
                case "bone-chert": adjustedTier = 0; break;
                case "bone-granite": adjustedTier = 0; break;
                case "bone-andesite": adjustedTier = 0; break;
                case "bone-basalt": adjustedTier = 0; break;
                case "bone-obsidian": adjustedTier = 0; break;
                case "bone-peridotite": adjustedTier = 0; break;
                case "bone-flint": adjustedTier = 0; break;
                case "chert": adjustedTier = 0; break;
                case "granite": adjustedTier = 0; break;
                case "andesite": adjustedTier = 0; break;
                case "basalt": adjustedTier = 0; break;
                case "obsidian": adjustedTier = 0; break;
                case "peridotite": adjustedTier = 0; break;
                case "flint": adjustedTier = 0; break;

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