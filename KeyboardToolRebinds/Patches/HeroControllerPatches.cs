using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace KeyboardToolRebinds
{
    // Patch HeroController to directly handle our custom keys
    [HarmonyPatch(typeof(HeroController))]
    public class HeroController_DirectPatches
    {
        private static FieldInfo willThrowToolField;
        private static MethodInfo canThrowToolMethod;
        private static MethodInfo throwToolMethod;

        [HarmonyPatch("LookForInput")]
        [HarmonyPostfix]
        private static void LookForInput_Postfix(HeroController __instance)
        {
            // Check for our custom keys
            if (Input.GetKeyDown(Plugin.ToolUpKey.Value))
            {
                TryUseToolDirectly(__instance, AttackToolBinding.Up);
            }

            if (Input.GetKeyDown(Plugin.ToolDownKey.Value))
            {
                TryUseToolDirectly(__instance, AttackToolBinding.Down);
            }
        }

        private static void TryUseToolDirectly(HeroController heroController, AttackToolBinding binding)
        {
            try
            {
                // Get the tool for this binding
                AttackToolBinding usedBinding;
                var tool = ToolItemManager.GetBoundAttackTool(binding, ToolEquippedReadSource.Active, out usedBinding);

                if (tool == null) return; // No tool available

                // Set willThrowTool field using reflection
                if (willThrowToolField == null)
                {
                    willThrowToolField = typeof(HeroController).GetField("willThrowTool", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (willThrowToolField == null) return; // Field not found

                willThrowToolField.SetValue(heroController, tool);

                // Get private method references
                if (canThrowToolMethod == null)
                {
                    canThrowToolMethod = typeof(HeroController).GetMethod("CanThrowTool", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (throwToolMethod == null)
                {
                    throwToolMethod = typeof(HeroController).GetMethod("ThrowTool", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (canThrowToolMethod == null || throwToolMethod == null) return; // Methods not found

                // Check if we can throw this tool and do it
                bool canThrow = (bool)canThrowToolMethod.Invoke(heroController, new object[] { tool, usedBinding, true });

                if (canThrow)
                {
                    throwToolMethod.Invoke(heroController, new object[] { false }); // Use the tool
                }
            }
            catch (System.Exception ex)
            {
                // Only log actual errors
                Plugin.Log.LogError($"Error using tool: {ex.Message}");
            }
        }
    }
}



