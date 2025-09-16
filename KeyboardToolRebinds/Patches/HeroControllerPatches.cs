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

        // Block the original GetWillThrowTool method only for directional inputs
        [HarmonyPatch("GetWillThrowTool")]
        [HarmonyPrefix]
        private static bool GetWillThrowTool_Prefix(HeroController __instance, ref bool __result)
        {
            // Only block if the setting is enabled
            if (Plugin.BlockOriginalInput.Value)
            {
                try
                {
                    // Get the inputHandler field
                    var inputHandlerField = typeof(HeroController).GetField("inputHandler", 
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (inputHandlerField != null)
                    {
                        var inputHandler = inputHandlerField.GetValue(__instance);

                        // Try property first, then field
                        System.Reflection.MemberInfo inputActionsMember = inputHandler.GetType().GetProperty("inputActions");
                        if (inputActionsMember == null)
                        {
                            inputActionsMember = inputHandler.GetType().GetField("inputActions");
                        }

                        if (inputActionsMember != null)
                        {
                            object inputActions = null;

                            if (inputActionsMember is System.Reflection.PropertyInfo prop)
                            {
                                inputActions = prop.GetValue(inputHandler);
                            }
                            else if (inputActionsMember is System.Reflection.FieldInfo field)
                            {
                                inputActions = field.GetValue(inputHandler);
                            }

                            if (inputActions is HeroActions heroActions)
                            {
                                // Check if Up or Down is pressed
                                bool upPressed = heroActions.Up.IsPressed;
                                bool downPressed = heroActions.Down.IsPressed;

                                // If directional input is being used, redirect to neutral cast
                                if (upPressed || downPressed)
                                {
                                    // Instead of blocking, use neutral cast as fallback
                                    AttackToolBinding usedBinding;
                                    var neutralTool = ToolItemManager.GetBoundAttackTool(AttackToolBinding.Neutral, ToolEquippedReadSource.Active, out usedBinding);

                                    if (neutralTool != null)
                                    {
                                        // Set willThrowTool to neutral tool
                                        if (willThrowToolField == null)
                                        {
                                            willThrowToolField = typeof(HeroController).GetField("willThrowTool", 
                                                BindingFlags.NonPublic | BindingFlags.Instance);
                                        }

                                        if (willThrowToolField != null)
                                        {
                                            willThrowToolField.SetValue(__instance, neutralTool);

                                            // Check if we can use this neutral tool
                                            if (canThrowToolMethod == null)
                                            {
                                                canThrowToolMethod = typeof(HeroController).GetMethod("CanThrowTool", 
                                                    BindingFlags.NonPublic | BindingFlags.Instance);
                                            }

                                            if (canThrowToolMethod != null)
                                            {
                                                bool canThrow = (bool)canThrowToolMethod.Invoke(__instance, new object[] { neutralTool, usedBinding, true });
                                                __result = canThrow;
                                                return false; // Skip original method, use our result
                                            }
                                        }
                                    }

                                    // If neutral tool setup failed, just block
                                    __result = false;
                                    return false;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // If reflection fails, don't block anything
                }
            }

            // Allow original method to run (for neutral cast or if blocking is disabled)
            return true;
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



