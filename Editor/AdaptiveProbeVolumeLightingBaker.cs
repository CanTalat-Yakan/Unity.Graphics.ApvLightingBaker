#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEssentials
{
    [ExecuteAlways]
    [RequireComponent(typeof(ProbeReferenceVolumeProvider))]
    public class AdaptiveProbeVolumeLightingBaker : MonoBehaviour
    {
        public static bool IsBakingInProgress => Lightmapping.isRunning;

        [Button]
        public bool BakeLightingScenario(string scenarioName, bool async = false)
        {
            if (IsBakingInProgress)
                return false;

            if (string.IsNullOrEmpty(scenarioName))
            {
                Debug.LogWarning("Scenario name cannot be null or empty.");
                return false;
            }

            if (!AddAndApplyLightingScenario(scenarioName))
            {
                Debug.LogWarning($"Failed to add or apply lighting scenario '{scenarioName}'. " +
                    $"Ensure the Probe Reference Volume is initialized and a baking set is created.");
                return false;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = async ? Lightmapping.BakeAsync() : Lightmapping.Bake();
            stopwatch.Stop();

            if (result)
                Debug.Log($"Successfully baked scenario '{scenarioName}' in {stopwatch.Elapsed.TotalSeconds:0.00} seconds.");

            return result;
        }

        public bool AddAndApplyLightingScenario(string scenarioName)
        {
            var bakingSet = ProbeReferenceVolumeProvider.Volume?.currentBakingSet;
            if (bakingSet == null)
            {
                Debug.LogWarning("No baking set found. Ensure the Probe Reference Volume is initialized and a baking set is created.");
                return false;
            }

            bakingSet.TryAddScenario(scenarioName);
            ProbeReferenceVolumeProvider.Volume.lightingScenario = scenarioName;

            return true;
        }

        [Button]
        public void ConvertAllMeshesToProbeVolumesGI()
        {
            int convertedCount = 0;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    var meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var meshRenderer in meshRenderers)
                    {
                        meshRenderer.receiveGI = ReceiveGI.LightProbes;
                        meshRenderer.lightProbeUsage = LightProbeUsage.UseProxyVolume;
                        EditorUtility.SetDirty(meshRenderer);
                        convertedCount++;
                    }
                }
            }
            Debug.Log($"Converted {convertedCount} MeshRenderers in loaded scenes to receive GI from Probe Volumes.");
        }
    }
}
#endif