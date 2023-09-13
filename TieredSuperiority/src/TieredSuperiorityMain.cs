using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

using ProtoBuf;
using HarmonyLib;


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

        internal static IServerNetworkChannel sSoundChannel;
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
            //{
            //    builder.Append(val.Name + ", ");
            //}
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
                    else if (item.Tool == EnumTool.Shovel 
                        || item.Tool == EnumTool.Saw
                        || item.Tool == EnumTool.Pickaxe
                        || item.Tool == EnumTool.Axe
                        || item.Tool == EnumTool.Hoe
                        || item.Tool == EnumTool.Wrench
                        || item.Tool == EnumTool.Sword
                        || item.Tool == EnumTool.Knife)
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new TSBehavior(item));
                }
            }

            //StringBuilder builder = new StringBuilder("Attached behavior to the following items: ");
            //foreach (Item item in api.World.Items)
            //{
            //    if (item.CollectibleBehaviors.OfType<TSBehavior>().Any())
            //        builder.Append("\n" + item.Code.Path);
            //}
            //Mod.Logger.Notification(builder.ToString());
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
