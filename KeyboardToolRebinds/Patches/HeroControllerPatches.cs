// SPDX-License-Identifier: GPL-3.0-or-later
using HarmonyLib;
using InControl;
using UnityEngine;

namespace KeyboardToolRebinds
{
    /// <summary>
    /// Tracks which custom tool key (if any) is currently driving the Quick Cast action.
    /// </summary>
    internal static class ToolKeyState
    {
        // Binding currently driving Quick Cast; null when no custom key is active.
        public static AttackToolBinding? ActiveBinding;

        // A key only counts once it was pressed while the player could act.
        // Presses made in menus, dialogs, shops or the pause screen are ignored entirely,
        // so they never queue up tools or leak through when the menu closes.
        private static bool upArmed;
        private static bool downArmed;

        private static int lastUpdatedFrame = -1;

        public static void Refresh()
        {
            // Input can be polled more than once per frame; only sample once.
            if (lastUpdatedFrame == Time.frameCount) return;
            lastUpdatedFrame = Time.frameCount;

            if (!Plugin.ModEnabled.Value)
            {
                upArmed = downArmed = false;
                ActiveBinding = null;
                return;
            }

            KeyConflictChecker.CheckIfNeeded();

            KeyCode upKey = Plugin.ToolUpKey.Value;
            KeyCode downKey = Plugin.ToolDownKey.Value;
            bool canStart = CanStartToolInput();

            // Arm on a fresh press during gameplay; disarm as soon as the key is let go.
            // An armed key stays armed while held even if the hero loses control
            // (held tools take control of the hero themselves).
            if (Input.GetKeyDown(upKey) && canStart) upArmed = true;
            if (Input.GetKeyDown(downKey) && canStart) downArmed = true;
            if (!Input.GetKey(upKey)) upArmed = false;
            if (!Input.GetKey(downKey)) downArmed = false;

            // Mirror vanilla direction handling: Quick Cast stays held while either key is held,
            // and the slot follows the keys exactly like holding Up/Down does (Up wins over Down).
            // Pressing the other key mid-hold therefore behaves like changing direction mid-hold.
            ActiveBinding = upArmed ? AttackToolBinding.Up
                : downArmed ? AttackToolBinding.Down
                : (AttackToolBinding?)null;
        }

        internal static bool CanStartToolInput()
        {
            HeroController hero = HeroController.SilentInstance;
            if (hero == null) return false;

            InputHandler input = ManagerSingleton<InputHandler>.Instance;
            if (input == null || !input.acceptingInput) return false;

            // CanInput covers pause, inventory, scene transitions, respawns and hazard deaths.
            // controlReqlinquished / IsInputBlocked cover NPC dialogs, shops and pop-up menus.
            return hero.CanInput() && !hero.controlReqlinquished && !hero.IsInputBlocked();
        }
    }

    /// <summary>
    /// Feeds the custom keys into the game's Quick Cast action, so every system that reads
    /// QuickCast (hero tool throw, held tool FSMs, held silk skills) sees a real press/hold/release.
    /// </summary>
    [HarmonyPatch(typeof(PlayerAction), "UpdateBindings")]
    internal static class PlayerAction_UpdateBindings_Patch
    {
        private static void Prefix(PlayerAction __instance, ulong updateTick, float deltaTime)
        {
            if (!(__instance.Owner is HeroActions heroActions) || !ReferenceEquals(__instance, heroActions.QuickCast))
                return;

            ToolKeyState.Refresh();

            if (ToolKeyState.ActiveBinding.HasValue)
                __instance.UpdateWithState(true, updateTick, deltaTime);
        }
    }

    [HarmonyPatch(typeof(HeroController))]
    internal static class HeroController_Patches
    {
        private static readonly AccessTools.FieldRef<HeroController, InputHandler> InputHandlerRef =
            AccessTools.FieldRefAccess<HeroController, InputHandler>("inputHandler");

        /// <summary>
        /// Chooses which tool Quick Cast will use. Called on press and every frame while a
        /// held tool is active, so the choice must stay stable for the whole hold.
        /// </summary>
        [HarmonyPatch(nameof(HeroController.GetWillThrowTool))]
        [HarmonyPrefix]
        private static bool GetWillThrowTool_Prefix(HeroController __instance, bool reportFailure,
            ref ToolItem ___willThrowTool, ref bool __result)
        {
            if (!Plugin.ModEnabled.Value) return true; // Mod turned off: pure vanilla.

            AttackToolBinding binding;

            if (ToolKeyState.ActiveBinding.HasValue)
            {
                binding = ToolKeyState.ActiveBinding.Value;
            }
            else if (Plugin.BlockOriginalInput.Value && IsDirectionHeld(__instance))
            {
                // Vanilla Up/Down + Cast is disabled: always use the neutral tool.
                binding = AttackToolBinding.Neutral;
            }
            else
            {
                return true; // Vanilla behaviour.
            }

            ___willThrowTool = ToolItemManager.GetBoundAttackTool(binding, ToolEquippedReadSource.Active, out AttackToolBinding usedBinding);
            __result = ___willThrowTool != null && CanThrowTool(__instance, ___willThrowTool, usedBinding, reportFailure);
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch("CanThrowTool", typeof(ToolItem), typeof(AttackToolBinding), typeof(bool))]
        private static bool CanThrowTool(HeroController instance, ToolItem tool, AttackToolBinding binding, bool reportFailure)
        {
            throw new System.NotImplementedException("Stub replaced by Harmony reverse patch");
        }

        [HarmonyReversePatch]
        [HarmonyPatch("ThrowTool", typeof(bool))]
        private static void ThrowTool(HeroController instance, bool isAutoThrow)
        {
            throw new System.NotImplementedException("Stub replaced by Harmony reverse patch");
        }

        /// <summary>
        /// Optional hold-to-repeat: while a custom key stays held, keep using the bound attack
        /// tool whenever the game would accept a fresh Quick Cast press. Every vanilla check
        /// (cooldown, attack recovery, ammo, control state) still applies.
        /// </summary>
        [HarmonyPatch("LookForInput")]
        [HarmonyPostfix]
        private static void LookForInput_Postfix(HeroController __instance, ref ToolItem ___willThrowTool)
        {
            if (!Plugin.HoldToRepeat.Value || !ToolKeyState.ActiveBinding.HasValue) return;

            InputHandler handler = InputHandlerRef(__instance);
            if (handler == null || handler.inputActions == null) return;

            // The first press is handled by vanilla; only repeat on later frames of the hold.
            PlayerAction quickCast = handler.inputActions.QuickCast;
            if (!quickCast.IsPressed || quickCast.WasPressed) return;

            // Let the current throw animation finish so repeats are paced like normal tapping.
            if (__instance.cState.isToolThrowing) return;
            if (!ToolKeyState.CanStartToolInput()) return;

            // CanThrowTool(false) = cooldown ready + can attack; GetWillThrowTool(false) checks ammo/silk silently.
            if (!__instance.CanThrowTool(false) || !__instance.GetWillThrowTool(false)) return;

            // Only repeat attack (red) tools; silk skills keep their one-press behaviour.
            if (___willThrowTool == null || ___willThrowTool.Type != ToolItemType.Red) return;

            ThrowTool(__instance, false);
        }

        private static bool IsDirectionHeld(HeroController hero)
        {
            InputHandler handler = InputHandlerRef(hero);
            if (handler == null || handler.inputActions == null) return false;
            return handler.inputActions.Up.IsPressed || handler.inputActions.Down.IsPressed;
        }
    }
}
