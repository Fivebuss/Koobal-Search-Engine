using System;
using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.V2.PartsListExperience
{
    /// <summary>Known organizer / extension families for compatibility.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal enum PartsListOrganizerKind
    {
        None = 0,
        Stock = 1,
        CommunityCategoryKit = 2,
        FilterExtension = 3,
        CustomCategoryCfg = 4,
        PartsListReskin = 5,
        Unknown = 9
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class PartsListOrganizerDetection
    {
        public PartsListOrganizerKind PrimaryKind { get; set; }

        public IReadOnlyList<PartsListOrganizerKind> DetectedKinds { get; set; }

        public string Detail { get; set; }

        public static PartsListOrganizerDetection StockOnly => new PartsListOrganizerDetection
        {
            PrimaryKind = PartsListOrganizerKind.Stock,
            DetectedKinds = new[] { PartsListOrganizerKind.Stock },
            Detail = "Stock only"
        };
    }

    /// <summary>
    /// Compatibility bridge for CCK / filter extensions / reskins.
    /// Shared by Track S and Track R.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface IPartsListOrganizerBridge
    {
        PartsListOrganizerDetection LastDetection { get; }

        PartsListOrganizerDetection Detect();

        /// <summary>True when Koobal should not own category/displayType / heavy reflow.</summary>
        bool ShouldYieldOwnership(PartsListOrganizerDetection detection);

        /// <summary>
        /// When compatibility mode is on and organizers present: layout/geometry only,
        /// skip aggressive icon rewrite.
        /// </summary>
        bool PreferLayoutOnly(PartsListOrganizerDetection detection, bool compatibilityModeOn);
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredPartsListOrganizerBridge : IPartsListOrganizerBridge
    {
        public PartsListOrganizerDetection LastDetection => null;

        public PartsListOrganizerDetection Detect()
        {
            throw new NotImplementedException(
                "UnwiredPartsListOrganizerBridge.Detect — wire assembly/type probes without hard refs where possible.");
        }

        public bool ShouldYieldOwnership(PartsListOrganizerDetection detection)
        {
            throw new NotImplementedException(
                "UnwiredPartsListOrganizerBridge.ShouldYieldOwnership — wire yield policy.");
        }

        public bool PreferLayoutOnly(PartsListOrganizerDetection detection, bool compatibilityModeOn)
        {
            throw new NotImplementedException(
                "UnwiredPartsListOrganizerBridge.PreferLayoutOnly — wire layout-only policy.");
        }
    }

    /// <summary>
    /// Pure policy double — detection injected; used in self-check without Unity probes.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class RecordingPartsListOrganizerBridge : IPartsListOrganizerBridge
    {
        private PartsListOrganizerDetection _detection = PartsListOrganizerDetection.StockOnly;

        public PartsListOrganizerDetection LastDetection => _detection;

        public int DetectCount { get; private set; }

        public void Seed(PartsListOrganizerDetection detection)
        {
            _detection = detection ?? PartsListOrganizerDetection.StockOnly;
        }

        public PartsListOrganizerDetection Detect()
        {
            DetectCount++;
            return _detection;
        }

        public bool ShouldYieldOwnership(PartsListOrganizerDetection detection)
        {
            detection = detection ?? PartsListOrganizerDetection.StockOnly;
            return detection.PrimaryKind == PartsListOrganizerKind.CommunityCategoryKit
                || detection.PrimaryKind == PartsListOrganizerKind.PartsListReskin
                || detection.PrimaryKind == PartsListOrganizerKind.FilterExtension;
        }

        public bool PreferLayoutOnly(PartsListOrganizerDetection detection, bool compatibilityModeOn)
        {
            if (!compatibilityModeOn)
            {
                return false;
            }

            return ShouldYieldOwnership(detection);
        }
    }

    /// <summary>
    /// Virtualization port — Track S attempts in-place; Track R owns recycle window.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface IPartsListVirtualizationPort
    {
        bool IsEnabled { get; }

        /// <summary>Configure viewport window (indices into filtered part set).</summary>
        void SetWindow(int firstVisibleIndex, int visibleCount, int overscan);

        /// <summary>Hint to defer a full rebuild to next frame / coalesced pass.</summary>
        void RequestDeferredRebuild(string reason);

        void Clear();
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredPartsListVirtualizationPort : IPartsListVirtualizationPort
    {
        public bool IsEnabled => false;

        public void SetWindow(int firstVisibleIndex, int visibleCount, int overscan)
        {
            throw new NotImplementedException(
                "UnwiredPartsListVirtualizationPort.SetWindow — wire recycle window (S in-place or R owned).");
        }

        public void RequestDeferredRebuild(string reason)
        {
            throw new NotImplementedException(
                "UnwiredPartsListVirtualizationPort.RequestDeferredRebuild(" + reason
                + ") — wire coalesce Refresh.");
        }

        public void Clear()
        {
            throw new NotImplementedException(
                "UnwiredPartsListVirtualizationPort.Clear — wire virtualization reset.");
        }
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class RecordingPartsListVirtualizationPort : IPartsListVirtualizationPort
    {
        public bool IsEnabled { get; set; }

        public int FirstVisibleIndex { get; private set; }

        public int VisibleCount { get; private set; }

        public int Overscan { get; private set; }

        public int DeferredRebuildRequests { get; private set; }

        public string LastDeferReason { get; private set; }

        public void SetWindow(int firstVisibleIndex, int visibleCount, int overscan)
        {
            FirstVisibleIndex = firstVisibleIndex;
            VisibleCount = visibleCount;
            Overscan = overscan;
        }

        public void RequestDeferredRebuild(string reason)
        {
            DeferredRebuildRequests++;
            LastDeferReason = reason ?? string.Empty;
        }

        public void Clear()
        {
            FirstVisibleIndex = 0;
            VisibleCount = 0;
            Overscan = 0;
        }
    }
}
