namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Stock (or stock-like) search UI surfaces that may receive Enter-to-search halt
    /// + tightened matching. See <c>docs/POST_V1_GLOBAL_SEARCH_HALT.md</c>.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal enum SearchBarSurface
    {
        /// <summary>VAB/SPH PartCategorizer search — proven in Native Search / full Koobal.</summary>
        EditorPartList = 0,

        /// <summary>R&amp;D tech tree filter — needs runtime confirmation.</summary>
        ResearchAndDevelopment = 1,

        /// <summary>Tracking Station vessel list filter — needs runtime confirmation.</summary>
        TrackingStation = 2,

        /// <summary>KSPedia / knowledge base search — needs runtime confirmation.</summary>
        KSPedia = 3,

        /// <summary>Settings, difficulty, or in-game mods list filters — needs runtime confirmation.</summary>
        SettingsOrModsList = 4,

        /// <summary>Mission Control agency / contracts browser — may have no search bar.</summary>
        AgenciesOrContracts = 5,

        /// <summary>Save/load craft browser name filter — needs runtime confirmation.</summary>
        CraftBrowser = 6,

        /// <summary>Action Groups editor filter — needs runtime confirmation.</summary>
        ActionGroups = 7,

        /// <summary>Astronaut Complex / roster filters — needs runtime confirmation.</summary>
        KerbalOrRoster = 8,

        /// <summary>Catch-all after facility audit.</summary>
        OtherBrowser = 9
    }

    /// <summary>How confident we are that a stock search bar exists and is live-as-you-type.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal enum SearchSurfaceConfidence
    {
        /// <summary>Shipping editor path already implements halt + tight Enter.</summary>
        Proven = 0,

        /// <summary>Known UI family; class names / events still need a runtime dump.</summary>
        Likely = 1,

        /// <summary>Do not patch until a scene audit confirms a search control.</summary>
        NeedsRuntimeConfirmation = 2
    }
}
