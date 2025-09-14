using HarmonyLib;
using InControl;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace KeyboardToolRebinds.Patches
{
    [HarmonyPatch(typeof(HeroController))]
    public class HeroController_Patches
    {
        [HarmonyPatch("GetWillThrowTool")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GetWillThrowTool_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var isPressedMethod = AccessTools.PropertyGetter(typeof(OneAxisInputControl), "IsPressed");
            var matcher = new CodeMatcher(instructions);

            // Find the first call to IsPressed (which should be for the 'Up' action) and insert our delegate.
            matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, isPressedMethod))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HeroController_Patches), nameof(GetUpPressed))));

            // Find the second call to IsPressed (which should be for the 'Down' action) and insert our delegate.
            matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, isPressedMethod))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HeroController_Patches), nameof(GetDownPressed))));

            return matcher.InstructionEnumeration();
        }

        public static bool GetUpPressed(bool oldValue)
        {
            if (ManagerSingleton<InputHandler>.Instance.inputActions.LastDeviceClass == InputDeviceClass.Keyboard)
            {
                return Input.GetKey(Plugin.ToolUpKey.Value);
            }
            return oldValue;
        }

        public static bool GetDownPressed(bool oldValue)
        {
            if (ManagerSingleton<InputHandler>.Instance.inputActions.LastDeviceClass == InputDeviceClass.Keyboard)
            {
                return Input.GetKey(Plugin.ToolDownKey.Value);
            }
            return oldValue;
        }
    }
}
