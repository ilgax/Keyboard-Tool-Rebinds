using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace KeyboardToolRebinds
{
    [BepInPlugin("com.ilgax.keyboardtoolrebinds", "KeyboardToolRebinds", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ConfigEntry<KeyCode> ToolUpKey;
        public static ConfigEntry<KeyCode> ToolDownKey;
        public static ConfigEntry<bool> BlockOriginalInput;
        public static ConfigEntry<bool> EnableHoldMode;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Plugin Awake method called");
            ToolUpKey = Config.Bind("Keybinds", "ToolUp", KeyCode.Z, "The key to replace the 'Up' action for tools.");
            ToolDownKey = Config.Bind("Keybinds", "ToolDown", KeyCode.X, "The key to replace the 'Down' action for tools.");
            BlockOriginalInput = Config.Bind("Settings", "BlockOriginalInput", true, "Block the original Up+Cast and Down+Cast combinations. Set to false to allow both systems.");
            EnableHoldMode = Config.Bind("Settings", "EnableHoldMode", false, "Enable hold mode: hold Z/X keys to continuously use tools (required for some charging tools).");

            new Harmony("com.ilgax.keyboardtoolrebinds").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
