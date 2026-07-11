using HarmonyLib;
using KSP.UI.Screens;

namespace PartSearchSuggest
{
    /// <summary>
    /// Minimal race guard (v0.6.7-era): while Koobal is applying a suggestion filter (EnterSuppress)
    /// or a Koobal custom filter is active, block stock's own async SearchFilterResult
    /// from overwriting the applied result. Koobal does NOT block stock typing, refresh, tab, or
    /// subassembly flows — those run 100% natively.
    ///
    /// IMPORTANT: never Harmony-skip <see cref="BasePartCategorizer"/> SearchRoutine.
    /// Prefix-skipping an IEnumerator method returns null; stock SearchStart then calls
    /// StartCoroutine(null) and throws ("routine is null"). Block SearchStart (void) instead
    /// while suppressed, and clear the custom-filter guard when stock SearchStart runs.
    /// </summary>
    internal static class StockSearchGuard
    {
        private const string StockTextSearchFilterId = "SearchFilter_";

        private static int _suppressDepth;
        private static string _activeCustomFilterId;

        internal static bool IsSuppressed => _suppressDepth > 0;

        internal static bool HasActiveCustomFilter =>
            !string.IsNullOrEmpty(_activeCustomFilterId);

        internal readonly struct SuppressScope : System.IDisposable
        {
            private readonly bool _active;

            internal SuppressScope(bool active)
            {
                _active = active;
                if (_active)
                {
                    EnterSuppress();
                }
            }

            public void Dispose()
            {
                if (_active)
                {
                    ExitSuppress();
                }
            }
        }

        internal static SuppressScope EnterSuppressScope()
        {
            return new SuppressScope(true);
        }

        internal static void EnterSuppress()
        {
            _suppressDepth++;
        }

        internal static void ExitSuppress()
        {
            if (_suppressDepth > 0)
            {
                _suppressDepth--;
            }
        }

        internal static void SetActiveCustomFilter(string filterId)
        {
            _activeCustomFilterId = filterId;
        }

        internal static void ClearActiveCustomFilter()
        {
            _activeCustomFilterId = null;
        }

        internal static bool ShouldBlockStockTextFilter(EditorPartListFilter<AvailablePart> filter)
        {
            if (filter == null)
            {
                return false;
            }

            if (!string.Equals(filter.ID, StockTextSearchFilterId, System.StringComparison.Ordinal))
            {
                return false;
            }

            return IsSuppressed || HasActiveCustomFilter;
        }

        internal static void ApplyPatches()
        {
            try
            {
                Harmony harmony = new Harmony("KoobalSearchEngine.StockSearchGuard");
                HarmonyPatchHelper.PatchNestedTypes(harmony, typeof(StockSearchGuard));
                EditorBootstrap.Log("Stock search guard patches applied (minimal race guard).");
            }
            catch (System.Exception ex)
            {
                EditorBootstrap.LogWarning(
                    "StockSearchGuard patch failed — stock search race guard disabled: " + ex.Message);
            }
        }

        /// <summary>
        /// Skip stock SearchStart only while Koobal is mid-apply. Safe for a void method.
        /// When stock SearchStart runs after an apply, release the custom-filter race guard
        /// so SearchRoutine can return a real IEnumerator (typing works without blur/refocus).
        /// </summary>
        [HarmonyPatch(typeof(BasePartCategorizer), "SearchStart")]
        private static class BaseSearchStartPatch
        {
            private static bool Prefix()
            {
                if (IsSuppressed)
                {
                    EditorBootstrap.Log("Blocked stock SearchStart while Koobal filter apply.");
                    return false;
                }

                if (HasActiveCustomFilter)
                {
                    ClearActiveCustomFilter();
                    EditorBootstrap.Log(
                        "Cleared active custom filter so stock SearchStart/SearchRoutine can run.");
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PartCategorizer), "SearchFilterResult")]
        private static class SearchFilterResultPatch
        {
            private static bool Prefix(EditorPartListFilter<AvailablePart> filter)
            {
                if (ShouldBlockStockTextFilter(filter))
                {
                    EditorBootstrap.Log(
                        "Blocked stock SearchFilter_ overwrite (active custom filter='"
                        + (_activeCustomFilterId ?? string.Empty)
                        + "').");
                    return false;
                }

                if (filter != null && filter.ID != null && filter.ID.StartsWith("KoobalSearchEngine_", System.StringComparison.Ordinal))
                {
                    SetActiveCustomFilter(filter.ID);
                }
                else if (filter == null)
                {
                    ClearActiveCustomFilter();
                }

                return true;
            }
        }
    }
}
