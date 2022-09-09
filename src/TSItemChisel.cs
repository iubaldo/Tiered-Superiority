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
    class TSItemChisel : ItemChisel
    {
        int initAttackDurability = -1;
        int adjustedTier = 0; // ToolTier is 0, so use material type as a marker instead

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            switch (Variant["material"]) // read from tool json
            {
                case "copper": adjustedTier = 2; break;

                case "tinbronze": adjustedTier = 3; break;
                case "bismuthbronze": adjustedTier = 3; break;
                case "blackbronze": adjustedTier = 3; break;

                case "iron": adjustedTier = 4; break;
                case "meteoriciron": adjustedTier = 4; break;

                case "steel": adjustedTier = 5; break;

                default: api.Logger.Warning("[Tiered Superiority] No valid variants found for " + Code + ", defaulting tier to 0"); adjustedTier = 0; break;
            }
        }


        // current bug: sending since we short circuit on serverside, we're trying to cast client entity as server player
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            ItemStack itemstack = slot.Itemstack;           
            initAttackDurability = itemstack.Collectible.GetRemainingDurability(itemstack);

            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);

            if (byEntity.World.Api.Side == EnumAppSide.Server) // only perform checks clientside for some reason                      
                return;

            if (blockSel == null)
                return;

            int blocktier = byEntity.World.BlockAccessor.GetBlock(blockSel.Position).RequiredMiningTier;
            TieredSuperiorityMain.RefundDurability(byEntity.Api.World, byEntity, slot, initAttackDurability, blocktier, adjustedTier);
        }


        // returns true if durability was refunded, done here instead of TSMain cause chisel is handled weirdly
        //public void RefundDurability(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int initDurability, int targettier, int tooltier)
        //{
        //    Random rand = new Random();
        //    ItemStack itemstack = itemslot.Itemstack;

        //    int durdiff = initDurability - itemstack.Collectible.GetRemainingDurability(itemstack);

            

        //    int refundChance = 15 * (tooltier - targettier); // 15% per tier, certain tools compare against tier of block
        //    byEntity.Api.Logger.Warning("durability diff:" + durdiff);
        //    byEntity.Api.Logger.Warning("Refund Chance: 15 x " + "(" + tooltier + " - " + targettier + ") = " + refundChance + "%");

        //    if (durdiff > 0)
        //    {
        //        bool playOnce = true;
        //        for (int i = 0; i < durdiff; i++)
        //        {
        //            int roll = rand.Next(100);
        //            byEntity.Api.Logger.Warning("refund roll: " + roll);
        //            if (roll < refundChance)
        //            {
        //                itemstack.Attributes.SetInt("durability", itemstack.Collectible.GetRemainingDurability(itemstack) + 1);
        //                byEntity.Api.Logger.Warning("Refunded tool durability");

        //                if (playOnce) // only play sound once if multiple instances of damage
        //                {
        //                    TieredSuperiorityMain.sChannel.SendPacket(new SoundMessage() { message = true }, (IServerPlayer)((EntityPlayer)byEntity).Player);
        //                    playOnce = false;
        //                }

        //                itemslot.MarkDirty();
        //            }
        //        }
        //    }
        //}
    }
}
