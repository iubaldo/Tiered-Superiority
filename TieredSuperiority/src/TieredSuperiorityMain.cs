using HarmonyLib;
using ProtoBuf;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TieredSuperiority.src
{
    public class TieredSuperiorityMain: ModSystem
    {
        bool requireInit = true; // init sounds only once

        internal static IServerNetworkChannel sSoundChannel;
        IClientNetworkChannel cSoundChannel;

        public static ICoreServerAPI sapi;
        ICoreClientAPI capi;

        ILoadedSound dingSound;


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Notification("Loading Tiered Superiority Mod...");

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

            sapi.ChatCommands
                .GetOrCreate("testcmd")
                .IgnoreAdditionalArgs()
                .RequiresPrivilege("worldedit")
                .WithDescription("TS mod debug commands")

                .BeginSubCommand("listbehaviors")
                    .WithDescription("Lists behavior names of all tool items")
                    .HandleWith(OnCmdListBehaviors)
                .EndSubCommand()
                ;
        }


        private TextCommandResult OnCmdListBehaviors(TextCommandCallingArgs args)
        {
            sapi.BroadcastMessageToAllGroups("List of items with TSBehavior:", EnumChatType.Notification);
            foreach (Item item in sapi.World.Items)
            {
                if (item.Tool != null)
                {
                    foreach (CollectibleBehavior behavior in item.CollectibleBehaviors)
                    {
                        if (behavior is TSBehavior)
                        {
                            sapi.BroadcastMessageToAllGroups(item.Code + ":", EnumChatType.Notification);
                            sapi.BroadcastMessageToAllGroups("\t" + behavior.GetType().Name, EnumChatType.Notification);
                        }
                    }
                }
            }

            return TextCommandResult.Success();
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
                        item.CollectibleBehaviors = (CollectibleBehavior[])item.CollectibleBehaviors.Append(new TSBehaviorHammer(item));
                    else
                        item.CollectibleBehaviors = (CollectibleBehavior[])item.CollectibleBehaviors.Append(new TSBehavior(item));
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
    }


    // tells the client to play a sound
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SoundMessage { public bool shouldPlay; }
}
