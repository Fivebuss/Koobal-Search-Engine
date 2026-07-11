using HarmonyLib;
using KSP.UI.Screens;

namespace PartSearchSuggest
{
    /// <summary>
    /// Minimal race guard (v0.6.7-era): while Koobal is applying a suggestion filter (EnterSuppress)
    /// or a Koobal custom filter is active, block stock's own async SearchRoutine / SearchFilterResult
    /// from overwriting the applied result. Koobal does NOT block stock typing, refresh, tab, or
    /// subassembly flows — those run 100% natively.
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

        private static bool ShouldBlockStockSearchRoutine()
        {
            if (IsSuppressed || HasActiveCustomFilter)
            {
                EditorBootstrap.Log("Blocked stock SearchRoutine while Koobal filter apply/active.");
                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(BasePartCategorizer), "SearchRoutine")]
        private static class BaseSearchRoutinePatch
        {
            private static bool Prefix()
            {
                return !ShouldBlockStockSearchRoutine();
            }
        }

        [HarmonyPatch(typeof(PartCategorizer), "SearchRoutine")]
        private static class PartCategorizerSearchRoutinePatch
        {
            private static bool Prefix()
            {
                return !ShouldBlockStockSearchRoutine();
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
