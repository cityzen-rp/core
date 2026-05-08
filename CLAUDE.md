# CLAUDE.md

A Garry's-Mod-style multiplayer sandbox built on **s&box** (Facepunch). C# game project, scene-and-component based, multiplayer-by-default (32 max, 50 tick).

## Reading s&box docs

The s&box doc site (`sbox.game/dev/doc/...`) is JS-rendered, so plain HTTP fetches return an empty shell. **Append `.md` to any doc URL to get the raw markdown** — that's the canonical way for Claude to read the docs:

- Index of every section: <https://sbox.game/llms.txt>
- Examples:
  - <https://sbox.game/dev/doc/getting-started.md>
  - <https://sbox.game/dev/doc/movie-maker.md>
  - <https://sbox.game/dev/doc/movie-maker/recording.md>
  - <https://sbox.game/dev/doc/movie-maker/recording-api.md>
  - <https://sbox.game/dev/doc/movie-maker/playback-api.md>
  - <https://sbox.game/dev/doc/scene/components/events.md>
  - <https://sbox.game/dev/doc/networking/rpcs.md>

For type/namespace lookups use `https://sbox.game/dev/api/Sandbox.<Type>` and `https://sbox.game/api/all/Sandbox.<Namespace>/` (also JS-rendered, no `.md` variant — use them in-browser, or grep this repo's source for usage examples).

## Project layout

```
sandbox.sbproj   StartupScene = scenes/sandbox.scene  ·  SystemScene = scenes/system.scene
Code/            All gameplay C#
Assets/scenes/   sandbox · system · zoo · npc_testing · tools · sandbox.scene_d ...
Editor/          Editor-side tools
Localization/    String tables
ProjectSettings/ Engine settings
```

`Code/` subfolders:
- **GameLoop/** — `GameManager` (partial: main / `Spawn` / `Achievements` / `Util`), `GamePreferences`, `LimitsSystem`, `ServerSettings`. `GameManager` is the central `INetworkListener` and orchestrates spawn / cleanup / save events.
- **Player/** — `Player` (partial: main / `Ammo` / `Camera` / `ConsoleCommands` / `Undo`), `PlayerData`, observer/flashlight/gibs/stats. `Player` implements `IDamageable`, `PlayerController.IEvents`, `ISaveEvents`, `IKillSource`. **Players are runtime, networked components** — there's no edit-time Player GameObject.
- **Game/Weapon/** — `BaseCarryable` → `BaseWeapon` (partials `.Ammo` / `.Reloading`) → `BaseBulletWeapon` / `MeleeWeapon`. ViewModel/WorldModel split.
- **Weapons/** — concrete weapons (Colt1911, Glock, M4A1, Mp5, Shotgun, Sniper, Rpg, PhysGun, ToolGun, HandGrenade, Crowbar, Vape, Camera, Screen…).
- **Npcs/** — schedule/task AI. `Npc` (partials), `BaseNpcLayer`, `ScheduleBase`, `TaskBase` + concrete schedules under `Combat/`, `Rollermine/`, `Scientist/`.
- **Items/** — `DroppedWeapon`, `Pickups/` (Ammo, Health, Inventory).
- **Spawner/** — `ISpawner` / `ISpawnEvents` and concrete spawners (`PropSpawner`, `EntitySpawner`, `MountSpawner`, `DuplicatorSpawner`) + `SpawnerWeapon`. Add new spawnable kinds here.
- **Save/** — `SaveSystem` and `ISaveEvents`. `Player` and `CleanupSystem` participate; new persistent state should hook here.
- **UI/** — Razor panels and inspector widgets: spawn menu (`ISpawnMenuTab`), utility tabs (`IUtilityTab`), context menus, kill icons (`IKillIcon`), tool info (`IToolInfo`).
- **Map/** — Hammer-side gameplay: `Door`, `Button`, `FuncMover`, `MapPlayerSpawner` (teleports existing `Player` components onto `SpawnPoint`s on map load), `TriggerPush`, `TriggerTeleport`, `BaseToggle`.
- **Game/ControlSystem/** — `ControlSystem`, `ClientInput`, `IPlayerControllable` (anything the player can drive — vehicles, weapons, etc.).
- **Components/** — small mixins / interfaces: `IPhysgunEvent`, `IToolgunEvent`, `Ownable`, `MorphState`, `PhysicalProperties`, `ManualLink`, `ConstraintCleanup`.
- **Cleanup/** — `CleanupSystem` snapshots baseline scene at start and resets to it on demand (without nuking players).
- **FreeCam/** — `FreeCamGameObjectSystem` for spectator/debug camera.
- **Utility/** — `DemoRecording.cs` (`[DefaultMovieRecorderOptions]` hook), `EngineAdditions`, `MetadataAttribute`.
- **Game/Entity/** — scripted entities (Dynamite, Emitter, EntitySpawner, PointLight/SpotLight, TV).
- **Game/** also holds: `BanSystem/`, `PostProcessing/`, `Sound/` (incl. `SoundDefinition`, `IResourcePreview`), `UtilityFunctions/`.

## Conventions

- File-scoped namespaces. Most code lives under `namespace Sandbox;` or a sub-namespace (`Sandbox.Npcs`, etc.).
- Heavy use of `public sealed partial class` to split big classes (`GameManager`, `Player`, `BaseWeapon`, `Npc`) by concern.
- Networking: `[Sync(SyncFlags.FromHost)]` for replicated state, `[Rpc.Broadcast]` / `[Rpc.Host]` for RPCs, `Networking.IsHost` guards for host-only logic, `Connection.Local` to detect the local caller.
- Editor / inspector attributes: `[Property]`, `[Range]`, `[TextArea]`, `[Hide]`, `[Feature("…")]`, `[RequireComponent]`.
- `.editorconfig`: tabs, CRLF, expression-bodied properties preferred. Match it.
- Don't introduce a new top-level folder under `Code/` without a clear reason — extend an existing one.

## Movie Maker / Demo Recording

The project is wired for Movie Maker recording:
- `Sandbox.MovieMaker.MovieRecorderSystem` is registered as a `GameObjectSystem` in `zoo.scene`, `system.scene`, `npc_testing.scene` (with `DisableClientRecording: false`). **It is NOT in `sandbox.scene` or `tools.scene`** — add it via *Scene Settings → Game Object Systems* if recording is needed there.
- [`Code/Utility/DemoRecording.cs`](Code/Utility/DemoRecording.cs) provides a `[DefaultMovieRecorderOptions]` static hook that filters viewmodels and `prefabs/surface/...` from recordings. Extend that filter rather than adding a parallel one.

How recording survives stop/play:
- Movie Maker's recorder stores changes to bound tracks into a `MovieClip` / `.movie` resource. The clip is independent of runtime GameObjects, so when you exit Play mode the recording stays in the movie and can be edited or saved (per the official Recording doc). The user does **not** need the runtime Player to persist — the clip is the data.
- When binding playback to a scene that doesn't have the original target GameObjects, set `MoviePlayer.CreateTargets = true` (default) or call `Binder.CreateTargets(clip)` so the player materialises ghost objects from the clip.
- Recording API surface (Sandbox.MovieMaker): `MovieRecorder` (`Start` / `Stop` / `Advance` / `Capture` / `ToClip`), `MovieRecorderOptions` (filters, sample rate, capture actions, `ComponentCapturer<T>`), `[DefaultMovieRecorderOptions]` attribute, `MovieClip`, `MovieResource`, `MoviePlayer` (component) with `Clip` / `Resource` / `Position` / `Binder`.

## Common gotchas

- `Player` is a runtime networked component, not an edit-time GameObject. Cinematics that need a "persistent character" should use a separate non-networked `PlayerController` GameObject placed in the scene at edit time, not the gameplay `Player`.
- Spawn pipeline goes through `GameManager.Spawn(string ident, string metadata = null)` (RPC.Broadcast, host-executes-only). Idents look like `prop:path`, `entity:path` / `sent:path`, `mount:path`, `dupe.local:id`, `dupe.workshop:id`. Add new spawnable kinds by implementing `ISpawner`.
- `MapPlayerSpawner.RespawnPlayers` queries `Scene.GetAllComponents<Player>()` and teleports them to a random `SpawnPoint`. It does not instantiate Players.
- `CleanupSystem` snapshots the scene baseline before play; if you add a system that creates persistent objects at startup, make sure it runs before the snapshot or it'll be wiped on cleanup.

## Useful upstream

- Repo is open-source MIT (Facepunch/sandbox). PRs welcomed but match style.
- Engine-side staging / Movie Maker development: <https://github.com/Facepunch/sbox-scenestaging>.
- Engine bug tracker: <https://github.com/Facepunch/sbox-issues>.
