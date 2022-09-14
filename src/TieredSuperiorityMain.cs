using System;

using ProtoBuf;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Server;


// Notes:
// Refunding spear durability on throw still not working
// Chisel not implemented in this version
// Weapons don't take target armor tier into account yet


namespace tieredsuperiority.src
{
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

            api.RegisterItemClass("TSItemAxe", typeof(TSItemAxe));
            api.RegisterItemClass("TSItemCleaver", typeof(TSItemCleaver));
            api.RegisterItemClass("TSItemHammer", typeof(TSItemHammer));
            api.RegisterItemClass("TSItemHoe", typeof(TSItemHoe));
            api.RegisterItemClass("TSItemKnife", typeof(TSItemKnife));
            api.RegisterItemClass("TSItemPickaxe", typeof(TSItemPickaxe));
            api.RegisterItemClass("TSItemProspectingPick", typeof(TSItemProspectingPick));
            api.RegisterItemClass("TSItemSaw", typeof(TSItemSaw));
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
}
