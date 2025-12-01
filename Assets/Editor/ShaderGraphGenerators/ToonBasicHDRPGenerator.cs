#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ShaderGraphGenerators
{
    /// <summary>
    /// Helper utility to guide ToonBasic HDRP Shader Graph creation.
    /// 
    /// NOTE: Unity's Shader Graph API is internal and cannot be accessed programmatically.
    /// This utility provides guidance and material conversion helpers instead.
    /// 
    /// For manual shader graph creation, see: TOONBASIC_CONVERSION.md
    /// </summary>
    public static class ToonBasicHDRPGenerator
    {
        private const string SHADER_GRAPH_PATH = "Assets/Shaders/HDRP/ToonBasic_HDRP.shadergraph";
        private const string HDRP_LIT_SHADER = "HDRP/Lit";
        
        [MenuItem("Tools/Shader Conversion/ToonBasic to HDRP/Show Conversion Guide")]
        public static void ShowConversionGuide()
        {
            string guide = @"
═══════════════════════════════════════════════════════════════
           ToonBasic → HDRP Shader Graph Conversion Guide
═══════════════════════════════════════════════════════════════

Unity's Shader Graph API is internal and cannot be scripted.
You must create the shader graph MANUALLY in Unity Editor.

STEP 1: Create New Shader Graph
───────────────────────────────
  • Right-click in Project: Assets/Shaders/HDRP/
  • Create → Shader Graph → HDRP → Lit Shader Graph
  • Name it: ToonBasic_HDRP

STEP 2: Add Properties (Blackboard)
───────────────────────────────────
  • _Color (Color, default gray)     → Reference: _Color
  • _MainTex (Texture2D)             → Reference: _MainTex  
  • _ToonShade (Cubemap)             → Reference: _ToonShade

STEP 3: Build Node Graph
────────────────────────
  • Add 'Sample Texture 2D' node → connect _MainTex
  • Add 'Sample Cubemap' node → connect _ToonShade
  • Add 'Normal Vector' node (View Space) → connect to Cubemap Dir
  • Add 'Multiply' node: _Color × Texture sample
  • Add 'Multiply' node: Result × Cubemap sample  
  • Add 'Multiply' node: Result × 2.0 (constant)
  • Connect final output → Base Color (Fragment)

STEP 4: Save and Apply
──────────────────────
  • Save shader graph (Ctrl+S)
  • Use 'Convert Legacy Materials' menu to update materials

Original Toon/Basic formula: 2.0 × Cubemap × (Color × Texture)

═══════════════════════════════════════════════════════════════
";
            Debug.Log(guide);
            EditorUtility.DisplayDialog("ToonBasic HDRP Conversion Guide", 
                "Conversion instructions have been printed to the Console.\n\n" +
                "Press Ctrl+Shift+C to open Console window.", "OK");
        }

        [MenuItem("Tools/Shader Conversion/ToonBasic to HDRP/Convert Legacy Materials to HDRP Lit")]
        public static void ConvertLegacyMaterialsToHDRP()
        {
            // Find all materials using legacy Toon shaders
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int converted = 0;
            int skipped = 0;
            
            Shader hdrpLit = Shader.Find(HDRP_LIT_SHADER);
            if (hdrpLit == null)
            {
                Debug.LogError("HDRP/Lit shader not found. Is HDRP installed and configured?");
                return;
            }
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat == null || mat.shader == null) continue;
                
                string shaderName = mat.shader.name;
                if (shaderName.Contains("Toon/Basic") || 
                    shaderName.Contains("Toon/Lit") ||
                    shaderName == "Hidden/InternalErrorShader")
                {
                    // Preserve properties before conversion
                    Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    
                    // Convert to HDRP/Lit
                    mat.shader = hdrpLit;
                    
                    // Remap properties
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", mainColor);
                    if (mat.HasProperty("_BaseColorMap") && mainTex != null)
                        mat.SetTexture("_BaseColorMap", mainTex);
                    
                    EditorUtility.SetDirty(mat);
                    Debug.Log($"Converted: {path}");
                    converted++;
                }
                else
                {
                    skipped++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"Material conversion complete: {converted} converted, {skipped} skipped");
        }

        [MenuItem("Tools/Shader Conversion/ToonBasic to HDRP/Find Broken Materials")]
        public static void FindBrokenMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int broken = 0;
            
            Debug.Log("Scanning for broken/legacy materials...\n");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat == null) continue;
                
                bool isBroken = false;
                string reason = "";
                
                if (mat.shader == null)
                {
                    isBroken = true;
                    reason = "Shader is null";
                }
                else if (mat.shader.name == "Hidden/InternalErrorShader")
                {
                    isBroken = true;
                    reason = "Missing shader (pink material)";
                }
                else if (mat.shader.name.Contains("Toon/"))
                {
                    isBroken = true;
                    reason = $"Legacy shader: {mat.shader.name}";
                }
                
                if (isBroken)
                {
                    Debug.LogWarning($"[BROKEN] {path}\n  Reason: {reason}");
                    broken++;
                }
            }
            
            if (broken == 0)
                Debug.Log("✓ No broken materials found!");
            else
                Debug.LogWarning($"\n══ Found {broken} broken/legacy materials ══\n" +
                    "Use 'Convert Legacy Materials to HDRP Lit' to fix them.");
        }

        [MenuItem("Tools/Shader Conversion/ToonBasic to HDRP/Create Shader Graph Folder")]
        public static void CreateShaderGraphFolder()
        {
            string folder = Path.GetDirectoryName(SHADER_GRAPH_PATH);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                Debug.Log($"Created folder: {folder}");
                
                // Select the folder
                var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folder);
                if (folderAsset != null)
                {
                    Selection.activeObject = folderAsset;
                    EditorGUIUtility.PingObject(folderAsset);
                }
            }
            else
            {
                Debug.Log($"Folder already exists: {folder}");
            }
            
            Debug.Log("\nNext: Right-click in Project window → Create → Shader Graph → HDRP → Lit Shader Graph");
        }
    }
}
#endif
