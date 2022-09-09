using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProtoBuf;

using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

[assembly: ModInfo("TieredSuperiority",
    Description = "Reduces tool/weapon damage when working with objects of lower tiers.",
    Side = "Server",
    Authors = new[] { "Landar" },
    Version = "1.0.0")]

// Notes:
// Refunding spear durability on throw still not working
namespace tieredsuperiority.src
{
    // just used to print error logs from harmony patch, remove later
    public class API
    {
        public static ICoreAPI api;
    }


    class TieredSuperiorityMain : ModSystem
    {
        bool requireInit = true; // init sounds

        public static IServerNetworkChannel sChannel;
        IClientNetworkChannel cChannel;
        static ICoreServerAPI sapi;       
        ICoreClientAPI capi;

        ILoadedSound dingSound;


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Warning("Loading Tiered Superiority...");

            API.api = api;
            var harmony = new Harmony("tieredsuperiority");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            api.RegisterItemClass("TSItemAxe", typeof(TSItemAxe));
            api.RegisterItemClass("TSItemChisel", typeof(TSItemChisel));
            api.RegisterItemClass("TSItemCleaver", typeof(TSItemCleaver));
            api.RegisterItemClass("TSItemHammer", typeof(TSItemHammer));
            api.RegisterItemClass("TSItemHoe", typeof(TSItemHoe));
            api.RegisterItemClass("TSItemKnife", typeof(TSItemKnife));
            api.RegisterItemClass("TSItemPickaxe", typeof(TSItemPickaxe));
            api.RegisterItemClass("TSItemProspectingPick", typeof(TSItemProspectingPick));
            api.RegisterItemClass("TSItemScythe", typeof(TSItemScythe));                      
            api.RegisterItemClass("TSItemShears", typeof(TSItemShears));
            api.RegisterItemClass("TSItemShovel", typeof(TSItemShovel));           
            api.RegisterItemClass("TSItemSpear", typeof(TSItemSpear));
            api.RegisterItemClass("TSItemSword", typeof(TSItemSword));
        }

        
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;
            sChannel =
                sapi.Network.RegisterChannel("cPlaySound")
                .RegisterMessageType(typeof(SoundMessage));
        }

        
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;
            cChannel =
                capi.Network.RegisterChannel("cPlaySound")
                .RegisterMessageType(typeof(SoundMessage))
                .SetMessageHandler<SoundMessage>(OnPlaySound);
        }


        // returns true if durability was refunded
        public static void RefundDurability(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int initDurability, int targettier, int tooltier)
        {
            var rand = new Random();
            var itemstack = itemslot.Itemstack;
           
            var durdiff = initDurability - itemstack.Collectible.GetRemainingDurability(itemstack);           

            var refundChance = 15 * (tooltier - targettier); // 15% per tier, certain tools compare against tier of block
            byEntity.Api.Logger.Warning("durability diff:" + durdiff);
            byEntity.Api.Logger.Warning("Refund Chance: 15 x " + "(" + tooltier + " - " + targettier + ") = " + refundChance + "%");

            if (durdiff > 0)
            {
                var playOnce = true;
                for (var i = 0; i < durdiff; i++)
                {
                    var roll = rand.Next(100);
                    byEntity.Api.Logger.Warning("refund roll: " + roll);
                    if (roll < refundChance) 
                    {
                        itemstack.Attributes.SetInt("durability", itemstack.Collectible.GetRemainingDurability(itemstack) + 1);
                        byEntity.Api.Logger.Warning("Refunded tool durability");

                        if (playOnce) // only play sound once if multiple instances of damage
                        {
                            sChannel.SendPacket(new SoundMessage() { message = true }, (IServerPlayer)((EntityPlayer)byEntity).Player); // signals the player to play a sound
                            playOnce = false;
                        }

                        itemslot.MarkDirty();
                    }
                }
            }
        }


        private void OnPlaySound(SoundMessage shouldPlaySound)
        {
            if (requireInit)
            {
                dingSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("tieredsuperiority", "sounds/ding.ogg"),
                    ShouldLoop = false,
                    RelativePosition = true,
                    DisposeOnFinish = false,
                    SoundType = EnumSoundType.Sound,
                    Volume = 1f
                });

                capi.Logger.Warning("[Tiered Superiority] sound initialized successfully!");
                requireInit = false;
            }

            if (shouldPlaySound.message)
            {
                dingSound.Start();
                capi.Logger.Warning("[Tiered Superiority] played sound!");
            }
            else
                capi.Logger.Warning("[Tiered Superiority] packet message was false!");
        }
    }


    // tells the client to play a sound
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SoundMessage { public bool message; }


    // removes the 1/4 chance to not consume durability from BlockEntityChisel
    [HarmonyPatch(typeof(BlockEntityChisel), "UpdateVoxel")]
    public class Patch_BlockEntityChisel_UpdateVoxel 
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var foundSection = false;
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_3) // skip Api.World.Rand.Next(3) == 0
                {
                    codes.RemoveRange(i - 4, 7);
                    foundSection = true;
                    API.api.Logger.Warning("[Tiered Superiority] successfully executed patch!");
                    break;
                }
            }

            if (!foundSection)
                API.api.Logger.Warning("[Tiered Superiority] failed to execute patch...");

            return codes;
        }
    }
}
