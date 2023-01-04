using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace MutePlus
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static HashSet<ulong> MutedPlayers;
        public static string muteFile;
        public override void Load()
        {
            MutedPlayers = new HashSet<ulong>();
            muteFile = Application.dataPath + "\\MutedPlayers.txt";
            if (!File.Exists(muteFile)){
                File.Create(muteFile);
            }
            foreach (string s in File.ReadAllLines(muteFile)) {
                MutedPlayers.Add(ulong.Parse(s));
            }
            Harmony.CreateAndPatchAll(typeof(Plugin));
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        [HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.SetPlayer))]
        [HarmonyPostfix]
        public static void onPlayerManagerAssigned(PlayerManager __instance, ulong __0, int __1, bool __2){
            if (MutedPlayers.Contains(__0)){
                LobbyManager.Instance.mutedPlayers[__0] = true;
                __instance.username = "[MUTED]";
                __instance.playerName.SetName("[MUTED]");
                __instance.UpdateForceMute();
            }
        }
        [HarmonyPatch(typeof(ManagePlayerListing),nameof(ManagePlayerListing.MutePlayer))]
        [HarmonyPostfix]
        public static void OnPlayerMuted(ManagePlayerListing __instance){
            // this method is actually a toggle so 
            // if muted:
            if (LobbyManager.Instance.mutedPlayers[__instance.field_Private_UInt64_0]){
                if (!MutedPlayers.Contains(__instance.field_Private_UInt64_0)){
                    File.AppendAllLines(muteFile, new string[]{__instance.field_Private_UInt64_0.ToString()});
                }
                MutedPlayers.Add(__instance.field_Private_UInt64_0);
            } else {// if unmuted:
                MutedPlayers.Remove(__instance.field_Private_UInt64_0);
                var content = new List<string>(File.ReadAllLines(muteFile));
                content.Remove(__instance.field_Private_UInt64_0.ToString());
                File.WriteAllLines(muteFile,content);
            }
        }
        [HarmonyPatch(typeof(ChatBox),nameof(ChatBox.AppendMessage))]
        [HarmonyPrefix]
        public static bool OnReceiveMessage(ChatBox __instance, ulong __0, string __1, string __2){
            if (MutedPlayers.Contains(__0)) {
                return false;
            }
            return true;
        }
    }
}
