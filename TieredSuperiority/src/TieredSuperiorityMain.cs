﻿using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

using ProtoBuf;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using System.Text;


/*
 * TODO:
 *      - implement tier difference for attacking weapons vs armor
 *      - implement tier difference for crafting
 *          - check crafting tool in grid vs highest other tier item in recipe, default to 1 if none
 */


namespace TieredSuperiority.src
{
    public class TieredSuperiorityMain: ModSystem
    {
        internal static bool debugMode = false; // enables verbose debug print statements and debug commands
        const string CONFIG_FILE_NAME = "tieredsuperiorityconfig.json";
        const string PATCH_CODE = "Landar.TieredSuperiority.TieredSuperiorityMain";

        private Harmony _harmony;
        private static Random rand = new();

        static IServerNetworkChannel sSoundChannel;
        IClientNetworkChannel cSoundChannel;

        internal static ICoreServerAPI sapi;
        ICoreClientAPI capi;

        ILoadedSound dingSound;

        bool requireInit = true; // init sounds only once


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            // apply harmony patches
            _harmony = new Harmony(PATCH_CODE);
            _harmony.PatchAll();

            if (debugMode)
            {
                StringBuilder builder = new StringBuilder("Harmony Patched Methods: ");
                foreach (var val in _harmony.GetPatchedMethods())
                    builder.Append(val.Name + ", ");

                Mod.Logger.Notification(builder.ToString());
            }

            // init configs
            try
            {
                ModConfig configFile;
                if ((configFile = api.LoadModConfig<ModConfig>(CONFIG_FILE_NAME)) == null)
                    api.StoreModConfig<ModConfig>(ModConfig.Instance, CONFIG_FILE_NAME);
                else
                    ModConfig.Instance = configFile;
            } catch
            {
                api.StoreModConfig<ModConfig>(ModConfig.Instance, CONFIG_FILE_NAME);
            }
           
            // register behaviors
            api.RegisterCollectibleBehaviorClass("TSBehavior", typeof(TSBehavior));
            api.RegisterCollectibleBehaviorClass("TSBehaviorHammer", typeof(TSBehaviorHammer));
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;

            sSoundChannel =
                sapi.Network.RegisterChannel("refundSoundChannel")
                .RegisterMessageType(typeof(SoundMessage));

            if (debugMode)
            {
                RegisterCommands();
            }
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
                    if (IsTool(item))
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new TSBehavior(item));
                }
            }

            if (debugMode)
            {
                api.Logger.StoryEvent("Loading TS");
            }
        }


        public static void RefundDurability(CollectibleObject tool, Entity byEntity, ItemSlot itemslot, int selectionTier, int durabilityDiff)
        {
            if (byEntity.World.Api.Side == EnumAppSide.Client)
                return;

            int adjustedTier = 0;
            if (tool.ToolTier == 0)
            {
                if (tool.Variant["metal"] != null)
                    adjustedTier = ResolveTier(tool.Variant["metal"]);
                else if (tool.Variant["material"] != null)
                    adjustedTier = ResolveTier(tool.Variant["material"]);
            }

            int adjustedChance = Math.Clamp(ModConfig.Instance.ChancePerTier, 0, 100);
            int refundChance = adjustedChance * ((tool.ToolTier == 0 ? adjustedTier : tool.ToolTier) - selectionTier); // by default, 10% per tier difference

            if (debugMode)
            {
                sapi.BroadcastMessageToAllGroups("Durability diff: " + durabilityDiff, EnumChatType.Notification);
                sapi.BroadcastMessageToAllGroups("Refund Chance: " + refundChance + " x " + "(" + (tool.ToolTier == 0 ? adjustedTier : tool.ToolTier) + " - " + selectionTier + ") = " + refundChance + "%", EnumChatType.Notification);
            }          

            bool playOnce = false;
            for (int i = 0; i < durabilityDiff; i++)
            {
                if (rand.Next(100) < refundChance)
                {
                    itemslot.Itemstack.Attributes.SetInt("durability", tool.GetRemainingDurability(itemslot.Itemstack) + 1);

                    if (debugMode)
                    {
                        sapi.BroadcastMessageToAllGroups("Refunded tool durability.", EnumChatType.Notification);
                    }
                    

                    if (ModConfig.Instance.PlaySoundOnRefund && !playOnce)
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


        // for debug purposes only, remember to disable before release
        void RegisterCommands()
        {
            CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;

            sapi.ChatCommands
                .GetOrCreate("ts")
                .IgnoreAdditionalArgs()
                .RequiresPrivilege("worldedit")
                .WithDescription("Tiered Superiority Mod debug commands.")

                .BeginSubCommand("setDurability")
                    .WithDescription("Sets the durability of the tool in your main hand, if any.")
                    .WithArgs(parsers.IntRange("newDurability", 0, int.MaxValue))
                    .HandleWith(OnCmdSetDurability)
                .EndSubCommand()
            ;
        }


        TextCommandResult OnCmdSetDurability(TextCommandCallingArgs args)
        {
            ItemSlot slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
            Item item = slot.Itemstack.Item;
            if (item == null)
                return TextCommandResult.Error("Error, could not find item in hand.");

            if (!IsTool(item))
                return TextCommandResult.Error("Error, item in hand is not a valid tool.");

            int newDurability = (int)args[0];
            slot.Itemstack.Attributes.SetInt("durability", newDurability);
            slot.MarkDirty();

            return TextCommandResult.Success();
        }


        public override void Dispose()
        {
            base.Dispose();

            if (_harmony != null)
                _harmony.UnpatchAll(PATCH_CODE);
        }
    }


    // tells the client to play a sound
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SoundMessage { public bool shouldPlay; }
}
