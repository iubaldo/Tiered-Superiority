using System;
using System.Linq;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

using ProtoBuf;
using HarmonyLib;
using Vintagestory.API.Common.Entities;


/*
 * TODO:
 *      - fix behavior for axe tree felling
 *      - harmony patch for shears/scythe multi hit
 *      - harmony patch for attacking weapons vs armor
 *      
 *      maybe
 *      - adjust comparison tier for tools like shovels, knives, etc since their tool tier is 0 no matter what
 */


namespace TieredSuperiority.src
{
    public class TieredSuperiorityMain: ModSystem
    {
        const string CONFIG_FILE_NAME = "tieredsuperiorityconfig.json";
        const string PATCH_CODE = "Landar.TieredSuperiority.TieredSuperiorityMain";
        
        public Harmony harmony = new Harmony(PATCH_CODE);

        internal static TSConfig config;
        static Random rand = new();

        static IServerNetworkChannel sSoundChannel;
        IClientNetworkChannel cSoundChannel;

        internal static ICoreServerAPI sapi;
        ICoreClientAPI capi;

        ILoadedSound dingSound;

        bool requireInit = true; // init sounds only once


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            // register behaviors
            api.RegisterCollectibleBehaviorClass("TSBehavior", typeof(TSBehavior));
            api.RegisterCollectibleBehaviorClass("TSBehaviorHammer", typeof(TSBehaviorHammer));

            // apply harmony patches
            harmony.PatchAll();

            //StringBuilder builder = new StringBuilder("Harmony Patched Methods: ");
            //foreach (var val in harmony.GetPatchedMethods())
            //    builder.Append(val.Name + ", ");

            //Mod.Logger.Notification(builder.ToString());
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;

            sSoundChannel =
                sapi.Network.RegisterChannel("refundSoundChannel")
                .RegisterMessageType(typeof(SoundMessage));

            try 
            {
                config = sapi.LoadModConfig<TSConfig>(CONFIG_FILE_NAME);
            } catch (Exception) { }

            if (config == null)
                config = new();

            sapi.StoreModConfig(config, CONFIG_FILE_NAME);
        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;
            cSoundChannel =
                capi.Network.RegisterChannel("refundSoundChannel")
                .RegisterMessageType(typeof(SoundMessage))
                .SetMessageHandler<SoundMessage>(OnPlaySound);
        }


        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

            // append behaviors without using JSON patching
            foreach(Item item in api.World.Items)
            {
                if (item.Code != null && item.Tool != null)
                {
                    if (item.Tool == EnumTool.Hammer)
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new TSBehaviorHammer(item));
                    else if (IsTool(item))
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new TSBehavior(item));
                }
            }

            api.Logger.StoryEvent("Loading TS");
        }


        public static void RefundDurability(CollectibleObject obj, Entity byEntity, ItemSlot itemslot, int selectionTier, int durabilityDiff)
        {
            if (byEntity.World.Api.Side == EnumAppSide.Client)
                return;

            int adjustedTier = 0;
            if (obj.ToolTier == 0)
            {
                if (obj.Variant["metal"] != null)
                    adjustedTier = ResolveTier(obj.Variant["metal"]);
                else if (obj.Variant["material"] != null)
                    adjustedTier = ResolveTier(obj.Variant["material"]);
            }

            int adjustedChance = Math.Clamp(config.chancePerTier, 0, 100);
            int refundChance = adjustedChance * ((obj.ToolTier == 0 ? adjustedTier : obj.ToolTier) - selectionTier); // by default, 10% per tier difference

            sapi.BroadcastMessageToAllGroups("Durability diff: " + durabilityDiff, EnumChatType.Notification);
            sapi.BroadcastMessageToAllGroups("Refund Chance: " + refundChance + " x " + "(" + (obj.ToolTier == 0 ? adjustedTier : obj.ToolTier) + " - " + selectionTier + ") = " + refundChance + "%", EnumChatType.Notification);

            bool playOnce = false;
            for (int i = 0; i < durabilityDiff; i++)
            {
                if (rand.Next(100) < refundChance)
                {
                    itemslot.Itemstack.Attributes.SetInt("durability", obj.GetRemainingDurability(itemslot.Itemstack) + 1);
                    sapi.BroadcastMessageToAllGroups("Refunded tool durability.", EnumChatType.Notification);

                    if (config.playSound && !playOnce)
                    {
                        sSoundChannel.SendPacket(new SoundMessage() { shouldPlay = true }, (IServerPlayer)((EntityPlayer)byEntity).Player);
                        playOnce = true;
                    }

                    itemslot.MarkDirty();
                }
            }
        }


        public static bool IsTool(Item item)
        {
            if (item.Code == null || item.Tool == null)
                return false;

            return item.Tool == EnumTool.Shovel
                || item.Tool == EnumTool.Saw
                || item.Tool == EnumTool.Pickaxe
                || item.Tool == EnumTool.Axe
                || item.Tool == EnumTool.Hoe
                || item.Tool == EnumTool.Wrench
                || item.Tool == EnumTool.Sword
                || item.Tool == EnumTool.Knife
                || item.Tool == EnumTool.Hammer
                || item.Tool == EnumTool.Shears
                || item.Tool == EnumTool.Scythe
                ;
        }


        // more-or-less copied from worldproperties\block\toolmetal
        // TODO: find a way to read from that file directly
        public static int ResolveTier(string variant)
        {
            switch (variant) 
            {
                case "bone-chert": return 1;
                case "bone-granite": return 1;
                case "bone-andesite": return 1;
                case "bone-basalt": return 1;
                case "bone-obsidian": return 1;
                case "bone-peridotite": return 1;
                case "bone-flint": return 1;                
                case "chert": return 1;
                case "granite": return 1;
                case "andesite": return 1;
                case "basalt": return 1;
                case "obsidian": return 1;
                case "peridotite": return 1;
                case "flint": return 1;
                case "lead": return 1;
                case "molybdochalkos": return 1;

                case "bismuth": return 2;
                case "copper": return 2;    
                case "silver": return 2;
                case "gold": return 2;

                case "tinbronze": return 3;
                case "bismuthbronze": return 3;
                case "blackbronze": return 3;

                case "iron": return 4;
                case "meteoriciron": return 4;

                case "steel": return 5;

                // unused tool metals, might become relevant in the future?
                // case "tin": workitemtier = 1; break;
                // case "zinc": workitemtier = 1; break;
                // case "chromium": workitemtier = 5; break;
                // case "stainlesssteel": workitemtier = 5; break;
                // case "platinum": workitemtier = 6; break;
                // case "titanium": workitemtier = 6; break;
                // case "rhodium": workitemtier = 7; break;
                // case "uranium": workitemtier = 8; break;

                default: sapi.Logger.Notification("No valid variants found for " + variant + ", defaulting tier to 0."); return 0;
            }
        }


        void OnPlaySound(SoundMessage message)
        {
            if (requireInit)
            {
                dingSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("tieredsuperiority", "sounds/ding.ogg"),
                    ShouldLoop = false,
                    RelativePosition = true,
                    DisposeOnFinish = false,
                    SoundType = EnumSoundType.Sound
                });

                requireInit = false;
            }

            if (message.shouldPlay)
                dingSound.Start();
        }


        public override void Dispose()
        {
            base.Dispose();

            if (harmony != null)
                harmony.UnpatchAll(PATCH_CODE);
        }
    }


    // tells the client to play a sound
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SoundMessage { public bool shouldPlay; }
}
