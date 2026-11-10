using System;
using System.Collections.Generic;
using InControl;
using UnityEngine;

namespace KeyboardToolRebinds
{
    /// <summary>
    /// Logs a warning when a tool key is also bound to a gameplay action in the game's keyboard
    /// controls (for example the old Z/X defaults, which are vanilla Jump/Attack).
    /// Re-checks whenever the tool keys or the game's keyboard bindings change.
    /// </summary>
    internal static class KeyConflictChecker
    {
        // Menu-only, right-stick and inventory-paging actions can't clash during gameplay.
        private static readonly HashSet<string> IgnoredActions = new HashSet<string>
        {
            "Submit", "Cancel", "Menu Extra", "Menu Super",
            "RS_Up", "RS_Down", "RS_Left", "RS_Right",
            "Pane Right", "Pane Left",
            "swipeInventoryMap", "swipeInventoryJournal", "swipeInventoryTools", "swipeInventoryQuests",
        };

        private const float CheckInterval = 2f;

        private static string lastSignature;
        private static float nextCheckTime;

        public static void CheckIfNeeded()
        {
            // Bindings rarely change; a slow poll keeps this off the per-frame path.
            if (Time.unscaledTime < nextCheckTime) return;
            nextCheckTime = Time.unscaledTime + CheckInterval;

            InputHandler input = ManagerSingleton<InputHandler>.Instance;
            HeroActions actions = input != null ? input.inputActions : null;
            if (actions == null) return;

            KeyCode upKey = Plugin.ToolUpKey.Value;
            KeyCode downKey = Plugin.ToolDownKey.Value;

            // Cheap signature of everything the result depends on; only re-check when it changes.
            string signature = upKey + "|" + downKey + "|" + BindingsSignature(actions);
            if (signature == lastSignature) return;
            lastSignature = signature;

            var warnings = new List<string>();
            if (upKey == downKey && upKey != KeyCode.None)
                warnings.Add($"ToolUp and ToolDown are both set to {upKey}; only the Up tool will be used.");
            AddConflicts(warnings, "ToolUp", upKey, actions);
            AddConflicts(warnings, "ToolDown", downKey, actions);

            foreach (string warning in warnings)
                Plugin.Log.LogWarning(warning);
        }

        private static void AddConflicts(List<string> warnings, string setting, KeyCode keyCode, HeroActions actions)
        {
            if (!TryToInControlKey(keyCode, out Key key)) return;

            foreach (PlayerAction action in actions.Actions)
            {
                if (IgnoredActions.Contains(action.Name)) continue;
                foreach (BindingSource binding in action.Bindings)
                {
                    if (binding is KeyBindingSource keyBinding && Uses(keyBinding.Control, key))
                    {
                        warnings.Add($"{setting} = {keyCode} is also the game's \"{action.Name}\" key. " +
                                     "Pressing it will do both. Change the key in the mod config or in the game's keyboard settings.");
                        break;
                    }
                }
            }
        }

        private static bool Uses(KeyCombo combo, Key key)
        {
            for (int i = 0; i < combo.IncludeCount; i++)
                if (combo.GetInclude(i) == key) return true;
            return false;
        }

        private static string BindingsSignature(HeroActions actions)
        {
            var parts = new List<string>();
            foreach (PlayerAction action in actions.Actions)
            {
                if (IgnoredActions.Contains(action.Name)) continue;
                foreach (BindingSource binding in action.Bindings)
                    if (binding is KeyBindingSource keyBinding)
                        parts.Add(action.Name + "=" + keyBinding.Control);
            }
            return string.Join(",", parts);
        }

        /// <summary>Maps a Unity KeyCode to InControl's Key enum (names mostly match).</summary>
        internal static bool TryToInControlKey(KeyCode keyCode, out Key key)
        {
            string name = keyCode.ToString();
            if (name.StartsWith("Alpha") && name.Length == 6) name = "Key" + name.Substring(5); // Alpha1 -> Key1
            else if (name.StartsWith("Keypad") && name.Length == 7) name = "Pad" + name.Substring(6); // Keypad1 -> Pad1
            return Enum.TryParse(name, out key) && key != Key.None;
        }
    }
}
