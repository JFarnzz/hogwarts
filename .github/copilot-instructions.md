<!-- .github/copilot-instructions.md - Guidance for AI coding agents working on this repository -->
# OpenHogwarts — AI assistant instructions

Purpose: quickly orient an AI agent to be effective editing, navigating and extending this Unity project. These notes focus on concrete, discoverable patterns, important files, and safe developer workflows.

1) Big picture
- This is a Unity game project (root contains `Assets/`, `ProjectSettings/`, `Packages/`). The authoritative runtime and build environment is the Unity Editor. See `ProjectSettings/ProjectVersion.txt` for the tested Unity version.
- Major systems are implemented under `Assets/Scripts/` and wired to Unity scenes & prefabs under `Assets/Scenes/` and `Assets/Prefabs/`.
- Networking uses Photon Unity Networking (PUN) — folder `Assets/Photon Unity Networking/` contains PUN assets and editor helpers. Configure PUN using the Editor: Window → Photon Unity Networking (PUN Wizard).
- Persistent/local data is handled with iBoxDB (search for usages in `Assets/Scripts/*`, e.g. `Assets/Scripts/Util/Service.cs`, `Assets/Scripts/Quest/Task.cs`).

2) Quick developer workflows (what to do, and where)
- Open project: Open the repository folder in Unity (File → Open Project). Recommended entry scene: `Assets/Scenes/MainMenu.unity` (double-click from Project view / use README steps).
- Play / run: Start the Unity Editor and press Play. Many gameplay features depend on scene setup and serialized Prefabs in `Assets/Prefabs/`.
- Build: use Unity's Build Settings. There is no custom build system in the repo; use the ProjectVersion to select a compatible Unity editor.
- Configure networking: run the PUN setup wizard (Window → Photon Unity Networking → PUN Wizard) and provide AppId or local server settings if needed. Photon settings live in `Assets/Photon Unity Networking/Resources/PhotonServerSettings.asset`.

3) Project-specific conventions & patterns
- Hotkeys and input: hotkey mapping is implemented both in code and an InputActions asset. Check `Assets/OpenHogwarts.inputactions` and `Assets/Scripts/Player/PlayerHotkeys.cs` for authoritative mappings.
- Systems-by-folder: Gameplay logic is organized by system (Player, Quest, NPC, Spells, Inventory) under `Assets/Scripts/`. Look for `Assets/Scripts/Quest/`, `Assets/Scripts/Player/`, `Assets/Scripts/NetworkManager.cs` to understand cross-cutting behavior.
- Data IDs & DB: some classes include explicit integer ids that map to iBoxDB records (for example `Task.taskId` in `Assets/Scripts/Quest/Task.cs`). Preserve those ids when adding persistent entities.
- Plugins & third-party libs: `Assets/Plugins/` and `Assets/Photon Unity Networking/` contain vendor code; avoid editing library code unless fixing a compatibility bug—prefer wrapping/adapter code in `Assets/Scripts/`.
- Meta-driven assets: the repo includes Unity .meta files and serialized assets. When creating or renaming assets prefer doing it from within Unity (to keep .meta GUIDs stable) instead of only editing the repo filesystem.

4) Integration points to pay attention to
- Photon (multiplayer): PUN callbacks (PunBehaviour/PUN interfaces) and PhotonViews are used across networking code. Use the PUN Editor tools to validate settings before runtime.
- iBoxDB (local DB): `Assets/Scripts/Util/Service.cs` and `NetworkManager.cs` import iBoxDB. Local DB initialization may run on startup—check for Reset/Init calls when modifying persistence.
- Input system: `OpenHogwarts.inputactions` and `PlayerHotkeys.cs` together control input. If adding actions, update both the InputActions asset and the hotkey switch/handler in code.

5) Safe change guidance and examples
- Adding a new player hotkey (concrete): Add an action to `Assets/OpenHogwarts.inputactions`, then update `Assets/Scripts/Player/PlayerHotkeys.cs` to read and handle the new action. The README explicitly points to this file as the single source of truth for hotkeys.
- Modifying networking behavior (concrete): Do NOT change PUN internals. Add or extend `Assets/Scripts/NetworkManager.cs` or create wrapper components that call Photon APIs. Validate by running two Editor instances or using PUN demo scenes.
- Adding persistent objects (concrete): create POCO model in `Assets/Scripts/` and ensure id fields are compatible with iBoxDB expectations (see `Assets/Scripts/Quest/Task.cs`).

6) Debugging & testing tips
- Editor logs & Console: primary debugging happens in Unity Editor Console. Use Debug.Log, and inspect serialized GameObjects in the Inspector during Play mode.
- IDE debugging: attach Visual Studio / Rider to the Unity Editor process for breakpoints in C# code.
- Multiplayer testing: run multiple Editor instances or use PUN cloud/local server. Open the PUN Wizard to verify server/AppId settings.

7) Where to look first (quick pointers)
- Readme & entry: `README.md` (root) — high-level project notes and hotkey references.
- Unity version: `ProjectSettings/ProjectVersion.txt` — pick the matching Unity editor.
- Input and hotkeys: `Assets/OpenHogwarts.inputactions`, `Assets/Scripts/Player/PlayerHotkeys.cs`.
- Networking: `Assets/Photon Unity Networking/` and `Assets/Scripts/NetworkManager.cs`.
- Persistence: `Assets/Scripts/Util/Service.cs`, `Assets/Scripts/Quest/Task.cs`.

Note on SRP choice: this project can be migrated to URP (recommended for broad platform support and easier conversion) or HDRP (High Definition Render Pipeline) if you want advanced lighting and higher-fidelity visuals. HDRP requires a higher Unity version and stricter hardware/lighting workflows — see `UPGRADE_TO_HDRP.md` for HDRP-specific guidance.

8) Editing rules for AI agents
- Preserve Unity serialized data: avoid changing GUIDs or deleting .meta files. If you add assets, prefer creating them via the Unity Editor to keep metadata consistent.
- Do not modify third-party plugin internals unless necessary; add adapter code under `Assets/Scripts/`.
- When updating input or serialized scene/prefab data, include a short human-friendly git commit message describing the Unity Editor step (e.g., "Added InputAction 'CastSpell' — updated InputActions asset in Unity Editor").

If anything here is unclear or you'd like more examples (for e.g. how spells or quests are implemented), tell me which subsystem to expand and I will iterate.
