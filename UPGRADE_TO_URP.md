# Upgrade guide: Migrate project to Universal Render Pipeline (URP) & Shader Graph

This repository currently targets Unity 2022.3.x (see `ProjectSettings/ProjectVersion.txt`). To modernize rendering and enable Shader Graph workflows, migrating to the Universal Render Pipeline (URP) is the recommended path. URP supports Shader Graph and is broadly compatible with a range of platforms.

Important: BACK UP the repo before starting (create a git branch). Upgrading render pipelines can change serialized scene/prefab data and material references. Preserve .meta files.

Recommended high-level steps
1. Create a new branch for the migration: `git checkout -b upgrade/urp`.
2. Open the project in Unity 2022.3 (the project's declared version).
3. Use the Package Manager to install the Universal RP package (version compatible with 2022.3 LTS). Also install "Shader Graph".
4. Create URP assets: `Assets -> Create -> Rendering -> Universal Render Pipeline -> Pipeline Asset (Forward)` and a Renderer if needed.
5. In `Project Settings -> Graphics` assign the newly created URP Pipeline Asset.
6. Use the built-in render pipeline upgrader: `Edit -> Render Pipeline -> Universal Render Pipeline -> Upgrade Project Materials to URP Materials`. This converts many `Standard` materials to URP equivalents.
7. Replace/port image effects and custom shaders:
   - Standard Assets image effects (found in `Assets/Standard Assets/Effects/ImageEffects/`) are shaderlab-based and will not work in URP. Replace them with URP-compatible post processing (URP has a Post-processing stack or custom Render Features) or port their logic into custom Render Features/HDRP equivalents.
   - Project contains many legacy `.shader` files (Toon, Tessellation, Projector, MotionBlur, etc.) — list below. Decide to either rewrite them in Shader Graph (for surface-based materials) or implement custom renderer passes for hidden/post effects.
8. Convert particle shaders & materials: particles often use different shader properties; test particle systems and reassign URP particle shaders if needed.
9. Update any scripts that refer to shader names (e.g., checks for `material.shader.name == "Standard"` or `Find("Hidden/...")`). See `Assets/Standard Assets/Utility/AutoMobileShaderSwitch.cs` for an example replacement pattern.
10. Test scenes: open `Assets/Scenes/MainMenu.unity` and gameplay-critical scenes and run Play mode. Run multiplayer tests if applicable.
11. Iterate: fix visual regressions, reassess lighting and reflection probes, and update HDR/skybox settings to URP-compatible settings.

Concrete pointers from this codebase (files to inspect)
- `Assets/Standard Assets/Effects/ImageEffects/` — many Hidden/ image effect shaders that must be replaced or ported.
- `Assets/Standard Assets/Effects/ToonShading/` — custom toon shaders (Toon/Lit) that should be ported to Shader Graph or replaced.
- `Assets/Standard Assets/Effects/TessellationShaders/` — uses surface/tessellation shaders; URP does not support the built-in tessellation pipeline in the same way—these will need rewriting (or removal) for cross-platform compatibility.
- `Assets/Standard Assets/Effects/Projectors/` — projector shaders may require rework or replacement with URP projector alternatives.
- `Assets/Standard Assets/Utility/AutoMobileShaderSwitch.cs` — this script automates swapping mobile shaders; update its replacement table to map old shaders to URP shaders.

Testing checklist (before merge)
- Scene(s) open and tested in Play mode: `Assets/Scenes/MainMenu.unity` and key gameplay scenes.
- UI elements rendered and interactable (check Canvas render modes & overlay compatibility).
- Particle systems visually acceptable.
- Multiplayer connect/test (if uses Photon) — ensure Photon still runs; PUN doesn't require SRP changes but custom shaders used by networked players should be updated.
- Build to target platform(s) (Windows, WebGL, Android) and run smoke tests.

Rollback & safety
- If the upgrade causes problems, revert to the pre-upgrade branch. Keep the backup branch until features are validated.

Further improvements to follow migration
- Replace custom surface shaders with Shader Graph versions for maintainability.
- Create a small library of Shader Graph nodes/profiles for this project's stylistic needs (toon ramp, stylized outlines, etc.).
- Consider migrating to URP's Renderer Features for modular post-processing and custom passes.

If you'd like, I can:
- Create a detailed PR checklist and `UPGRADE_TO_URP.md` (this file) is added — already done.
- Create a script that scans project `.mat` and `.shader` usage and produces a mapping CSV for manual conversion.
- Start porting a small, test shader (e.g., `ToonBasic.shader`) to a Shader Graph asset and include instructions.

Which of the above would you like me to do next? (I can create the scanner script or begin porting one shader to Shader Graph as a concrete example.)
