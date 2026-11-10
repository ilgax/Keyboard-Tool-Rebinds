# Keyboard Tool Rebinds

A Hollow Knight: Silksong mod that lets you use your tools with their custom keys, instead of holding a direction while pressing the tool button.

* **Holding works:** held tools and held silk skills keep going while you hold the key, exactly like holding Quick Cast.
* **Vanilla rules:** cooldowns, ammo, silk cost and input buffering all behave like the base game.
* **Menu safe:** presses in menus, dialogs, shops and the pause screen are ignored.
* **Optional hold-to-repeat** for attack tools.
* **Matching prompts:** on keyboard, the tool menu shows your key (e.g. `\[E] Use`) instead of the vanilla direction + button combo.
* **Key conflict warning:** if a tool key is also one of the game's keys (e.g. Jump), the mod logs a warning to `BepInEx/LogOutput.log`.

## Installation

Requires [BepInEx](https://www.nexusmods.com/hollowknightsilksong/mods/26).

* **Vortex:** on the [Nexus Mods page](https://www.nexusmods.com/hollowknightsilksong/mods/174), click **Mod Manager Download** and enable the mod in Vortex.
* **Thunderstore mod managers** (r2modman, Gale): search for Keyboard Tool Rebinds and install. BepInEx is installed automatically.
* **Manually:** download the zip from Nexus Mods and extract it into your Silksong folder, so `KeyboardToolRebinds.dll`
  ends up in `<Silksong folder>/BepInEx/plugins/KeyboardToolRebinds/`.

## Config

Edit `BepInEx/config/com.ilgax.keyboardtoolrebinds.cfg` (created on first launch), or change settings in-game with
BepInEx ConfigurationManager (opened with F1) if you have it installed. Changes apply immediately.

|Setting|Default|Description|
|-|-|-|
|`ModEnabled`|`true`|Turn the mod on or off. When off, the game behaves exactly like vanilla.|
|`ToolUp`|`E`|Key for the Up tool. Must be a [Unity KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html).|
|`ToolDown`|`R`|Key for the Down tool.|
|`BlockOriginalInput`|`true`|Up + Quick Cast and Down + Quick Cast use the neutral tool instead. Set to `false` to keep the vanilla direction combos too.|
|`HoldToRepeat`|`false`|Hold a tool key to keep using that attack tool as fast as the game allows.|

## Controllers

The mod listens for keyboard keys. To use it on a controller, map spare buttons to your tool keys
with Steam Input or JoyToKey.

## License

Keyboard Tool Rebinds is free software: you can redistribute it and/or modify it under the terms of the
[GNU General Public License](LICENSE) as published by the Free Software Foundation, either version 3 of the License,
or (at your option) any later version.
