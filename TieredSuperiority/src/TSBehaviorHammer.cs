using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TieredSuperiority.src
{
    internal class TSBehaviorHammer : TSBehavior
    {
        int currDurability;


        public TSBehaviorHammer(CollectibleObject collObj) : base(collObj) { }


        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling); // hammer loses durability here

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSelection.Position);
            if (!(byEntity.World.BlockAccessor.GetBlock(blockSelection.Position) is BlockAnvil)) return;
            BlockEntityAnvil bea = be as BlockEntityAnvil;
            if (bea == null) return;

            // more-or-less copied from worldproperties\block\toolmetal
            // TODO: find a way to read from that file directly
            int workitemtier;
            switch (bea.WorkItemStack.Item.Variant["metal"])
            {
                case "lead": workitemtier = 0; break;
                case "molybdochalkos": workitemtier = 0; break;
                case "bismuth": workitemtier = 1; break;
                case "copper": workitemtier = 1; break;
                case "silver": workitemtier = 1; break;
                case "gold": workitemtier = 1; break;
                case "bismuthbronze": workitemtier = 2; break;
                case "blackbronze": workitemtier = 2; break;
                case "brass": workitemtier = 2; break;
                case "tinbronze": workitemtier = 2; break;
                case "iron": workitemtier = 3; break;
                case "meteoriciron": workitemtier = 3; break;
                case "steel": workitemtier = 4; break;
            
                // unused tool metals, might become relevant in the future?
                // case "tin": workitemtier = 1; break;
                // case "zinc": workitemtier = 1; break;
                // case "chromium": workitemtier = 5; break;
                // case "stainlesssteel": workitemtier = 5; break;
                // case "platinum": workitemtier = 6; break;
                // case "titanium": workitemtier = 6; break;
                // case "rhodium": workitemtier = 7; break;
                // case "uranium": workitemtier = 8; break;

                default: workitemtier = 0; break;
            }

            RefundDurability(byEntity, slot, workitemtier);
        }
    }
}
