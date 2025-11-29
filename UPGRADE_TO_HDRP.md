# Upgrade guide: Migrate project to High Definition Render Pipeline (HDRP) & Shader Graph

This repository currently targets Unity 2022.3.x (see `ProjectSettings/ProjectVersion.txt`). HDRP offers advanced lighting, volumetrics, and higher-fidelity visuals compared to URP, but it has stricter requirements and is intended for high-end platforms. Only migrate to HDRP if you need high-end PC/console visuals; otherwise URP is safer for broad platform compatibility.

IMPORTANT PREPARATION
- HDRP requires Unity versions that support the HDRP package reliably. For Unity 2022.3 LTS, HDRP 12.x is the matching major release. Verify package compatibility in Package Manager.
- BACK UP the repo and create a migration branch: `git checkout -b upgrade/hdrp`.
- HDRP will change lighting, materials, and many serializations. Keep the backup branch until validation is complete.

High-level migration steps
1. Confirm target platforms are appropriate for HDRP (PC/console). HDRP is not supported on many mobile or lightweight platforms.
2. In Unity (2022.3 recommended), open Package Manager and install:
   - High Definition RP (HDRP) package matching the Unity major version (e.g., 12.x for 2022.3)
   - Shader Graph (matching version)
3. Create HDRP assets: `Assets -> Create -> Rendering -> High Definition Render Pipeline -> HDRenderPipelineAsset` and create an HD Render Pipeline Default Settings if prompted.
4. In `Project Settings -> Graphics` assign the HDRP Render Pipeline Asset. Also set the `Scriptable Render Pipeline Settings` to the HDRP asset if applicable.
5. Use the HDRP upgrade utilities: `Edit -> Render Pipeline -> HD Render Pipeline -> Upgrade Project Materials to HDRP Materials` (this will attempt to convert Standard materials to HDRP equivalents).
6. Manually port complex/custom shaders and image effects:
   - Many image effects in `Assets/Standard Assets/Effects/ImageEffects/` are not directly compatible. Replace them with HDRP Volume-based post-processing or write Custom Passes / Custom Post Processors in HDRP.
   - Surface and tessellation shaders (`Assets/Standard Assets/Effects/TessellationShaders/`) may require rewrites or replacement; HDRP supports tessellation via HDRP Lit shader features but custom CGPROGRAM tessellation may not port cleanly.
   - Toon and specialized shaders (`Assets/Standard Assets/Effects/ToonShading/`) should be reimplemented in Shader Graph with HDRP Master Node (Lit) or via HDRP Custom ShaderGraph subgraphs.
7. Update project lighting:
   - HDRP uses a physically-based lighting model. Re-bake reflection probes, update Lighting settings (Environment Lighting, Sky & Fog Volume), and use HDRP-specific settings for Shadow Quality, Contact Shadows, and Volumetrics.
8. Update assets and prefabs referencing legacy shaders — replace shader references to HDRP equivalents. Use automated scripts to list `.mat` files referencing old shader names.
9. Test scenes and gameplay: run `Assets/Scenes/MainMenu.unity` and play through major flows. Inspect UI, particle systems, and networked objects.

Key considerations for this repository
- Project contains many legacy `.shader` files, image effects and Standard Assets that will require porting. See `Assets/Standard Assets/Effects/` for the most affected folders.
- `AutoMobileShaderSwitch.cs` assumes alternate shader replacements — update or disable this for HDRP as it targets mobile replacements.
- PUN (Photon) networking is orthogonal to SRP changes — networking code should continue to work, but verify that networked objects' materials/shaders are converted.

Testing checklist (before merge)
- Run Play mode against `Assets/Scenes/MainMenu.unity` and other gameplay scenes.
- Verify all major characters render correctly (players, NPCs) and that shadows/lighting are acceptable.
- Re-bake lightmaps and reflection probes where applicable.
- Verify post-processing volumes give expected visual results.
- Build to target high-end desktop platform(s) and run smoke tests.

Rollback & safety
- Keep a pre-upgrade branch. If issues are severe, revert the main branch or cherry-pick only safe changes.

Follow-up tasks I can do for you
- Generate a mapping scan script that enumerates all `.mat` files and outputs their current shader names to a CSV, helping plan conversions.
- Start porting a single shader (e.g., `ToonBasic.shader`) into an HDRP Shader Graph with a matching look, then document the steps.
- Create small helper scripts to replace shader references in `.mat` YAML files (use with caution; prefer doing replacements in Unity when possible).

Which follow-up would you like me to start: create the scanner script, port a shader to Shader Graph/HDRP, or prepare a PR checklist with exact steps to run in the Editor?
