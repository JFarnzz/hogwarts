#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using System.IO;
using System.Linq;

namespace ShaderGraphGenerators
{
    /// <summary>
    /// Editor utility to programmatically generate ToonBasic HDRP Shader Graph
    /// This recreates the functionality of the legacy Toon/Basic shader for HDRP
    /// 
    /// Usage: Tools -> Shader Conversion -> Generate ToonBasic HDRP Shader Graph
    /// </summary>
    public static class ToonBasicHDRPGenerator
    {
        private const string OUTPUT_PATH = "Assets/Shaders/HDRP/ToonBasic_HDRP.shadergraph";
        
        [MenuItem("Tools/Shader Conversion/Generate ToonBasic HDRP Shader Graph")]
        public static void GenerateToonBasicShaderGraph()
        {
            Debug.Log("Starting ToonBasic HDRP Shader Graph generation...");
            
            try
            {
                // Ensure output directory exists
                string directory = Path.GetDirectoryName(OUTPUT_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }
                
                // Create a new HDRP Lit Shader Graph
                var graphData = new GraphData();
                graphData.isSubGraph = false;
                
                // Set up HDRP Lit target
                var target = (HDTarget)graphData.activeTargets.FirstOrDefault(t => t is HDTarget);
                if (target == null)
                {
                    target = ScriptableObject.CreateInstance<HDTarget>();
                    graphData.activeTargets.Add(target);
                }
                
                // Configure HDRP target for opaque rendering (matching original shader)
                target.TrySetActiveSubTarget(typeof(HDLitSubTarget));
                
                // Create property nodes
                var colorProperty = new ColorShaderProperty
                {
                    displayName = "Main Color",
                    value = new Color(0.5f, 0.5f, 0.5f, 1.0f)
                };
                graphData.AddGraphInput(colorProperty);
                
                var mainTexProperty = new Texture2DShaderProperty
                {
                    displayName = "Base (RGB)"
                };
                graphData.AddGraphInput(mainTexProperty);
                
                var toonShadeProperty = new CubemapShaderProperty
                {
                    displayName = "ToonShader Cubemap (RGB)"
                };
                graphData.AddGraphInput(toonShadeProperty);
                
                // Create nodes for the shader logic
                // 1. Sample Base Texture
                var sampleTextureNode = new SampleTexture2DNode();
                graphData.AddNode(sampleTextureNode);
                sampleTextureNode.drawState.position = new Rect(new Vector2(-800, 0), sampleTextureNode.drawState.position.size);
                
                // 2. Sample Cubemap for toon shading (using View Direction as approximation)
                var sampleCubemapNode = new SampleCubemapNode();
                graphData.AddNode(sampleCubemapNode);
                sampleCubemapNode.drawState.position = new Rect(new Vector2(-800, 200), sampleCubemapNode.drawState.position.size);
                
                // 3. Get Normal in View Space (for cubemap lookup)
                var normalNode = new NormalVectorNode();
                normalNode.space = CoordinateSpace.View;
                graphData.AddNode(normalNode);
                normalNode.drawState.position = new Rect(new Vector2(-1000, 250), normalNode.drawState.position.size);
                
                // 4. Multiply Main Color with Texture
                var multiplyColorTexNode = new MultiplyNode();
                graphData.AddNode(multiplyColorTexNode);
                multiplyColorTexNode.drawState.position = new Rect(new Vector2(-500, 0), multiplyColorTexNode.drawState.position.size);
                
                // 5. Multiply by 2 (for 2.0f * cube.rgb * col.rgb)
                var multiplyBy2Node = new MultiplyNode();
                graphData.AddNode(multiplyBy2Node);
                multiplyBy2Node.drawState.position = new Rect(new Vector2(-300, 100), multiplyBy2Node.drawState.position.size);
                
                // 6. Create Vector1 node for the 2.0 multiplier
                var twoValueNode = new Vector1Node();
                twoValueNode.value = 2.0f;
                graphData.AddNode(twoValueNode);
                twoValueNode.drawState.position = new Rect(new Vector2(-500, 300), twoValueNode.drawState.position.size);
                
                // 7. Multiply cubemap with color*texture
                var finalMultiplyNode = new MultiplyNode();
                graphData.AddNode(finalMultiplyNode);
                finalMultiplyNode.drawState.position = new Rect(new Vector2(-200, 150), finalMultiplyNode.drawState.position.size);
                
                // Connect property nodes to sampler nodes
                var mainTexPropertyNode = new PropertyNode();
                mainTexPropertyNode.property = mainTexProperty;
                graphData.AddNode(mainTexPropertyNode);
                mainTexPropertyNode.drawState.position = new Rect(new Vector2(-1000, -50), mainTexPropertyNode.drawState.position.size);
                
                var colorPropertyNode = new PropertyNode();
                colorPropertyNode.property = colorProperty;
                graphData.AddNode(colorPropertyNode);
                colorPropertyNode.drawState.position = new Rect(new Vector2(-700, -100), colorPropertyNode.drawState.position.size);
                
                var cubemapPropertyNode = new PropertyNode();
                cubemapPropertyNode.property = toonShadeProperty;
                graphData.AddNode(cubemapPropertyNode);
                cubemapPropertyNode.drawState.position = new Rect(new Vector2(-1000, 200), cubemapPropertyNode.drawState.position.size);
                
                // Create edges (connections between nodes)
                // Connect properties to samplers
                var mainTexToSampler = new Edge(
                    mainTexPropertyNode.GetSlotReference(mainTexProperty.guid),
                    sampleTextureNode.GetSlotReference(SampleTexture2DNode.TextureInputId)
                );
                graphData.AddEdge(mainTexToSampler);
                
                var cubemapToCubemapSampler = new Edge(
                    cubemapPropertyNode.GetSlotReference(toonShadeProperty.guid),
                    sampleCubemapNode.GetSlotReference(SampleCubemapNode.CubemapInputId)
                );
                graphData.AddEdge(cubemapToCubemapSampler);
                
                // Connect normal to cubemap LOD
                var normalToCubemap = new Edge(
                    normalNode.GetSlotReference(NormalVectorNode.OutputSlotId),
                    sampleCubemapNode.GetSlotReference(SampleCubemapNode.DirInputId)
                );
                graphData.AddEdge(normalToCubemap);
                
                // Connect color * texture
                var colorToMultiply = new Edge(
                    colorPropertyNode.GetSlotReference(colorProperty.guid),
                    multiplyColorTexNode.GetSlotReference(0)
                );
                graphData.AddEdge(colorToMultiply);
                
                var textureToMultiply = new Edge(
                    sampleTextureNode.GetSlotReference(SampleTexture2DNode.OutputSlotRGBAId),
                    multiplyColorTexNode.GetSlotReference(1)
                );
                graphData.AddEdge(textureToMultiply);
                
                // Connect cubemap * (color * texture)
                var cubemapToFinalMultiply = new Edge(
                    sampleCubemapNode.GetSlotReference(SampleCubemapNode.OutputSlotRGBId),
                    finalMultiplyNode.GetSlotReference(0)
                );
                graphData.AddEdge(cubemapToFinalMultiply);
                
                var colorTexToFinalMultiply = new Edge(
                    multiplyColorTexNode.GetSlotReference(MultiplyNode.OutputSlotId),
                    finalMultiplyNode.GetSlotReference(1)
                );
                graphData.AddEdge(colorTexToFinalMultiply);
                
                // Connect 2.0 multiplier
                var twoValueToMultiply = new Edge(
                    twoValueNode.GetSlotReference(0),
                    multiplyBy2Node.GetSlotReference(0)
                );
                graphData.AddEdge(twoValueToMultiply);
                
                var finalToMultiplyBy2 = new Edge(
                    finalMultiplyNode.GetSlotReference(MultiplyNode.OutputSlotId),
                    multiplyBy2Node.GetSlotReference(1)
                );
                graphData.AddEdge(finalToMultiplyBy2);
                
                // Connect to master node (Base Color)
                var masterNode = graphData.outputNode;
                if (masterNode != null)
                {
                    var baseColorSlot = masterNode.FindSlot<Vector3MaterialSlot>(HDLitMasterNode.AlbedoSlotId);
                    if (baseColorSlot != null)
                    {
                        var toMaster = new Edge(
                            multiplyBy2Node.GetSlotReference(MultiplyNode.OutputSlotId),
                            masterNode.GetSlotReference(HDLitMasterNode.AlbedoSlotId)
                        );
                        graphData.AddEdge(toMaster);
                    }
                }
                
                // Save the shader graph
                File.WriteAllText(OUTPUT_PATH, EditorJsonUtility.ToJson(graphData, true));
                AssetDatabase.ImportAsset(OUTPUT_PATH);
                
                Debug.Log($"✓ ToonBasic HDRP Shader Graph generated successfully at: {OUTPUT_PATH}");
                Debug.Log("Next steps:");
                Debug.Log("1. Select ToonBasic.mat material in Assets/Standard Assets/Effects/ToonShading/Materials/");
                Debug.Log("2. Change shader to 'Shader Graphs/ToonBasic_HDRP'");
                Debug.Log("3. Test in MainMenu scene");
                
                // Ping the asset in the project window
                var asset = AssetDatabase.LoadAssetAtPath<Object>(OUTPUT_PATH);
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate ToonBasic HDRP Shader Graph: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [MenuItem("Tools/Shader Conversion/Generate ToonBasic HDRP Shader Graph", true)]
        public static bool ValidateGenerateToonBasicShaderGraph()
        {
            // Check if HDRP and Shader Graph packages are available
            return true; // TODO: Add package validation if needed
        }
    }
}
#endif
