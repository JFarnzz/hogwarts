# ToonBasic Shader Conversion to HDRP - Automated Generation Guide

This document explains how to convert the legacy `Toon/Basic` shader to HDRP using the **automated shader graph generator**. The original shader (`Assets/Standard Assets/Effects/ToonShading/Shaders/ToonBasic.shader`) is an unlit-style toon shader that multiplies a sampled cubemap ramp by the textured color.

## Original Shader Logic

- **Properties**: `_Color` (Color), `_MainTex` (Texture2D), `_ToonShade` (Cubemap)
- **Fragment Logic**: 
  - `col = _Color × Sample(_MainTex, uv)`
  - `cube = Sample(_ToonShade, viewSpaceNormal)`
  - `output = 2.0 × cube.rgb × col.rgb` (alpha = col.a)

## Automated Generation Approach

We've created an **Editor script** that programmatically generates the HDRP Shader Graph, ensuring consistency and reducing manual errors. The generator creates an HDRP Lit shader graph (for better integration with HDRP lighting) that replicates the toon shading effect.

## Prerequisites

Before starting, ensure:
1. ✅ Unity 2022.3+ or Unity 6 (6000.0.4+) is installed
2. ✅ HDRP package is installed via Package Manager
3. ✅ Shader Graph package is installed via Package Manager
4. ✅ All console errors have been resolved (78 errors were recently fixed)

## Step-by-Step Conversion

### 1. Run the Automated Generator

1. Open Unity Editor
2. From the menu bar, select: **Tools → Shader Conversion → Generate ToonBasic HDRP Shader Graph**
3. Wait for the console message: `✓ ToonBasic HDRP Shader Graph generated successfully`
4. The shader graph will be created at: `Assets/Shaders/HDRP/ToonBasic_HDRP.shadergraph`
5. The Project window will automatically select and highlight the new asset

### 2. Verify the Generated Shader Graph

1. Double-click `ToonBasic_HDRP.shadergraph` to open it in the Shader Graph editor
2. Verify the node structure:
   - **Properties** (Blackboard): Main Color, Base (RGB) texture, ToonShader Cubemap
   - **Sample Texture 2D**: Samples the base texture
   - **Normal Vector** (View Space): Provides normals for cubemap lookup
   - **Sample Cubemap**: Samples toon shade using view-space normals
   - **Multiply nodes**: Implements `2.0 × cubemap × (color × texture)` logic
   - **Master node**: Connected to Base Color output (HDRP Lit)

### 3. Update Materials

1. Navigate to: `Assets/Standard Assets/Effects/ToonShading/Materials/`
2. Select `ToonBasic.mat`
3. In the Inspector, click the **Shader** dropdown
4. Select: **Shader Graphs → ToonBasic_HDRP**
5. Verify properties are preserved:
   - Main Color tint
   - Base (RGB) texture assignment
   - ToonShader Cubemap assignment

### 4. Test in MainMenu Scene

1. Open `Assets/Scenes/MainMenu.unity`
2. Locate objects using ToonBasic materials (check Hierarchy for character models)
3. Enter Play mode and verify:
   - Characters/objects render correctly
   - Toon shading effect is visible
   - Colors match original appearance
   - No pink/missing shader materials

## Technical Details

### Generated Node Graph Structure

The automated generator creates this node flow:
```
ViewSpaceNormal → SampleCubemap(ToonShadeCubemap) → RGB
MainColor × SampleTexture2D(BaseTexture) → RGBA
2.0 × CubemapRGB × (MainColor × BaseTexture) → BaseColor output
```

### Key Implementation Notes

- **Normal Space**: Uses View Space normals (matching original shader's `UNITY_MATRIX_MV` transform)
- **Target**: HDRP Lit shader (for better HDRP integration vs Unlit)
- **Surface Type**: Opaque (matching original shader)
- **Culling**: Default back-face culling (original used `Cull Off` - modify if needed)
- **Alpha**: Preserves texture alpha in output

### Differences from Original

- **Rendering Path**: HDRP Lit vs Built-in RP unlit
- **Fog Handling**: HDRP Volume-based fog vs built-in fog
- **Culling**: Back-face culling enabled by default (original rendered both sides)

To enable two-sided rendering:
1. Open the shader graph
2. Graph Inspector → Surface Options → Render Face → Both

## Troubleshooting

### Shader Graph appears empty/broken
- Ensure HDRP and Shader Graph packages are installed
- Check Console for generation errors
- Re-run: Tools → Shader Conversion → Generate ToonBasic HDRP Shader Graph

### Material turns pink (missing shader)
- Verify shader compiled successfully (check Console)
- Right-click `.shadergraph` file → Reimport
- Confirm HDRP is active in Project Settings → Graphics

### Lighting looks different
- HDRP uses different lighting model
- Adjust Main Color tint to compensate
- Try different ToonShader Cubemap textures
- Adjust HDRP Volume settings (Exposure, Color Adjustments)

### Cubemap not working
- Ensure cubemap texture is imported as "Cubemap" type
- Texture Import Settings → Texture Shape → Cube
- Verify proper mip maps

## Next Steps

After ToonBasic works, convert remaining toon shaders in priority order:
1. **ToonLit.shader** - Adds lighting calculations
2. **ToonBasicOutline.shader** - Adds outline/rim effect
3. **ToonLitOutline.shader** - Combines lighting and outlines

## Validation Checklist

Before marking conversion complete:
- [ ] Shader graph generates without errors
- [ ] Material accepts new shader without errors
- [ ] Visual appearance matches original (or acceptable)
- [ ] Characters render correctly in MainMenu scene
- [ ] No performance regressions
- [ ] Multiplayer test: networked characters render correctly
- [ ] Build test: shader works in development build

## References

- **Generator Script**: `Assets/Editor/ShaderGraphGenerators/ToonBasicHDRPGenerator.cs`
- **Original Shader**: `Assets/Standard Assets/Effects/ToonShading/Shaders/ToonBasic.shader`
- **Material Usage**: `material_usage_by_guid.csv`
- **HDRP Guide**: `UPGRADE_TO_HDRP.md`
- **Conversion Plan**: `shader_conversion_plan.md`
