using System;
using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

using ProtoBuf;
using HarmonyLib;


namespace TieredSuperiority.src
{
    [HarmonyPatch]
    public class TieredSuperiorityMain: ModSystem
    {
        Harmony harmony;

        readonly string CONFIG_FILE_NAME = "tieredsuperiorityconfig.json";
        bool requireInit = true; // init sounds only once

        internal static TSConfig config;

        internal static IServerNetworkChannel sSoundChannel;
        IClientNetworkChannel cSoundChannel;

        internal static ICoreServerAPI sapi;
        ICoreClientAPI capi;

        ILoadedSound dingSound;


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            // register behaviors
            api.RegisterCollectibleBehaviorClass("TSBehavior", typeof(TSBehavior));
            api.RegisterCollectibleBehaviorClass("TSBehaviorHammer", typeof(TSBehaviorHammer));

            harmony = new Harmony("TieredSuperiority");
            harmony.PatchAll();
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
                if (item.Code != null && item.Tool != null && item.ToolTier >= 1)
                {
                    if (item.Tool == EnumTool.Hammer)
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new TSBehaviorHammer(item));
                        
                    else
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new TSBehavior(item));
                }
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


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemHammer), "OnHeldAttackStop")]
        public static void HammerPostStrike(ItemHammer __instance, float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || secondsPassed < 0.4f) return;


            EnumHandHandling handling = EnumHandHandling.Handled;
            TSBehaviorHammer behavior = __instance.CollectibleBehaviors.OfType<TSBehaviorHammer>().DefaultIfEmpty(null).FirstOrDefault();
            if (behavior != null)
                behavior.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSel, entitySel, ref handling);
        }


        public override void Dispose()
        {
            base.Dispose();

            if (harmony != null)
                harmony.UnpatchAll("TieredSuperiority");
        }
    }


    // tells the client to play a sound
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SoundMessage { public bool shouldPlay; }
}
