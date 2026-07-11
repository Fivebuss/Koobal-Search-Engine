using System;
using PartSearchSuggest.PostV1.V2.ModSettings;

namespace PartSearchSuggest.PostV1.V2.PartsListExperience
{
    /// <summary>
    /// V2 architecture track. Default is <see cref="SlideExpand"/> (Track S).
    /// <see cref="Rebuild"/> (Track R) is optional after an explicit go/no-go — not forbidden, not assumed.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal enum PartsListArchitectureTrack
    {
        /// <summary>Track S — slide-grow stock panel + soft reflow / in-place optimize.</summary>
        SlideExpand = 0,

        /// <summary>Track R — owned parts list UI hosting icons/rows (experimental until go).</summary>
        Rebuild = 1
    }

    /// <summary>Icon size preference for parts list cells.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal enum PartsListIconSize
    {
        Compact = 0,
        Comfortable = 1,
        Large = 2
    }

    /// <summary>List presentation style.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal enum PartsListStyle
    {
        /// <summary>Stock-like icon grid.</summary>
        Grid = 0,

        /// <summary>Denser rows / tighter spacing.</summary>
        CompactList = 1,

        /// <summary>Grid with text affordances where feasible.</summary>
        Hybrid = 2
    }

    /// <summary>
    /// Pure panel geometry intent — composed from user slide-expand + transient dropdown slide.
    /// Not a fullscreen maximize / chrome-swap.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal struct PartsListPanelGeometryIntent : IEquatable<PartsListPanelGeometryIntent>
    {
        /// <summary>Horizontal shift in UI units (positive direction defined at live adapter).</summary>
        public float OffsetX { get; set; }

        public float OffsetY { get; set; }

        /// <summary>Added width beyond stock rest size.</summary>
        public float WidthDelta { get; set; }

        /// <summary>Added height beyond stock rest size (optional taller list).</summary>
        public float HeightDelta { get; set; }

        /// <summary>True when dropdown-open contribution is included.</summary>
        public bool IncludesDropdownContribution { get; set; }

        /// <summary>True when user slide-expand contribution is included.</summary>
        public bool IncludesUserExpand { get; set; }

        public static PartsListPanelGeometryIntent StockRest => default;

        public bool Equals(PartsListPanelGeometryIntent other)
        {
            return Nearly(OffsetX, other.OffsetX)
                && Nearly(OffsetY, other.OffsetY)
                && Nearly(WidthDelta, other.WidthDelta)
                && Nearly(HeightDelta, other.HeightDelta)
                && IncludesDropdownContribution == other.IncludesDropdownContribution
                && IncludesUserExpand == other.IncludesUserExpand;
        }

        public override bool Equals(object obj)
        {
            return obj is PartsListPanelGeometryIntent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = OffsetX.GetHashCode();
                hash = (hash * 397) ^ OffsetY.GetHashCode();
                hash = (hash * 397) ^ WidthDelta.GetHashCode();
                hash = (hash * 397) ^ HeightDelta.GetHashCode();
                hash = (hash * 397) ^ IncludesDropdownContribution.GetHashCode();
                hash = (hash * 397) ^ IncludesUserExpand.GetHashCode();
                return hash;
            }
        }

        private static bool Nearly(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001f;
        }
    }

    /// <summary>
    /// Transient contribution from today’s dropdown slide (CollapseHelper family).
    /// Live adapters fill magnitudes from measured UIPanelTransition deltas.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal struct DropdownSlideContribution
    {
        public bool IsDropdownOpen { get; set; }

        public float OffsetX { get; set; }

        public float OffsetY { get; set; }

        public float WidthDelta { get; set; }

        public float HeightDelta { get; set; }

        public static DropdownSlideContribution Closed => default;
    }

    /// <summary>Resolved layout preferences for applying icon size / style / track.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class PartsListLayoutPreferences
    {
        public PartsListIconSize IconSize { get; set; }

        public PartsListStyle ListStyle { get; set; }

        public bool SlideExpandEnabled { get; set; }

        public float ExpandWidthAmount { get; set; }

        public float ExpandHeightAmount { get; set; }

        public bool OrganizerCompatibilityMode { get; set; }

        public bool VirtualizationExperimental { get; set; }

        public PartsListArchitectureTrack EffectiveTrack { get; set; }

        public static PartsListLayoutPreferences FromSettings(KoobalSettingsModel model)
        {
            model = KoobalSettingsStore.Sanitize(model ?? KoobalSettingsModel.CreateDefaults());
            return new PartsListLayoutPreferences
            {
                IconSize = model.IconSize,
                ListStyle = model.ListStyle,
                SlideExpandEnabled = model.SlideExpandEnabled,
                ExpandWidthAmount = model.ExpandWidthAmount,
                ExpandHeightAmount = model.ExpandHeightAmount,
                OrganizerCompatibilityMode = model.OrganizerCompatibilityMode,
                VirtualizationExperimental = model.VirtualizationExperimental,
                EffectiveTrack = ResolveEffectiveTrack(model)
            };
        }

        /// <summary>
        /// Effective track: ForceSlideExpand wins; Rebuild requires AllowRebuildExperimental;
        /// otherwise model.ArchitectureTrack (default SlideExpand).
        /// </summary>
        public static PartsListArchitectureTrack ResolveEffectiveTrack(KoobalSettingsModel model)
        {
            model = model ?? KoobalSettingsModel.CreateDefaults();
            if (model.ForceSlideExpandTrack)
            {
                return PartsListArchitectureTrack.SlideExpand;
            }

            if (model.ArchitectureTrack == PartsListArchitectureTrack.Rebuild
                && model.AllowRebuildExperimental)
            {
                return PartsListArchitectureTrack.Rebuild;
            }

            return PartsListArchitectureTrack.SlideExpand;
        }

        /// <summary>
        /// Compose user slide-expand + dropdown contribution into one geometry intent.
        /// Dropdown does not wipe user expand — contributions are additive on each axis.
        /// </summary>
        public static PartsListPanelGeometryIntent Compose(
            PartsListLayoutPreferences prefs,
            DropdownSlideContribution dropdown)
        {
            prefs = prefs ?? FromSettings(KoobalSettingsModel.CreateDefaults());
            var intent = new PartsListPanelGeometryIntent();

            if (prefs.SlideExpandEnabled
                && prefs.EffectiveTrack == PartsListArchitectureTrack.SlideExpand)
            {
                intent.WidthDelta += Math.Max(0f, prefs.ExpandWidthAmount);
                intent.HeightDelta += Math.Max(0f, prefs.ExpandHeightAmount);
                intent.IncludesUserExpand = intent.WidthDelta > 0f || intent.HeightDelta > 0f;
            }

            if (dropdown.IsDropdownOpen)
            {
                intent.OffsetX += dropdown.OffsetX;
                intent.OffsetY += dropdown.OffsetY;
                intent.WidthDelta += dropdown.WidthDelta;
                intent.HeightDelta += dropdown.HeightDelta;
                intent.IncludesDropdownContribution = true;
            }

            return intent;
        }

        /// <summary>Nominal cell scale factor for soft reflow (architecture heuristic).</summary>
        public static float IconScaleFactor(PartsListIconSize size)
        {
            switch (size)
            {
                case PartsListIconSize.Compact:
                    return 0.75f;
                case PartsListIconSize.Large:
                    return 1.25f;
                default:
                    return 1f;
            }
        }
    }

    /// <summary>
    /// Pure go/no-go helper for choosing Track R over continuing Track S.
    /// Does not flip gates — records a decision for docs / ModTest.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal static class PartsListTrackGoNoGo
    {
        public static PartsListTrackDecision Evaluate(PartsListTrackEvidence evidence)
        {
            evidence = evidence ?? new PartsListTrackEvidence();

            if (evidence.ForceStayOnSlideExpand)
            {
                return new PartsListTrackDecision
                {
                    RecommendedTrack = PartsListArchitectureTrack.SlideExpand,
                    GoRebuild = false,
                    Reason = "Forced stay on Track S."
                };
            }

            int score = 0;
            if (evidence.SlideExpandMissedLagTarget)
            {
                score++;
            }

            if (evidence.SoftReflowHitPrefabCeiling)
            {
                score++;
            }

            if (evidence.DropdownComposeTooFragile)
            {
                score++;
            }

            if (evidence.StockBindHasNoRecycleHook)
            {
                score++;
            }

            if (evidence.ProductAcceptsRebuildRisk)
            {
                score++;
            }

            bool go = score >= 3 && evidence.ResearchSpikeCompleted;
            return new PartsListTrackDecision
            {
                RecommendedTrack = go
                    ? PartsListArchitectureTrack.Rebuild
                    : PartsListArchitectureTrack.SlideExpand,
                GoRebuild = go,
                CriteriaMet = score,
                Reason = go
                    ? "Go: Track R criteria met (score=" + score + "/5, spike done)."
                    : "No-go: stay on Track S (score=" + score + "/5, spikeDone="
                      + evidence.ResearchSpikeCompleted + ")."
            };
        }
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class PartsListTrackEvidence
    {
        public bool ResearchSpikeCompleted { get; set; }
        public bool SlideExpandMissedLagTarget { get; set; }
        public bool SoftReflowHitPrefabCeiling { get; set; }
        public bool DropdownComposeTooFragile { get; set; }
        public bool StockBindHasNoRecycleHook { get; set; }
        public bool ProductAcceptsRebuildRisk { get; set; }
        public bool ForceStayOnSlideExpand { get; set; }
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class PartsListTrackDecision
    {
        public PartsListArchitectureTrack RecommendedTrack { get; set; }
        public bool GoRebuild { get; set; }
        public int CriteriaMet { get; set; }
        public string Reason { get; set; }
    }
}
