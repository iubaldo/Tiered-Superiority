﻿using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

using HarmonyLib;
using Vintagestory.GameContent;

namespace TieredSuperiority.src
{
    [HarmonyPatch]
    public class TSBehavior : CollectibleBehavior
    {
        public int initDurability;
        public long timeLastCalled = -1;


        public TSBehavior(CollectibleObject collObj) : base(collObj) { }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CollectibleObject), "OnBlockBrokenWith")]
        public static void PrefixOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            TSBehavior behavior = __instance.GetCollectibleBehavior(typeof(TSBehavior), false) as TSBehavior;

            if (behavior != null)
                behavior.initDurability = __instance.GetRemainingDurability(itemslot.Itemstack);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CollectibleObject), "OnBlockBrokenWith")]
        public static void PostfixOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
                return;

            if (itemslot.Itemstack == null)
            {
                // TieredSuperiorityMain.sapi.Logger.Notification("item broke before calculation");
                return;
            }
                

            TSBehavior behavior = __instance.GetCollectibleBehavior(typeof(TSBehavior), false) as TSBehavior;

            if (behavior == null)
                return;

            if (TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds - behavior.timeLastCalled < 0.5)
            {
                behavior.timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;
                return;
            }
            behavior.timeLastCalled = TieredSuperiorityMain.sapi.World.Calendar.ElapsedSeconds;

            int durabilityDiff = behavior.initDurability - __instance.GetRemainingDurability(itemslot.Itemstack);
            int selectionTier = blockSel.Block.RequiredMiningTier;

            //TieredSuperiorityMain.sapi.Logger.Notification("durabilityDiff: " + behavior.initDurability + " - " + __instance.GetRemainingDurability(itemslot.Itemstack) + " = " + durabilityDiff);
            if (durabilityDiff > 0)
                TieredSuperiorityMain.RefundDurability(__instance, byEntity, itemslot, selectionTier, durabilityDiff);
        }


        // Patches for items that don't use base.OnBlockBrokenWith


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemAxe), "OnBlockBrokenWith")]
        public static void PrefixAxeOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot)
        {
            PrefixOnBlockBrokenWith(__instance, byEntity, itemslot);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemAxe), "OnBlockBrokenWith")]
        public static void PostfixAxeOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
        {
            PostfixOnBlockBrokenWith(__instance, byEntity, itemslot, blockSel);
        }


        // Note: ItemScythe extends ItemShears, so only need to patch shears
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemShears), "OnBlockBrokenWith")]
        public static void PrefixShearsOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot)
        {
            PrefixOnBlockBrokenWith(__instance, byEntity, itemslot);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemShears), "OnBlockBrokenWith")]
        public static void PostfixShearsOnBlockBrokenWith(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
        {
            PostfixOnBlockBrokenWith(__instance, byEntity, itemslot, blockSel);
        }
    }
}
