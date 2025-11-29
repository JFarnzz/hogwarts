# Porting `ToonBasic.shader` -> HDRP Shader Graph (conversion spec)

This document describes how to recreate the `Assets/Standard Assets/Effects/ToonShading/Shaders/ToonBasic.shader` look in an HDRP-compatible Shader Graph. The original shader is an unlit-style toon that multiplies a sampled cubemap ramp by the textured color.

Summary of original logic

- Properties: `_Color` (Color), `_MainTex` (Texture2D), `_ToonShade` (Cubemap)
- For each fragment: col = _Color * Sample(_MainTex, uv)
- cube = Sample(_ToonShade, transformedNormal)
- output = 2.0 *cube.rgb* col.rgb (alpha = col.a)

Recommended approach

- Create an Unlit Shader Graph in HDRP (Unlit is appropriate because original shader doesn't use Unity lighting). If you need interaction with HDRP lighting, port to HDRP Lit and use the graph for BaseColor / Emission instead.

Step-by-step (Editor)

1. In Unity (with HDRP + Shader Graph installed), create a new Unlit Shader Graph:
   - Assets -> Create -> Shader -> HDRP -> Unlit Shader Graph
   - Name it `ToonBasic_HDRP.shadergraph`.
2. Open the Shader Graph (double-click).
3. Add the following Properties (blackboard, top-left):
   - Color (Name: _Color) — Color, default (0.5,0.5,0.5,1)
   - MainTex (Name: _MainTex) — Texture2D (Default white)
   - ToonShade (Name: _ToonShade) — Texture Cube

4. Create nodes and wire them:
   - Sample Texture2D node: set Texture input to property `MainTex`. Feed UV from the `UV` node (UV0) or `Tiling and Offset` if you want to respect `_MainTex_ST`.
   - Multiply Node A: multiply `Sample Texture2D RGBA` by `_Color` property. This produces `col` (vec4).
   - Normal Vector node: use the `Normal Vector` node (Space: Object) or use `Transform` node to convert Normal from Object to World. The original shader used MV * normal; for cubemap lookup, the important part is a direction in world or view space. Using Object->View or Object->World is acceptable—choose `Transform` (from Space: Object to Space: World) applied to `Normal`.
   - Sample Texture Cube node: sample the `ToonShade` cubemap using the transformed normal (as direction). This gives `cube` (RGB).
   - Multiply Node B: multiply `cube.rgb` by `col.rgb`.
   - Multiply Node C: multiply result by `2.0` (use a `Vector1` property or a `Multiply` with a `1` constant set to 2).
   - Construct the final RGBA output: use `Vector4` or output as `Color` where alpha = `col.a`.
   - Connect final color to the `Color` input of the Unlit Master (or to `BaseColor`/`Emission` for Lit Master).

Notes and mapping details

- Normal space: the original shader multiplies by UNITY_MATRIX_MV (model-view) and samples cube. In Shader Graph use `Normal Vector` -> `Transform` from Object to World (or View) and then feed to Sample Texture Cube. If the cubemap looks inverted, try flipping the normal or using View space. Test both Object->World and Object->View to find the closer visual match.
- UV tiling/offset: if the project uses `_MainTex_ST`, add a `Tiling/Offset` (Vector4) property and pass UV through `Tiling And Offset` node to the Sample Texture2D node.
- Alpha: pass `col.a` to output alpha. Unlit master has Alpha input—set Surface Type to Transparent if you need alpha blending.
- HDRP specifics: when using Unlit Graph in HDRP, ensure the Graph's Graph Settings Target is HDRP and that the material using the graph uses appropriate material settings (surface type, queue, etc.).

Testing and iteration

- Create a new Material using this Shader Graph and assign textures and a cubemap similar to the original `_ToonShade` asset.
- Assign the material to the mesh previously using `ToonBasic.mat` and compare visuals.
- Tweak normal transform (Object->World vs Object->View) and cubemap orientation to match the original shading.

If you'd like, I can:

- Produce a step-by-step automated .shadergraph asset text (JSON/YAML) for this simple graph that you can add to the repo. Warning: shadergraph format may differ between Unity/ShaderGraph versions and must be validated in Unity.
- Or, I can port the shader by creating a Shader Graph in a Unity Editor instance and committing the resulting `.shadergraph` asset (recommended for correctness).
