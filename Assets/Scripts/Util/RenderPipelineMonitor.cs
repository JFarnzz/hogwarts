using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Utility component to observe and (optionally) switch the active render pipeline at runtime.
// Attach to a singleton GameObject in an always-loaded scene (e.g., MainMenu) to log pipeline changes.
// NOTE: Switching between Built-in and HDRP at runtime can incur hitches; prefer doing major pipeline changes on a loading screen.
// For HDRP migration tracking only; remove if not needed in production builds.
namespace OpenHogwarts.Util
{
    [DisallowMultipleComponent]
    public class RenderPipelineMonitor : MonoBehaviour
    {
        [Header("Assign pipeline assets for optional runtime switching")]
        [Tooltip("Default HDRP Render Pipeline Asset to apply (GraphicsSettings.defaultRenderPipeline)")] 
        public RenderPipelineAsset hdrpDefaultAsset;
        [Tooltip("Optional override asset for current QualitySettings.renderPipeline (can be same as HDRP asset)")] 
        public RenderPipelineAsset hdrpQualityOverrideAsset;
        [Tooltip("If true, will log pipeline info on start and when it changes")] 
        public bool logChanges = true;
        [Tooltip("If true, allows keyboard debug shortcuts: LeftShift toggles default, RightShift toggles override")] 
        public bool enableDebugHotkeys = false;
        [Tooltip("If true, will defer pipeline switch to end of frame to reduce mid-frame state changes")] 
        public bool deferApply = true;

        static RenderPipelineMonitor _instance;

        void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            RenderPipelineManager.activeRenderPipelineTypeChanged += OnActivePipelineTypeChanged;
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                RenderPipelineManager.activeRenderPipelineTypeChanged -= OnActivePipelineTypeChanged;
            }
        }

        void Start()
        {
            if (logChanges)
            {
                LogPipelineState("Initial render pipeline state");
            }
        }

        void Update()
        {
            if (!enableDebugHotkeys) return;

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                ToggleDefaultPipeline();
            }
            else if (Input.GetKeyDown(KeyCode.RightShift))
            {
                ToggleOverridePipeline();
            }
        }

        void OnActivePipelineTypeChanged()
        {
            if (logChanges)
            {
                LogPipelineState("Pipeline type changed callback");
            }
        }

        public void ToggleDefaultPipeline()
        {
            var current = GraphicsSettings.defaultRenderPipeline;
            if (current == hdrpDefaultAsset)
            {
                SetDefaultPipeline(null);
            }
            else
            {
                SetDefaultPipeline(hdrpDefaultAsset);
            }
        }

        public void ToggleOverridePipeline()
        {
            var current = QualitySettings.renderPipeline;
            if (current == hdrpQualityOverrideAsset)
            {
                SetQualityOverridePipeline(null);
            }
            else
            {
                SetQualityOverridePipeline(hdrpQualityOverrideAsset);
            }
        }

        public void SetDefaultPipeline(RenderPipelineAsset asset)
        {
            if (deferApply)
            {
                StartCoroutine(ApplyDefaultDeferred(asset));
            }
            else
            {
                GraphicsSettings.defaultRenderPipeline = asset;
                if (logChanges) LogPipelineState("Applied default pipeline immediately");
            }
        }

        public void SetQualityOverridePipeline(RenderPipelineAsset asset)
        {
            if (deferApply)
            {
                StartCoroutine(ApplyOverrideDeferred(asset));
            }
            else
            {
                QualitySettings.renderPipeline = asset;
                if (logChanges) LogPipelineState("Applied quality override pipeline immediately");
            }
        }

        System.Collections.IEnumerator ApplyDefaultDeferred(RenderPipelineAsset asset)
        {
            yield return new WaitForEndOfFrame();
            GraphicsSettings.defaultRenderPipeline = asset;
            if (logChanges) LogPipelineState("Applied default pipeline (deferred)");
        }

        System.Collections.IEnumerator ApplyOverrideDeferred(RenderPipelineAsset asset)
        {
            yield return new WaitForEndOfFrame();
            QualitySettings.renderPipeline = asset;
            if (logChanges) LogPipelineState("Applied quality override pipeline (deferred)");
        }

        public static RenderPipelineAsset CurrentAsset => GraphicsSettings.currentRenderPipeline;

        public static bool IsHDRPActive()
        {
            var asset = GraphicsSettings.currentRenderPipeline;
            if (!asset) return false;
            var type = asset.GetType();
            // HDRP asset type name check (avoid direct assembly dependency string changes)
            return type.Name.Contains("HDRenderPipelineAsset");
        }

        void LogPipelineState(string context)
        {
            var defaultAsset = GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.name : "(Built-in)";
            var overrideAsset = QualitySettings.renderPipeline ? QualitySettings.renderPipeline.name : "(None)";
            var activeAsset = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.name : "Built-in";
            Debug.Log($"[RenderPipelineMonitor] {context}\n Default: {defaultAsset}\n Override(Quality): {overrideAsset}\n Active: {activeAsset}\n HDRP Active?: {IsHDRPActive()} ");
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Render Pipeline/Log Current State")] 
        static void MenuLogState()
        {
            if (_instance)
            {
                _instance.LogPipelineState("Menu Log State");
            }
            else
            {
                Debug.Log("[RenderPipelineMonitor] No instance present in scene.");
            }
        }
#endif
    }
}
