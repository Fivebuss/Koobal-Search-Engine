using System;
using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.V2.PartsListExperience
{
    /// <summary>
    /// Track S — apply slide-expand / width-height geometry without replacing EditorPartList.
    /// Same interaction family as shipping PartsPanelCollapseHelper dropdown slide.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface IPartsListSlideExpandController
    {
        /// <summary>Last applied intent (stock rest if never applied).</summary>
        PartsListPanelGeometryIntent LastAppliedIntent { get; }

        /// <summary>Apply composed geometry (user expand + optional dropdown contribution).</summary>
        void ApplyGeometry(PartsListPanelGeometryIntent intent);

        /// <summary>
        /// Return to user-expanded rest (if expand on) or stock rest — never blindly stock
        /// when user expand is still enabled.
        /// </summary>
        void RestoreRest(PartsListLayoutPreferences prefs);

        /// <summary>Notify dropdown open/close so live adapter can recompose.</summary>
        void NotifyDropdownOpen(bool open, DropdownSlideContribution contribution);
    }

    /// <summary>Throws — documents live CollapseHelper / RectTransform wire-up.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredPartsListSlideExpandController : IPartsListSlideExpandController
    {
        public PartsListPanelGeometryIntent LastAppliedIntent => default;

        public void ApplyGeometry(PartsListPanelGeometryIntent intent)
        {
            throw new NotImplementedException(
                "UnwiredPartsListSlideExpandController.ApplyGeometry — wire UIPanelTransition / RectTransform like PartsPanelCollapseHelper.");
        }

        public void RestoreRest(PartsListLayoutPreferences prefs)
        {
            throw new NotImplementedException(
                "UnwiredPartsListSlideExpandController.RestoreRest — restore user-expand rest or stock; do not fight dropdown.");
        }

        public void NotifyDropdownOpen(bool open, DropdownSlideContribution contribution)
        {
            throw new NotImplementedException(
                "UnwiredPartsListSlideExpandController.NotifyDropdownOpen — compose with SearchDropdownPanel collapse path.");
        }
    }

    /// <summary>Recording double — pure bookkeeping for self-check.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class RecordingPartsListSlideExpandController : IPartsListSlideExpandController
    {
        public PartsListPanelGeometryIntent LastAppliedIntent { get; private set; }

        public int ApplyCount { get; private set; }

        public int RestoreCount { get; private set; }

        public bool DropdownOpen { get; private set; }

        public List<PartsListPanelGeometryIntent> AppliedHistory { get; } =
            new List<PartsListPanelGeometryIntent>();

        public void ApplyGeometry(PartsListPanelGeometryIntent intent)
        {
            LastAppliedIntent = intent;
            AppliedHistory.Add(intent);
            ApplyCount++;
        }

        public void RestoreRest(PartsListLayoutPreferences prefs)
        {
            RestoreCount++;
            PartsListPanelGeometryIntent rest = PartsListLayoutPreferences.Compose(
                prefs,
                DropdownSlideContribution.Closed);
            ApplyGeometry(rest);
        }

        public void NotifyDropdownOpen(bool open, DropdownSlideContribution contribution)
        {
            DropdownOpen = open;
            contribution.IsDropdownOpen = open;
            // Caller supplies prefs via separate Apply after compose in services.
            _ = contribution;
        }
    }

    /// <summary>
    /// Soft layout port — icon size / list style reflow on stock hosts (Track S)
    /// or hints for Track R host.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface IPartsListLayout
    {
        void ApplyLayout(PartsListLayoutPreferences prefs);

        PartsListLayoutPreferences LastApplied { get; }
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredPartsListLayout : IPartsListLayout
    {
        public PartsListLayoutPreferences LastApplied => null;

        public void ApplyLayout(PartsListLayoutPreferences prefs)
        {
            throw new NotImplementedException(
                "UnwiredPartsListLayout.ApplyLayout — wire soft icon scale / spacing on stock PartIcon hosts.");
        }
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class RecordingPartsListLayout : IPartsListLayout
    {
        public PartsListLayoutPreferences LastApplied { get; private set; }

        public int ApplyCount { get; private set; }

        public void ApplyLayout(PartsListLayoutPreferences prefs)
        {
            LastApplied = prefs;
            ApplyCount++;
        }
    }

    /// <summary>
    /// Track R — optional owned parts list host. Gated; not default.
    /// Still should consume organizer/filter predicates where feasible.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface IPartsListRebuildHost
    {
        bool IsActive { get; }

        /// <summary>Show owned list and optionally suppress stock icon binds.</summary>
        void Activate(PartsListLayoutPreferences prefs);

        void Deactivate();

        /// <summary>Rebind visible rows from a snapshot of part ids (architecture-level).</summary>
        void BindPartIds(IReadOnlyList<string> partIds);

        /// <summary>Clear owned rows.</summary>
        void Clear();
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredPartsListRebuildHost : IPartsListRebuildHost
    {
        public bool IsActive => false;

        public void Activate(PartsListLayoutPreferences prefs)
        {
            throw new NotImplementedException(
                "UnwiredPartsListRebuildHost.Activate — Track R only after go/no-go; wire owned scroll/virtual list.");
        }

        public void Deactivate()
        {
            throw new NotImplementedException(
                "UnwiredPartsListRebuildHost.Deactivate — restore stock EditorPartList visibility.");
        }

        public void BindPartIds(IReadOnlyList<string> partIds)
        {
            throw new NotImplementedException(
                "UnwiredPartsListRebuildHost.BindPartIds — wire icon/row factory from filtered part set.");
        }

        public void Clear()
        {
            throw new NotImplementedException(
                "UnwiredPartsListRebuildHost.Clear — wire owned list clear.");
        }
    }

    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class RecordingPartsListRebuildHost : IPartsListRebuildHost
    {
        public bool IsActive { get; private set; }

        public int BindCount { get; private set; }

        public IReadOnlyList<string> LastBoundIds { get; private set; } = Array.Empty<string>();

        public void Activate(PartsListLayoutPreferences prefs)
        {
            _ = prefs;
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void BindPartIds(IReadOnlyList<string> partIds)
        {
            LastBoundIds = partIds ?? Array.Empty<string>();
            BindCount++;
        }

        public void Clear()
        {
            LastBoundIds = Array.Empty<string>();
            BindCount++;
        }
    }
}
