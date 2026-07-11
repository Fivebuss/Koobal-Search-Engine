using System.Collections.Generic;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Cancels in-flight stock live-as-you-type search for a surface.
    /// Mirrors <c>StockSearchHelper.CancelPendingStockSearchForTyping</c> + SearchRoutine cancel
    /// without Unity types in the architecture project.
    /// </summary>
    /// <remarks>
    /// <para><b>Hard ban:</b> never Prefix-skip an IEnumerator search routine (null → StartCoroutine NRE).</para>
    /// <para>Live adapters stop the real coroutine / debounce; these ports track intent for tests.</para>
    /// </remarks>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal interface ISearchExecutionHalt
    {
        bool IsTypingHaltActive(SearchBarSurface surface);

        int SuppressDepth { get; }

        /// <summary>Enter apply suppress — block stock search entry while installing a custom filter.</summary>
        void EnterSuppress();

        void ExitSuppress();

        /// <summary>
        /// Cancel pending stock search work for <paramref name="surface"/> (keystroke / focus).
        /// Must be cheap — this is the no-lag path.
        /// </summary>
        void CancelPending(SearchBarSurface surface, string reason);

        /// <summary>Clear typing-halt bookkeeping when the field empties or scene exits.</summary>
        void ClearHalt(SearchBarSurface surface);

        void ClearAll();
    }

    /// <summary>Throws — documents the live boundary until wire-up.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class UnwiredSearchExecutionHalt : ISearchExecutionHalt
    {
        public int SuppressDepth => 0;

        public bool IsTypingHaltActive(SearchBarSurface surface)
        {
            throw new System.NotImplementedException(
                "UnwiredSearchExecutionHalt.IsTypingHaltActive — wire per-surface cancel (editor: CancelSearchRoutine).");
        }

        public void EnterSuppress()
        {
            throw new System.NotImplementedException(
                "UnwiredSearchExecutionHalt.EnterSuppress — bridge StockSearchGuard.EnterSuppress at wire-up.");
        }

        public void ExitSuppress()
        {
            throw new System.NotImplementedException(
                "UnwiredSearchExecutionHalt.ExitSuppress — bridge StockSearchGuard.ExitSuppress at wire-up.");
        }

        public void CancelPending(SearchBarSurface surface, string reason)
        {
            throw new System.NotImplementedException(
                "UnwiredSearchExecutionHalt.CancelPending(" + surface + ", " + reason
                + ") — wire cancel without Prefix-skipping IEnumerator search.");
        }

        public void ClearHalt(SearchBarSurface surface)
        {
            throw new System.NotImplementedException(
                "UnwiredSearchExecutionHalt.ClearHalt — wire scene/empty clear.");
        }

        public void ClearAll()
        {
            throw new System.NotImplementedException(
                "UnwiredSearchExecutionHalt.ClearAll — wire global clear on scene change.");
        }
    }

    /// <summary>
    /// In-memory halt bookkeeping for architecture self-checks / orchestrator tests.
    /// Records cancel reasons; does not touch Unity.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class RecordingSearchExecutionHalt : ISearchExecutionHalt
    {
        private readonly HashSet<SearchBarSurface> _active = new HashSet<SearchBarSurface>();
        private int _suppressDepth;

        public List<string> CallLog { get; } = new List<string>();

        public int SuppressDepth => _suppressDepth;

        public bool IsTypingHaltActive(SearchBarSurface surface)
        {
            return _active.Contains(surface);
        }

        public void EnterSuppress()
        {
            _suppressDepth++;
            CallLog.Add("EnterSuppress:" + _suppressDepth);
        }

        public void ExitSuppress()
        {
            if (_suppressDepth > 0)
            {
                _suppressDepth--;
            }

            CallLog.Add("ExitSuppress:" + _suppressDepth);
        }

        public void CancelPending(SearchBarSurface surface, string reason)
        {
            _active.Add(surface);
            CallLog.Add("CancelPending:" + surface + ":" + (reason ?? string.Empty));
        }

        public void ClearHalt(SearchBarSurface surface)
        {
            _active.Remove(surface);
            CallLog.Add("ClearHalt:" + surface);
        }

        public void ClearAll()
        {
            _active.Clear();
            CallLog.Add("ClearAll");
        }
    }
}
