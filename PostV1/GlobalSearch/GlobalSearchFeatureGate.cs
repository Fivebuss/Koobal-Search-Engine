namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Feature gates for global Enter-halt + tight matching across KSP search bars.
    /// </summary>
    /// <remarks>
    /// <b>Always false in this scaffolding.</b> Shipping must not reference this type until
    /// after v1 + categories/SA PostV1, and only with ModTest validation.
    /// No branding / dropdown enable flags — this feature never adds chrome.
    /// </remarks>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal static class GlobalSearchFeatureGate
    {
        /// <summary>Master enable. Scaffold default: off.</summary>
        public const bool Enabled = false;

        /// <summary>G1 — editor shared-core swap (skip when full Koobal owns field). Off.</summary>
        public const bool EnableEditorPartList = false;

        /// <summary>G2 — R&amp;D. Off.</summary>
        public const bool EnableResearchAndDevelopment = false;

        /// <summary>G3 — Tracking Station. Off.</summary>
        public const bool EnableTrackingStation = false;

        /// <summary>G4 — Craft browser. Off.</summary>
        public const bool EnableCraftBrowser = false;

        /// <summary>G5 — KSPedia + remaining confirmed surfaces. Off.</summary>
        public const bool EnableRemainingSurfaces = false;
    }
}
