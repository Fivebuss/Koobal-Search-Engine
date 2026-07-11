using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Static metadata for one <see cref="SearchBarSurface"/> — rollout order, ownership rules,
    /// and honesty tags. No Unity / KSP types.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class SearchSurfaceDescriptor
    {
        public SearchBarSurface Surface { get; set; }

        public string DisplayName { get; set; }

        public SearchSurfaceConfidence Confidence { get; set; }

        /// <summary>
        /// Suggested rollout phase from the plan (G1 editor … G5 remaining).
        /// </summary>
        public string RolloutPhase { get; set; }

        /// <summary>
        /// When true, full Koobal’s editor hook already owns halt + Enter (+ dropdown).
        /// Global Search Halt must not double-attach.
        /// </summary>
        public bool OwnedByFullKoobalDropdownWhenPresent { get; set; }

        /// <summary>
        /// When true, Native Search standalone already implements this surface.
        /// </summary>
        public bool ImplementedByNativeSearchStandalone { get; set; }

        /// <summary>Short notes for wire-up / runtime audit.</summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Inventory of known / candidate search bars. Pure data — used by composition and docs.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal static class SearchSurfaceRegistry
    {
        private static readonly SearchSurfaceDescriptor[] All =
        {
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.EditorPartList,
                DisplayName = "VAB/SPH part search",
                Confidence = SearchSurfaceConfidence.Proven,
                RolloutPhase = "G1",
                OwnedByFullKoobalDropdownWhenPresent = true,
                ImplementedByNativeSearchStandalone = true,
                Notes = "PartCategorizer.searchField; CancelSearchRoutine; NativeEnterMatcher / SuggestionIndex Enter path."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.ResearchAndDevelopment,
                DisplayName = "R&D tech tree",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G2",
                Notes = "Confirm RD UI filter field + live rebuild cost before Harmony."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.TrackingStation,
                DisplayName = "Tracking Station",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G3",
                Notes = "Vessel list filter control — needs scene audit."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.CraftBrowser,
                DisplayName = "Save/load craft browser",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G4",
                Notes = "CraftBrowserDialog name filter likely live-as-you-type."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.KSPedia,
                DisplayName = "KSPedia",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G5",
                Notes = "Knowledge base search UI — confirm scene + control type."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.SettingsOrModsList,
                DisplayName = "Settings / mods lists",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G5",
                Notes = "Only hook confirmed search/filter fields — not every settings TMP."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.AgenciesOrContracts,
                DisplayName = "Agencies / contracts",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G5",
                Notes = "May have no search bar — skip if audit finds none."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.ActionGroups,
                DisplayName = "Action Groups",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G5",
                Notes = "Stock AG editor filter if present."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.KerbalOrRoster,
                DisplayName = "Kerbal / roster",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G5",
                Notes = "Astronaut Complex / hire UI filters."
            },
            new SearchSurfaceDescriptor
            {
                Surface = SearchBarSurface.OtherBrowser,
                DisplayName = "Other browser",
                Confidence = SearchSurfaceConfidence.NeedsRuntimeConfirmation,
                RolloutPhase = "G5",
                Notes = "Catch-all after facility audit."
            }
        };

        public static IReadOnlyList<SearchSurfaceDescriptor> GetAll()
        {
            return All;
        }

        public static SearchSurfaceDescriptor Get(SearchBarSurface surface)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Surface == surface)
                {
                    return All[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Surfaces the global module may attach to when full Koobal owns the editor dropdown.
        /// </summary>
        public static IEnumerable<SearchSurfaceDescriptor> GetEligibleWhenFullKoobalPresent()
        {
            for (int i = 0; i < All.Length; i++)
            {
                SearchSurfaceDescriptor d = All[i];
                if (!d.OwnedByFullKoobalDropdownWhenPresent)
                {
                    yield return d;
                }
            }
        }
    }
}
