#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ShaderConversion
{
    /// <summary>
    /// Utility to convert materials using legacy Built-in RP shaders to HDRP Lit shader
    /// This is a simplified material converter that updates shader references and attempts
    /// to preserve material properties where possible.
    /// 
    /// Usage: 
    /// - Tools -> Shader Conversion -> Convert Selected Materials to HDRP Lit
    /// - Or call ConvertMaterialToHDRP() directly with material references
    /// </summary>
    public static class MaterialToHDRPConverter
    {
        private const string HDRP_LIT_SHADER = "HDRP/Lit";
        
        [MenuItem("Tools/Shader Conversion/Convert Selected Materials to HDRP Lit")]
        public static void ConvertSelectedMaterialsToHDRP()
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
            {
                Debug.LogWarning("No materials selected. Please select one or more material assets.");
                return;
            }

            int convertedCount = 0;
            int failedCount = 0;

            foreach (var obj in Selection.objects)
            {
                Material material = obj as Material;
                if (material != null)
                {
                    if (ConvertMaterialToHDRP(material))
                    {
                        convertedCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Material conversion complete: {convertedCount} successful, {failedCount} failed");
        }

        /// <summary>
        /// Convert a single material to HDRP Lit shader, attempting to preserve properties
        /// </summary>
        public static bool ConvertMaterialToHDRP(Material material)
        {
            if (material == null)
            {
                Debug.LogError("Cannot convert null material");
                return false;
            }

            string originalShaderName = material.shader.name;
            Debug.Log($"Converting material '{material.name}' from shader '{originalShaderName}' to HDRP Lit");

            // Store original property values before shader change
            var savedProperties = new Dictionary<string, object>();
            
            // Save common texture properties
            TrySaveTexture(material, "_MainTex", savedProperties);
            TrySaveTexture(material, "_BaseColorMap", savedProperties);
            TrySaveTexture(material, "_BumpMap", savedProperties);
            TrySaveTexture(material, "_NormalMap", savedProperties);
            TrySaveTexture(material, "_MetallicGlossMap", savedProperties);
            TrySaveTexture(material, "_MaskMap", savedProperties);
            TrySaveTexture(material, "_EmissionMap", savedProperties);
            TrySaveTexture(material, "_EmissiveColorMap", savedProperties);

            // Save common color properties
            TrySaveColor(material, "_Color", savedProperties);
            TrySaveColor(material, "_BaseColor", savedProperties);
            TrySaveColor(material, "_EmissionColor", savedProperties);
            TrySaveColor(material, "_EmissiveColor", savedProperties);

            // Save common float properties
            TrySaveFloat(material, "_Metallic", savedProperties);
            TrySaveFloat(material, "_Smoothness", savedProperties);
            TrySaveFloat(material, "_Glossiness", savedProperties);
            TrySaveFloat(material, "_BumpScale", savedProperties);
            TrySaveFloat(material, "_NormalScale", savedProperties);
            TrySaveFloat(material, "_AlphaCutoff", savedProperties);

            // Find HDRP Lit shader
            Shader hdrpLitShader = Shader.Find(HDRP_LIT_SHADER);
            if (hdrpLitShader == null)
            {
                Debug.LogError($"HDRP Lit shader not found. Ensure HDRP package is installed and project is configured for HDRP.");
                return false;
            }

            // Assign new shader
            material.shader = hdrpLitShader;

            // Restore properties to new shader (if property names match)
            RestoreTexture(material, "_MainTex", "_BaseColorMap", savedProperties);
            RestoreTexture(material, "_BaseColorMap", "_BaseColorMap", savedProperties);
            RestoreTexture(material, "_BumpMap", "_NormalMap", savedProperties);
            RestoreTexture(material, "_NormalMap", "_NormalMap", savedProperties);
            RestoreTexture(material, "_MetallicGlossMap", "_MaskMap", savedProperties);
            RestoreTexture(material, "_MaskMap", "_MaskMap", savedProperties);
            RestoreTexture(material, "_EmissionMap", "_EmissiveColorMap", savedProperties);
            RestoreTexture(material, "_EmissiveColorMap", "_EmissiveColorMap", savedProperties);

            RestoreColor(material, "_Color", "_BaseColor", savedProperties);
            RestoreColor(material, "_BaseColor", "_BaseColor", savedProperties);
            RestoreColor(material, "_EmissionColor", "_EmissiveColor", savedProperties);
            RestoreColor(material, "_EmissiveColor", "_EmissiveColor", savedProperties);

            RestoreFloat(material, "_Metallic", "_Metallic", savedProperties);
            RestoreFloat(material, "_Smoothness", "_Smoothness", savedProperties);
            RestoreFloat(material, "_Glossiness", "_Smoothness", savedProperties); // Map glossiness to smoothness
            RestoreFloat(material, "_BumpScale", "_NormalScale", savedProperties);
            RestoreFloat(material, "_NormalScale", "_NormalScale", savedProperties);
            RestoreFloat(material, "_AlphaCutoff", "_AlphaCutoff", savedProperties);

            EditorUtility.SetDirty(material);
            Debug.Log($"✓ Successfully converted material '{material.name}' to HDRP Lit");
            return true;
        }

        #region Property Save/Restore Helpers

        private static void TrySaveTexture(Material material, string propertyName, Dictionary<string, object> storage)
        {
            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);
                if (texture != null)
                {
                    storage[propertyName] = texture;
                }
            }
        }

        private static void TrySaveColor(Material material, string propertyName, Dictionary<string, object> storage)
        {
            if (material.HasProperty(propertyName))
            {
                storage[propertyName] = material.GetColor(propertyName);
            }
        }

        private static void TrySaveFloat(Material material, string propertyName, Dictionary<string, object> storage)
        {
            if (material.HasProperty(propertyName))
            {
                storage[propertyName] = material.GetFloat(propertyName);
            }
        }

        private static void RestoreTexture(Material material, string oldPropertyName, string newPropertyName, Dictionary<string, object> storage)
        {
            if (storage.ContainsKey(oldPropertyName) && material.HasProperty(newPropertyName))
            {
                material.SetTexture(newPropertyName, storage[oldPropertyName] as Texture);
            }
        }

        private static void RestoreColor(Material material, string oldPropertyName, string newPropertyName, Dictionary<string, object> storage)
        {
            if (storage.ContainsKey(oldPropertyName) && material.HasProperty(newPropertyName))
            {
                material.SetColor(newPropertyName, (Color)storage[oldPropertyName]);
            }
        }

        private static void RestoreFloat(Material material, string oldPropertyName, string newPropertyName, Dictionary<string, object> storage)
        {
            if (storage.ContainsKey(oldPropertyName) && material.HasProperty(newPropertyName))
            {
                material.SetFloat(newPropertyName, (float)storage[oldPropertyName]);
            }
        }

        #endregion

        /// <summary>
        /// Batch convert all materials in a folder using legacy shaders to HDRP Lit
        /// </summary>
        [MenuItem("Tools/Shader Conversion/Convert All Materials in Selected Folder")]
        public static void ConvertAllMaterialsInFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder Containing Materials", "Assets", "");
            
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.Log("Folder selection cancelled");
                return;
            }

            // Convert absolute path to relative Unity path
            if (!folderPath.StartsWith(Application.dataPath))
            {
                Debug.LogError("Selected folder must be inside the Assets folder");
                return;
            }

            string relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
            
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { relativePath });
            
            int convertedCount = 0;
            int skippedCount = 0;

            foreach (string guid in materialGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                
                if (material != null && !material.shader.name.StartsWith("HDRP"))
                {
                    if (ConvertMaterialToHDRP(material))
                    {
                        convertedCount++;
                    }
                }
                else
                {
                    skippedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Batch conversion complete: {convertedCount} converted, {skippedCount} skipped (already HDRP or null)");
        }
    }
}
#endif