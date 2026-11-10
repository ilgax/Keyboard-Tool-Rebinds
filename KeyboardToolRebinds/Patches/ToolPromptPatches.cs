// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using GlobalEnums;
using HarmonyLib;
using InControl;
using UnityEngine;

namespace KeyboardToolRebinds
{
    /// <summary>
    /// Replaces the tool menu's "Up/Down + Quick Cast" use prompt with the mod's key
    /// (e.g. "[E] Use") while the player is using keyboard. Controller prompts stay vanilla.
    /// </summary>
    [HarmonyPatch]
    internal static class ToolPromptPatches
    {
        private static readonly AccessTools.FieldRef<InventoryItemComboButtonPromptDisplay, GameObject> ParentWithModifier =
            AccessTools.FieldRefAccess<InventoryItemComboButtonPromptDisplay, GameObject>("parentWithModifier");
        private static readonly AccessTools.FieldRef<InventoryItemComboButtonPromptDisplay, GameObject> ParentWithoutModifier =
            AccessTools.FieldRefAccess<InventoryItemComboButtonPromptDisplay, GameObject>("parentWithoutModifier");
        private static readonly AccessTools.FieldRef<InventoryItemComboButtonPromptDisplay, ActionButtonIcon> IconWithoutModifier =
            AccessTools.FieldRefAccess<InventoryItemComboButtonPromptDisplay, ActionButtonIcon>("actionButtonWithoutModifier");

        // Last Up/Down Quick Cast prompt shown by each display, so it can be redrawn on device switch.
        private static readonly Dictionary<InventoryItemComboButtonPromptDisplay, InventoryItemComboButtonPromptDisplay.Display> Shown =
            new Dictionary<InventoryItemComboButtonPromptDisplay, InventoryItemComboButtonPromptDisplay.Display>();

        // While set, the next keyboard skin lookup returns this key instead of the Quick Cast key.
        private static string keySkinOverride;

        private static InputHandler subscribedHandler;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryItemComboButtonPromptDisplay), nameof(InventoryItemComboButtonPromptDisplay.Show))]
        private static void Show_Postfix(InventoryItemComboButtonPromptDisplay __instance,
            InventoryItemComboButtonPromptDisplay.Display display)
        {
            bool isToolCombo = display.ActionButton == HeroActionButton.QUICK_CAST &&
                               (display.DirectionModifier == AttackToolBinding.Up || display.DirectionModifier == AttackToolBinding.Down);
            if (!isToolCombo)
            {
                Shown.Remove(__instance);
                return;
            }

            Shown[__instance] = display;
            SubscribeToDeviceSwitch();

            if (!Plugin.ModEnabled.Value || !IsKeyboardActive()) return; // Vanilla prompt stays as drawn.

            KeyCode keyCode = display.DirectionModifier == AttackToolBinding.Up
                ? Plugin.ToolUpKey.Value
                : Plugin.ToolDownKey.Value;
            if (!KeyConflictChecker.TryToInControlKey(keyCode, out Key key)) return;

            // Use the single-button layout (no arrow, no "+") and draw our key on its keycap.
            ParentWithModifier(__instance).SetActive(false);
            ParentWithoutModifier(__instance).SetActive(true);

            keySkinOverride = key.ToString();
            try
            {
                IconWithoutModifier(__instance).SetAction(HeroActionButton.QUICK_CAST);
            }
            finally
            {
                keySkinOverride = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryItemComboButtonPromptDisplay), nameof(InventoryItemComboButtonPromptDisplay.Hide))]
        private static void Hide_Postfix(InventoryItemComboButtonPromptDisplay __instance) => Shown.Remove(__instance);

        /// <summary>Swaps in the mod key's keycap while the prompt icon is being drawn.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIButtonSkins), nameof(UIButtonSkins.GetButtonSkinFor), typeof(HeroActionButton))]
        private static bool GetButtonSkinFor_Prefix(UIButtonSkins __instance, ref ButtonSkin __result)
        {
            if (keySkinOverride == null) return true;
            __result = KeyboardSkin(__instance, keySkinOverride);
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(UIButtonSkins), "GetButtonSkinFor", typeof(string))]
        private static ButtonSkin KeyboardSkin(UIButtonSkins instance, string buttonName)
        {
            throw new System.NotImplementedException("Stub replaced by Harmony reverse patch");
        }

        /// <summary>Same rule the game uses to pick keyboard or controller button icons.</summary>
        private static bool IsKeyboardActive()
        {
            InputHandler handler = ManagerSingleton<InputHandler>.Instance;
            if (handler == null) return true;
            switch (handler.lastActiveController)
            {
                case BindingSourceType.None:
                case BindingSourceType.KeyBindingSource:
                case BindingSourceType.MouseBindingSource:
                    return true;
                default:
                    return false;
            }
        }

        private static void SubscribeToDeviceSwitch()
        {
            InputHandler handler = ManagerSingleton<InputHandler>.Instance;
            if (handler == null || ReferenceEquals(handler, subscribedHandler)) return;
            if (subscribedHandler != null) subscribedHandler.RefreshActiveControllerEvent -= RedrawShownPrompts;
            handler.RefreshActiveControllerEvent += RedrawShownPrompts;
            subscribedHandler = handler;
        }

        /// <summary>Redraws visible tool prompts when switching between keyboard and controller.</summary>
        private static void RedrawShownPrompts()
        {
            var displays = new List<InventoryItemComboButtonPromptDisplay>(Shown.Keys);
            foreach (InventoryItemComboButtonPromptDisplay display in displays)
            {
                if (display == null)
                {
                    Shown.Remove(display);
                    continue;
                }
                if (display.gameObject.activeInHierarchy)
                    display.Show(Shown[display]);
            }
        }
    }
}
