using System;
using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.SearchHistory
{
    /// <summary>
    /// Cosmetic UI port: per-history-row delete control on the editor dropdown.
    /// Data remove lives in <see cref="ISearchHistoryStore"/>; this port is the bulk of later wire-up.
    /// </summary>
    /// <remarks>
    /// Live adapter attaches a ✕ / trash button on each history row, isolates clicks from
    /// row-apply, and raises <see cref="OnRemoveRequested"/>. Architecture doubles only
    /// record bind/clear calls.
    /// </remarks>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal interface IHistoryRowChrome
    {
        /// <summary>Raised when the user activates a row's delete control (stable entry id).</summary>
        event Action<string> OnRemoveRequested;

        /// <summary>
        /// Bind or refresh per-row delete affordances for the current history snapshot.
        /// No-op when feature gate is off at wire-up.
        /// </summary>
        void BindRows(IReadOnlyList<HistoryRowChromeRequest> rows);

        /// <summary>Remove all per-row delete controls (dropdown hide / non-history mode).</summary>
        void Clear();
    }

    /// <summary>One history row's chrome bind request (no Unity types).</summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class HistoryRowChromeRequest
    {
        public string EntryId { get; set; }

        public string DisplayText { get; set; }

        /// <summary>0-based index in the current visible history list (newest = 0).</summary>
        public int VisibleIndex { get; set; }
    }

    /// <summary>Throws — documents the live SearchDropdownPanel boundary until wire-up.</summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class UnwiredHistoryRowChrome : IHistoryRowChrome
    {
        public event Action<string> OnRemoveRequested
        {
            add { }
            remove { }
        }

        public void BindRows(IReadOnlyList<HistoryRowChromeRequest> rows)
        {
            throw new NotImplementedException(
                "UnwiredHistoryRowChrome.BindRows — wire per-row ✕ on SearchDropdownPanel history rows.");
        }

        public void Clear()
        {
            throw new NotImplementedException(
                "UnwiredHistoryRowChrome.Clear — wire chrome teardown on dropdown hide / mode change.");
        }
    }

    /// <summary>
    /// Records bind/clear/remove for architecture self-checks without Unity.
    /// </summary>
    [PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]
    internal sealed class RecordingHistoryRowChrome : IHistoryRowChrome
    {
        public event Action<string> OnRemoveRequested;

        public List<string> CallLog { get; } = new List<string>();

        public List<HistoryRowChromeRequest> LastBoundRows { get; } = new List<HistoryRowChromeRequest>();

        public int BindCount { get; private set; }

        public int ClearCount { get; private set; }

        public void BindRows(IReadOnlyList<HistoryRowChromeRequest> rows)
        {
            BindCount++;
            LastBoundRows.Clear();
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i] != null)
                    {
                        LastBoundRows.Add(rows[i]);
                    }
                }
            }

            CallLog.Add("BindRows:" + LastBoundRows.Count);
        }

        public void Clear()
        {
            ClearCount++;
            LastBoundRows.Clear();
            CallLog.Add("Clear");
        }

        /// <summary>Simulate user clicking ✕ on a bound row (for self-check).</summary>
        public void SimulateRemoveClick(string entryId)
        {
            CallLog.Add("SimulateRemoveClick:" + (entryId ?? string.Empty));
            OnRemoveRequested?.Invoke(entryId);
        }
    }
}
