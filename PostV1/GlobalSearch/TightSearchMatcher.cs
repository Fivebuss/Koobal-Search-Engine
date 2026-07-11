using System;
using System.Collections.Generic;
using System.Text;
using PartSearchSuggest.PostV1.Shared;

namespace PartSearchSuggest.PostV1.GlobalSearch
{
    /// <summary>
    /// Tightened Enter-query matcher. Contract mirrors <c>KoobalNativeSearch.NativeEnterMatcher</c>:
    /// title/name/tags/category/module/manufacturer/tech/auto-tags — no description-only hits,
    /// no predictive UI, Match only on explicit Enter/submit.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal interface ITightSearchMatcher
    {
        int ItemCount { get; }

        void Clear();

        void BuildFromSnapshot(SearchableItemSnapshotSet set);

        /// <summary>
        /// Yields qualifying matches for <paramref name="query"/>. Empty when too short or no hits.
        /// Does not mutate UI.
        /// </summary>
        IEnumerable<TightMatchResult> Match(string query);
    }

    /// <summary>
    /// Pure NativeEnterMatcher-style scoring on <see cref="SearchableItemSnapshot"/> rows.
    /// Unit-testable without Unity / PartLoader.
    /// </summary>
    [PostV1Phase(PostV1Phase.D_GlobalSearchHalt)]
    internal sealed class TightSearchMatcher : ITightSearchMatcher
    {
        // Score bands — keep aligned with NativeEnterMatcher / SuggestionIndex.
        private const int TitlePrefix = 0;
        private const int TitleContains = 1;
        private const int NamePrefix = 2;
        private const int NameContains = 3;
        private const int CategoryPrefix = 4;
        private const int CategoryContains = 5;
        private const int ModulePrefix = 6;
        private const int ModuleContains = 7;
        private const int ManufacturerPrefix = 8;
        private const int ManufacturerContains = 9;
        private const int TechPrefix = 10;
        private const int TechContains = 11;
        private const int SecondaryPrefix = 12;
        private const int SecondaryContains = 13;
        private const int TagPrefix = 14;
        private const int TagContains = 15;
        private const int AutoTagPrefix = 16;
        private const int AutoTagContains = 17;
        private const int DescriptionPrefix = 22;
        private const int DescriptionContains = 23;

        private const int TagWeightedTagPrefix = 0;
        private const int TagWeightedTagContains = 1;
        private const int TagWeightedAutoTagPrefix = 2;
        private const int TagWeightedAutoTagContains = 3;
        private const int TagWeightedTitlePrefix = 14;
        private const int TagWeightedTitleContains = 15;
        private const int TagWeightedNamePrefix = 16;
        private const int TagWeightedNameContains = 17;

        /// <summary>Enter-submit ceiling — excludes description-only loose hits.</summary>
        internal const int EnterSearchMaxAggregateScore = AutoTagContains;

        private readonly List<SearchableItemSnapshot> _items = new List<SearchableItemSnapshot>();

        public int ItemCount => _items.Count;

        public void Clear()
        {
            _items.Clear();
        }

        public void BuildFromSnapshot(SearchableItemSnapshotSet set)
        {
            _items.Clear();
            if (set == null || set.Items == null)
            {
                return;
            }

            for (int i = 0; i < set.Items.Count; i++)
            {
                SearchableItemSnapshot item = set.Items[i];
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                EnsureDefaultScores(item);
                _items.Add(item);
            }
        }

        public IEnumerable<TightMatchResult> Match(string query)
        {
            string trimmed = (query ?? string.Empty).Trim();
            if (PostV1QueryGuards.IsTooShortForSuggestions(trimmed))
            {
                yield break;
            }

            string[] words = TextMatchScoring.SplitQueryWords(trimmed);
            if (words.Length == 0)
            {
                yield break;
            }

            bool titleFirst = trimmed.Length <= PostV1QueryGuards.TitleFirstMaxQueryLength;

            for (int i = 0; i < _items.Count; i++)
            {
                SearchableItemSnapshot item = _items[i];
                ScoredItem scored = ScoreItem(item, words, titleFirst);
                if (!QualifiesForEnterSearch(scored))
                {
                    continue;
                }

                yield return new TightMatchResult
                {
                    Id = item.Id,
                    DisplayTitle = string.IsNullOrWhiteSpace(item.DisplayTitle) ? item.Id : item.DisplayTitle,
                    Score = scored.Score,
                    BestFieldKind = scored.BestKind
                };
            }
        }

        /// <summary>
        /// Builds a part-like snapshot with NativeEnterMatcher default score bands.
        /// Callers supply already-cleaned field texts.
        /// </summary>
        public static SearchableItemSnapshot CreatePartLikeItem(
            string id,
            string title,
            string name,
            SearchBarSurface surface,
            IEnumerable<string> tags = null,
            string category = null,
            string manufacturer = null,
            string tech = null,
            IEnumerable<string> modules = null,
            IEnumerable<string> autoTags = null,
            string description = null)
        {
            var item = new SearchableItemSnapshot
            {
                Id = id ?? string.Empty,
                DisplayTitle = title ?? name ?? id,
                Surface = surface
            };

            AddField(item, Clean(title), TightSearchFieldKind.Title, TitlePrefix, TitleContains);
            AddField(item, Clean(name), TightSearchFieldKind.Name, NamePrefix, NameContains);
            AddField(item, Clean(category), TightSearchFieldKind.Category, CategoryPrefix, CategoryContains);
            AddField(item, Clean(manufacturer), TightSearchFieldKind.Manufacturer, ManufacturerPrefix, ManufacturerContains);
            AddField(item, Clean(tech), TightSearchFieldKind.TechRequired, TechPrefix, TechContains);
            AddField(item, Clean(description), TightSearchFieldKind.Description, DescriptionPrefix, DescriptionContains);

            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    AddField(item, Clean(tag), TightSearchFieldKind.Tag, TagPrefix, TagContains);
                }
            }

            if (modules != null)
            {
                foreach (string module in modules)
                {
                    AddField(item, Clean(module), TightSearchFieldKind.Module, ModulePrefix, ModuleContains);
                }
            }

            if (autoTags != null)
            {
                foreach (string auto in autoTags)
                {
                    AddField(item, Clean(auto), TightSearchFieldKind.AutoTag, AutoTagPrefix, AutoTagContains);
                }
            }

            return item;
        }

        /// <summary>
        /// Generic browser row: title + optional secondary + tags. Description excluded from qualify.
        /// </summary>
        public static SearchableItemSnapshot CreateBrowserRow(
            string id,
            string title,
            SearchBarSurface surface,
            string secondary = null,
            string description = null,
            IEnumerable<string> tags = null)
        {
            var item = new SearchableItemSnapshot
            {
                Id = id ?? string.Empty,
                DisplayTitle = title ?? id,
                Surface = surface
            };

            AddField(item, Clean(title), TightSearchFieldKind.Title, TitlePrefix, TitleContains);
            AddField(item, Clean(id), TightSearchFieldKind.Name, NamePrefix, NameContains);
            AddField(item, Clean(secondary), TightSearchFieldKind.Secondary, SecondaryPrefix, SecondaryContains);
            AddField(item, Clean(description), TightSearchFieldKind.Description, DescriptionPrefix, DescriptionContains);

            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    AddField(item, Clean(tag), TightSearchFieldKind.Tag, TagPrefix, TagContains);
                }
            }

            return item;
        }

        private static void EnsureDefaultScores(SearchableItemSnapshot item)
        {
            if (item.Fields == null)
            {
                return;
            }

            for (int i = 0; i < item.Fields.Count; i++)
            {
                SearchableFieldSnapshot field = item.Fields[i];
                if (field == null)
                {
                    continue;
                }

                if (field.PrefixScore != 0 || field.ContainsScore != 0)
                {
                    continue;
                }

                AssignDefaultScores(field);
            }
        }

        private static void AssignDefaultScores(SearchableFieldSnapshot field)
        {
            switch (field.Kind)
            {
                case TightSearchFieldKind.Title:
                    field.PrefixScore = TitlePrefix;
                    field.ContainsScore = TitleContains;
                    break;
                case TightSearchFieldKind.Name:
                    field.PrefixScore = NamePrefix;
                    field.ContainsScore = NameContains;
                    break;
                case TightSearchFieldKind.Category:
                    field.PrefixScore = CategoryPrefix;
                    field.ContainsScore = CategoryContains;
                    break;
                case TightSearchFieldKind.Module:
                    field.PrefixScore = ModulePrefix;
                    field.ContainsScore = ModuleContains;
                    break;
                case TightSearchFieldKind.Manufacturer:
                    field.PrefixScore = ManufacturerPrefix;
                    field.ContainsScore = ManufacturerContains;
                    break;
                case TightSearchFieldKind.TechRequired:
                    field.PrefixScore = TechPrefix;
                    field.ContainsScore = TechContains;
                    break;
                case TightSearchFieldKind.Secondary:
                    field.PrefixScore = SecondaryPrefix;
                    field.ContainsScore = SecondaryContains;
                    break;
                case TightSearchFieldKind.Tag:
                    field.PrefixScore = TagPrefix;
                    field.ContainsScore = TagContains;
                    break;
                case TightSearchFieldKind.AutoTag:
                    field.PrefixScore = AutoTagPrefix;
                    field.ContainsScore = AutoTagContains;
                    break;
                case TightSearchFieldKind.Description:
                    field.PrefixScore = DescriptionPrefix;
                    field.ContainsScore = DescriptionContains;
                    break;
            }
        }

        private static bool QualifiesForEnterSearch(ScoredItem scored)
        {
            if (scored.Score < 0 || scored.Score > EnterSearchMaxAggregateScore)
            {
                return false;
            }

            if (scored.BestKind == TightSearchFieldKind.Description)
            {
                return false;
            }

            return true;
        }

        private static ScoredItem ScoreItem(SearchableItemSnapshot item, string[] words, bool titleFirst)
        {
            int aggregateScore = -1;
            TightSearchFieldKind bestKind = TightSearchFieldKind.Description;
            SearchableFieldSnapshot bestField = null;

            for (int w = 0; w < words.Length; w++)
            {
                string word = words[w];
                int wordBestScore = -1;
                SearchableFieldSnapshot wordBestField = null;

                for (int i = 0; i < item.Fields.Count; i++)
                {
                    SearchableFieldSnapshot field = item.Fields[i];
                    int score = ScoreField(field, word, titleFirst);
                    if (score < 0)
                    {
                        continue;
                    }

                    if (wordBestScore < 0
                        || score < wordBestScore
                        || (score == wordBestScore
                            && KindPriority(field.Kind, titleFirst) < KindPriority(wordBestField.Kind, titleFirst)))
                    {
                        wordBestScore = score;
                        wordBestField = field;
                    }
                }

                if (wordBestScore < 0)
                {
                    return new ScoredItem { Score = -1, BestKind = TightSearchFieldKind.Description };
                }

                if (aggregateScore < 0
                    || wordBestScore > aggregateScore
                    || (wordBestScore == aggregateScore
                        && KindPriority(wordBestField.Kind, titleFirst) < KindPriority(bestKind, titleFirst)))
                {
                    aggregateScore = wordBestScore;
                    bestField = wordBestField;
                    bestKind = wordBestField.Kind;
                }
            }

            return new ScoredItem
            {
                Score = aggregateScore,
                BestKind = bestField != null ? bestField.Kind : TightSearchFieldKind.Description
            };
        }

        private static int ScoreField(SearchableFieldSnapshot field, string word, bool titleFirst)
        {
            if (field == null || string.IsNullOrEmpty(field.Text) || field.Text.Length < 2 || string.IsNullOrEmpty(word))
            {
                return -1;
            }

            ResolveFieldScores(field, titleFirst, out int prefixScore, out int containsScore);

            if (field.Text.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            {
                return prefixScore;
            }

            if (field.Text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return containsScore;
            }

            return -1;
        }

        private static void ResolveFieldScores(
            SearchableFieldSnapshot field,
            bool titleFirst,
            out int prefixScore,
            out int containsScore)
        {
            if (titleFirst)
            {
                prefixScore = field.PrefixScore;
                containsScore = field.ContainsScore;
                return;
            }

            switch (field.Kind)
            {
                case TightSearchFieldKind.Tag:
                    prefixScore = TagWeightedTagPrefix;
                    containsScore = TagWeightedTagContains;
                    return;
                case TightSearchFieldKind.AutoTag:
                    prefixScore = TagWeightedAutoTagPrefix;
                    containsScore = TagWeightedAutoTagContains;
                    return;
                case TightSearchFieldKind.Title:
                    prefixScore = TagWeightedTitlePrefix;
                    containsScore = TagWeightedTitleContains;
                    return;
                case TightSearchFieldKind.Name:
                    prefixScore = TagWeightedNamePrefix;
                    containsScore = TagWeightedNameContains;
                    return;
                default:
                    prefixScore = field.PrefixScore;
                    containsScore = field.ContainsScore;
                    return;
            }
        }

        private static int KindPriority(TightSearchFieldKind kind, bool titleFirst)
        {
            if (titleFirst)
            {
                switch (kind)
                {
                    case TightSearchFieldKind.Title:
                    case TightSearchFieldKind.Name:
                        return 0;
                    case TightSearchFieldKind.Category:
                    case TightSearchFieldKind.Module:
                    case TightSearchFieldKind.Manufacturer:
                    case TightSearchFieldKind.TechRequired:
                    case TightSearchFieldKind.Secondary:
                        return 1;
                    case TightSearchFieldKind.Tag:
                    case TightSearchFieldKind.AutoTag:
                        return 3;
                    case TightSearchFieldKind.Description:
                        return 4;
                    default:
                        return 5;
                }
            }

            switch (kind)
            {
                case TightSearchFieldKind.Tag:
                case TightSearchFieldKind.AutoTag:
                case TightSearchFieldKind.Category:
                case TightSearchFieldKind.Module:
                case TightSearchFieldKind.Manufacturer:
                case TightSearchFieldKind.TechRequired:
                case TightSearchFieldKind.Secondary:
                    return 0;
                case TightSearchFieldKind.Title:
                case TightSearchFieldKind.Name:
                    return 2;
                case TightSearchFieldKind.Description:
                    return 3;
                default:
                    return 4;
            }
        }

        private static void AddField(
            SearchableItemSnapshot item,
            string value,
            TightSearchFieldKind kind,
            int prefixScore,
            int containsScore)
        {
            string cleaned = Clean(value);
            if (cleaned.Length < 2)
            {
                return;
            }

            item.Fields.Add(new SearchableFieldSnapshot
            {
                Text = cleaned,
                Kind = kind,
                PrefixScore = prefixScore,
                ContainsScore = containsScore
            });
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().Trim('"');
        }

        /// <summary>Utility for adapters that mirror NativeEnterMatcher camel-case split.</summary>
        internal static string SplitCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length + 4);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private struct ScoredItem
        {
            public int Score;
            public TightSearchFieldKind BestKind;
        }
    }
}
