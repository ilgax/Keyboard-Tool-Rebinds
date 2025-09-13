using HarmonyLib;
using KeyboardToolRebinds;
using UnityEngine;

namespace KeyboardToolRebinds.Patches
{
    [HarmonyPatch(typeof(HeroController))]
    public class HeroControllerPatches
    {
        [HarmonyPatch("GetWillThrowTool")]
        [HarmonyPostfix]
        private static void GetWillThrowTool_Postfix(ref bool __result)
        {
            // If the game already determined the tool should be thrown, we don't need to do anything.
            if (__result)
            {
                return;
            }

            bool toolUpPressed = Plugin.ToolUpKey.Value != KeyCode.None && Input.GetKeyDown(Plugin.ToolUpKey.Value);
            bool toolDownPressed = Plugin.ToolDownKey.Value != KeyCode.None && Input.GetKeyDown(Plugin.ToolDownKey.Value);

            if (toolUpPressed || toolDownPressed)
            {
                __result = true;
            }
        }
    }
}