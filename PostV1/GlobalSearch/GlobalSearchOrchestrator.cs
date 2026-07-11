using System.Collections.Generic;
using System.Linq;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>Outcome of an Enter/submit apply attempt.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal enum GlobalSearchApplyStatus
    {
        Applied = 0,
        NoMatches = 1,
        QueryTooShort = 2,
        ApplyFailed = 3,
        SurfaceSkipped = 4
    }

    /// <summary>Result DTO for orchestrator Enter path.</summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class GlobalSearchApplyResult
    {
        public GlobalSearchApplyStatus Status { get; set; }

        public int MatchCount { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// Type → halt; Enter → tight match → apply. No branding, no dropdown, no predictive UI.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class GlobalSearchOrchestrator
    {
        private readonly ISearchExecutionHalt _halt;
        private readonly ITightSearchMatcher _matcher;
        private readonly ISearchResultApplier _applier;
        private readonly bool _skipEditorWhenFullKoobalPresent;
        private readonly bool _fullKoobalPresent;

        public GlobalSearchOrchestrator(
            ISearchExecutionHalt halt,
            ITightSearchMatcher matcher,
            ISearchResultApplier applier,
            bool fullKoobalPresent = false,
            bool skipEditorWhenFullKoobalPresent = true)
        {
            _halt = halt;
            _matcher = matcher;
            _applier = applier;
            _fullKoobalPresent = fullKoobalPresent;
            _skipEditorWhenFullKoobalPresent = skipEditorWhenFullKoobalPresent;
        }

        public ITightSearchMatcher Matcher => _matcher;

        public ISearchExecutionHalt Halt => _halt;

        public ISearchResultApplier Applier => _applier;

        /// <summary>
        /// True when this module must not attach listeners for <paramref name="surface"/>.
        /// </summary>
        public bool ShouldSkipSurface(SearchBarSurface surface)
        {
            if (!_skipEditorWhenFullKoobalPresent || !_fullKoobalPresent)
            {
                return false;
            }

            SearchSurfaceDescriptor d = SearchSurfaceRegistry.Get(surface);
            return d != null && d.OwnedByFullKoobalDropdownWhenPresent;
        }

        /// <summary>Keystroke / focus — cancel pending stock search (no match work).</summary>
        public void OnTyping(SearchBarSurface surface, string reason)
        {
            if (ShouldSkipSurface(surface))
            {
                return;
            }

            _halt.CancelPending(surface, reason ?? "typing");
        }

        /// <summary>Empty query / blur / scene exit.</summary>
        public void OnClear(SearchBarSurface surface)
        {
            if (ShouldSkipSurface(surface))
            {
                return;
            }

            _applier.ClearFilter(surface);
            _halt.ClearHalt(surface);
        }

        /// <summary>Enter / submit — tight match then apply; no loose stock fallback.</summary>
        public GlobalSearchApplyResult OnEnter(SearchBarSurface surface, string query)
        {
            if (ShouldSkipSurface(surface))
            {
                return new GlobalSearchApplyResult
                {
                    Status = GlobalSearchApplyStatus.SurfaceSkipped,
                    Message = "Surface owned by full Koobal dropdown — global halt skipped."
                };
            }

            string trimmed = (query ?? string.Empty).Trim();
            if (Shared.PostV1QueryGuards.IsTooShortForSuggestions(trimmed))
            {
                return new GlobalSearchApplyResult
                {
                    Status = GlobalSearchApplyStatus.QueryTooShort,
                    Message = "Query too short for Enter search."
                };
            }

            _halt.CancelPending(surface, "enter-pre-apply");

            List<TightMatchResult> matches = _matcher.Match(trimmed).ToList();
            if (matches.Count == 0)
            {
                return new GlobalSearchApplyResult
                {
                    Status = GlobalSearchApplyStatus.NoMatches,
                    MatchCount = 0,
                    Message = "No tight matches — list unchanged (stock fallback disabled)."
                };
            }

            _halt.EnterSuppress();
            try
            {
                bool ok = _applier.Apply(surface, matches, trimmed);
                if (!ok)
                {
                    return new GlobalSearchApplyResult
                    {
                        Status = GlobalSearchApplyStatus.ApplyFailed,
                        MatchCount = matches.Count,
                        Message = "Applier returned false."
                    };
                }

                return new GlobalSearchApplyResult
                {
                    Status = GlobalSearchApplyStatus.Applied,
                    MatchCount = matches.Count,
                    Message = "Applied " + matches.Count + " match(es)."
                };
            }
            finally
            {
                _halt.ExitSuppress();
            }
        }
    }
}
