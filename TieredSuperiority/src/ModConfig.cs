using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TieredSuperiority.src
{
    class ModConfig
    {
        public static ModConfig Instance { get; set; } = new ModConfig();

        /// <summary>
        /// Chance for durability refund per additional tier above targeted block tier
        /// ex. bronze pickaxe (tier 3) vs stone block (tier 2): (3 - 2) x chancePerTier = final refund chance
        /// </summary>
        public int ChancePerTier { get { return _chancePerTier; } set { _chancePerTier = (value >= 0) ? value : 0; } }
        public int _chancePerTier = 10;

        /// <summary>
        /// Whether or not to play a 'ding' sound upon refund
        /// </summary>
        public bool PlaySoundOnRefund { get { return _playSoundOnRefund; } set { _playSoundOnRefund = value; } }
        public bool _playSoundOnRefund = true;
    }
}
