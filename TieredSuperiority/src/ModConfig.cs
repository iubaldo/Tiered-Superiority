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
        /// Current version of the config file. Mismatching versions will cause the config file to be regenerated with default values.
        /// </summary>
        public double ConfigVersion { get; set; } = 3.0;

        /// <summary>
        /// Chance for durability refund per additional tier above targeted block tier
        /// ex. bronze pickaxe (tier 3) vs stone block (tier 2) -> (3 - 2) x chancePerTier = final refund chance
        /// </summary>
        public int ChancePerTier { get; set; } = 10;

        /// <summary>
        /// Whether or not to play a 'ding' sound upon refund
        /// </summary>
        public bool PlaySoundOnRefund { get; set; } = true;

        /// <summary>
        /// Toggle for enabling refunds for Primitive Materials (Tier 1)
        /// </summary>
        public bool EnablePrimitiveRefund { get; set; } = true;

        /// <summary>
        /// Toggle for enabling refunds for Soft Metals (Tier 2)
        /// </summary>
        public bool EnableSoftMetalRefund { get; set; } = true;

        /// <summary>
        /// Toggle for enabling refunds for Bronze Alloys (Tier 3)
        /// </summary>
        public bool EnableBronzeRefund { get; set; } = true;

        /// <summary>
        /// Toggle for enabling refunds for Iron Metals (Tier 4)
        /// </summary>
        public bool EnableIronRefund { get; set; } = true;

        /// <summary>
        /// Toggle for enabling refunds for Steel (Tier 5)
        /// </summary>
        public bool EnableSteelRefund { get; set; } = true;

        /// <summary>
        /// Creates a new config with default values
        /// </summary>
        public static ModConfig CreateDefault()
        {
            return new ModConfig();
        }
    }
}
