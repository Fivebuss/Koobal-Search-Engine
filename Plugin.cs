using UnityEngine;

namespace PartSearchSuggest
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public sealed class EditorBootstrap : MonoBehaviour
    {
        private EditorSearchHook _hook;

        private void Awake()
        {
            StockSearchGuard.ApplyPatches();
            PartsPanelTransitionGuard.ApplyPatches();
        }

        private void Start()
        {
            Log("Editor scene detected — attaching search UI (indexes pre-built at save load).");
            _hook = gameObject.AddComponent<EditorSearchHook>();
        }

        private void OnDestroy()
        {
            PartsPanelCollapseHelper.ReleaseAllForEditorExit("EditorBootstrap.OnDestroy");
        }

        internal static void Log(string message)
        {
            Debug.Log("[Koobal] " + message);
        }

        internal static void LogWarning(string message)
        {
            Debug.LogWarning("[Koobal] " + message);
        }
    }
}
