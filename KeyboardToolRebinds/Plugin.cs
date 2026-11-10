// SPDX-License-Identifier: GPL-3.0-or-later
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace KeyboardToolRebinds
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ConfigEntry<KeyCode> ToolUpKey;
        public static ConfigEntry<KeyCode> ToolDownKey;
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> BlockOriginalInput;
        public static ConfigEntry<bool> HoldToRepeat;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded");
            ModEnabled = Config.Bind("General", "ModEnabled", true, "Turn the mod on or off. When off, the game behaves exactly like vanilla. Takes effect immediately.");
            ToolUpKey = Config.Bind("Keybinds", "ToolUp", KeyCode.E, "Key that uses the Up tool. Avoid keys the game already uses (vanilla keyboard defaults use Z, X, C, A, S, D, F, V, Tab, I, M, J, Q, T and the arrow keys).");
            ToolDownKey = Config.Bind("Keybinds", "ToolDown", KeyCode.R, "Key that uses the Down tool. Avoid keys the game already uses (vanilla keyboard defaults use Z, X, C, A, S, D, F, V, Tab, I, M, J, Q, T and the arrow keys).");
            BlockOriginalInput = Config.Bind("Settings", "BlockOriginalInput", true, "Block the original Up+Cast and Down+Cast combinations. Set to false to allow both systems.");
            HoldToRepeat = Config.Bind("Settings", "HoldToRepeat", false, "Holding a tool key keeps using that attack tool as fast as the game allows (normal cooldowns still apply). Silk skills are not repeated. Tools that already act while held (like Rosary Cannon) keep their normal hold behaviour.");

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}