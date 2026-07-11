using UnityEngine;

namespace PartSearchSuggest
{
    /// <summary>
    /// Runs on every scene transition. Clears indexes on main menu only; builds indexes
    /// during the loading screen after the user selects a new or existing save (KSC, flight,
    /// tracking station, or editor fallback). Never indexes on main menu.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class GameLoadBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                GameLoadIndexService.InvalidateForMainMenu();
            }
        }

        private void Start()
        {
            if (!GameLoadIndexService.ShouldBuildForCurrentScene())
            {
                return;
            }

            StartCoroutine(GameLoadIndexService.BuildIfNeeded(this));
        }
    }
}
