# Chrono Warden

A highly replayable special role plugin for SCP: Secret Laboratory, built on **LabAPI 1.1.7**.

Chrono Warden spawns randomly from Class-D personnel, featuring an independent energy system, three active abilities, kill-based progression, and a max-level death-reversal mechanic. The plugin supports hot-reloading configuration and refreshing the round's role via the Remote Admin panel without restarting the server.

## Compatibility

| Item | Version |
| --- | --- |
| Plugin Version | 1.0.0 |
| LabAPI | 1.1.7 |
| Target Framework | .NET Framework 4.8 |
| Test Server | SCP:SL Dedicated Server (local server files) |

> API incompatibilities may arise after SCP:SL or LabAPI updates. Back up the plugin and its configuration before upgrading the server.

## Gameplay

### Base Attributes

- Spawns randomly from Class-D personnel after the round starts, with a configurable probability.
- At most 1 Warden per round by default.
- Starts with 135 HP max health and 50 energy.
- Energy regenerates automatically; kills grant bonus energy.
- Gains one level for every 2 kills, up to level 3.
- Leveling up increases max health, energy regeneration rate, and ability effectiveness.

### Controls

1. Press `Alt` to cycle through abilities.
2. You can also use `.cwcycle` in the in-game client console to switch abilities.
3. Throw the role's special coin to cast the currently selected ability.
4. The HUD hint on the right side of the screen shows your level, energy, kill count, and current ability.

### Active Abilities

#### Phase Shield

Spend energy to gain temporary AHP. Upgraded levels reduce the cost and increase the shield amount, making it useful for breaking through or absorbing burst damage from SCPs.

#### Temporal Pulse

Release a radial pulse that:

- Heals human players within range.
- Damages SCPs within range.
- Increased healing and damage at higher levels.

#### Time Rewind

Return to your position and health from roughly 8 seconds ago, useful for undoing mispositioning, escaping pursuers, or restoring combat status.

### Max-Level Passive: Refuse Death

Upon reaching level 3 with 100 energy, your first death automatically reverts you to a past state. This can only trigger once per round, and it consumes all energy upon triggering.

## Remote Admin Commands

Commands require the `Players Management` permission.

| Command | Effect |
| --- | --- |
| `cw reload` | Hot-reload configuration without restarting the server |
| `cw refresh` | Clear role runtime state and respawn the role according to the current configuration |
| `cw give <PlayerID>` | Set the specified Class-D as Chrono Warden |
| `cw remove <PlayerID>` | Remove the special role from the specified player |
| `cw list` | View current role, level, energy, kills, and ability |

Both the full command name `chronowarden` and the alias `cw` are available.

## Installation

1. Make sure LabAPI 1.1.7 is properly installed and enabled on the server.
2. Download the latest `ChronoWarden-v1.0.0.zip` from the Gitee Release page.
3. Extract and place `ChronoWarden.dll` into the LabAPI plugin directory:
   - Global plugins: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global`
   - Single-port plugins: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\<server port>`
4. If the server environment lacks `YamlDotNet.dll`, place the file from the release package into the LabAPI dependencies directory; LabAPI usually bundles this dependency already.
5. Start the server. LabAPI will generate the plugin configuration file on first load.
6. Confirm that `Chrono Warden v1.0.0 enabled` appears in the server logs.

## Configuration

The configuration file supports the following main options:

- Plugin enabled status, per-round role limit, and spawn probability.
- Role max health and energy regeneration rate.
- Kill rewards and number of kills required to level up.
- Cost, power, range, rewind duration, and cooldowns for the three abilities.
- Max-level death-reversal toggle.
- Spawn broadcast duration and debug log toggle.

After modifying the configuration, run `cw reload` in Remote Admin to apply it immediately. Run `cw refresh` to respawn the round's role under the new configuration.

## Building from Source

You need the .NET SDK and the managed assemblies of an SCP:SL Dedicated Server.

```powershell
$env:SL_REFERENCES = "C:\Program Files (x86)\Steam\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed"
$env:UNITY_REFERENCES = $env:SL_REFERENCES
dotnet restore .\ChronoWarden\ChronoWarden.csproj
dotnet build .\ChronoWarden\ChronoWarden.csproj -c Release
```

The compiled output is located at:

```text
ChronoWarden\bin\Release\net48\ChronoWarden.dll
```

## FAQ

### Throwing the coin does not cast an ability

Verify that the player is actually a Chrono Warden, threw the role's coin, has enough energy, and the ability is not on cooldown.

### The `cw` command reports insufficient permissions

Add the `Players Management` permission to the admin role.

### No role respawn after modifying the configuration

`cw reload` only re-reads the configuration and updates existing role values; run `cw refresh` when you need a fresh respawn.

### Plugin fails to load after updating the server

Check whether the server's LabAPI version is still 1.1.7-compatible and inspect the server startup logs for assembly or API errors.

## Source Structure

- `ChronoWardenPlugin.cs`: plugin entry point, lifecycle, and hot reload.
- `WardenManager.cs`: role spawning, energy loop, abilities, and progression logic.
- `WardenState.cs`: per-player runtime state and time snapshots.
- `Config.cs`: configurable parameters.
- `Commands/`: Remote Admin commands and the player ability-switching command.

