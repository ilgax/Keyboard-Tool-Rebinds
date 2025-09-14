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

        private void Awake()
        {
            Log = Logger;
            ToolUpKey = Config.Bind("Keybinds", "ToolUp", KeyCode.Z, "The key to replace the 'Up' action for tools.");
            ToolDownKey = Config.Bind("Keybinds", "ToolDown", KeyCode.X, "The key to replace the 'Down' action for tools.");

            new Harmony("com.ilgax.keyboardtoolrebinds").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}