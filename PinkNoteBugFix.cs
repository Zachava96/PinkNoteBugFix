using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Rhythm;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using System;
using Challenges;

namespace NoteFading
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("UNBEATABLE.exe")]
    public class PinkNoteBugFix : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "net.zachava.pinknotebugfix";
        public const string PLUGIN_NAME = "Pink Note Bug Fix";
        public const string PLUGIN_VERSION = "1.0.0";
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PLUGIN_GUID);
			harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(BaseKillableNote))]
    [HarmonyPatch("IsSwungAt")]
    class BaseKillableNotePatches
    {
        static bool Prefix(ref BaseKillableNote __instance, ref bool __result)
		{
            if (!__instance.PlayerCanHit() || !__instance.PlayerSwung() || !__instance.upcoming)
            {
                __result = false;
                return false;
            }
            if (__instance.height != Height.Mid && __instance.height != Height.Top && __instance.height != Height.Low)
            {
                __result = true;
                return false;
            }

            float? topTime = null;
            float? midTime = null;
            float? lowTime = null;
            foreach (KeyValuePair<Lane, BaseKillableNote> upcomingNote in __instance.controller.upcoming)
            {
                float hitTime = upcomingNote.Value.hitTime;

                if (upcomingNote.Value is DoubleNote doubleNote
                    && doubleNote.worstAttack != null
                    && doubleNote.worstAttack.position == __instance.songPosition)
                {
                    hitTime = doubleNote.prevTime;
                }
                else if (upcomingNote.Value is SpamNote spamNote
                        && spamNote.attack != null
                        && spamNote.attack.position == __instance.songPosition)
                {
                    hitTime = spamNote.prevTime;
                }
                else if (upcomingNote.Value is HoldNote holdNote
                        && holdNote.attack != null
                        && holdNote.attack.position == __instance.songPosition)
                {
                    hitTime = holdNote.prevTime;
                }

                switch (upcomingNote.Key.height)
                {
                case Height.Low:
                    if (lowTime == null || hitTime < lowTime)
                    {
                        lowTime = hitTime;
                    }
                    break;
                case Height.Mid:
                    if (midTime == null || hitTime < midTime)
                    {
                        midTime = hitTime;
                    }
                    break;
                case Height.Top:
                    if (topTime == null || hitTime < topTime)
                    {
                        topTime = hitTime;
                    }
                    break;
                }
            }
            switch (__instance.height)
            {
            case Height.Low:
                if (__instance.player.input.anyLow && (midTime == null || midTime >= __instance.hitTime))
                {
                    __result = true;
                    return false;
                }
                break;
            case Height.Mid:
                if (__instance.player.input.anyTop && (topTime == null || topTime >= __instance.hitTime))
                {
                    __result = true;
                    return false;
                }
                if (__instance.player.input.anyLow && (lowTime == null || lowTime >= __instance.hitTime))
                {
                    __result = true;
                    return false;
                }
                break;
            case Height.Top:
                if (__instance.player.input.anyTop && (midTime == null || midTime >= __instance.hitTime))
                {
                    __result = true;
                    return false;
                }
                break;
            }
            __result = false;
            return false;
		}
    }

    [HarmonyPatch(typeof(HighScoreList))]
    [HarmonyPatch("IsScoreSaveable")]
    class HighScoreListPatches
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}