using System.Collections.Generic;
using System.Linq;
using PartSearchSuggest.PostV1.Apply;
using PartSearchSuggest.PostV1.Categories;
using PartSearchSuggest.PostV1.GlobalSearch;
using PartSearchSuggest.PostV1.SearchHistory;
using PartSearchSuggest.PostV1.Subassemblies;
using PartSearchSuggest.PostV1.V2;
using PartSearchSuggest.PostV1.V2.ModSettings;
using PartSearchSuggest.PostV1.V2.PartsListExperience;

namespace PartSearchSuggest.PostV1
{
    /// <summary>
    /// Lightweight in-process checks for the architecture assembly (no NUnit dependency).
    /// Call from a future test host or ignore — exists to keep critical paths from rotting.
    /// </summary>
    [PostV1Phase(PostV1Phase.A_SuggestOnlyCategories)]
    internal static class ArchitectureSelfCheck
    {
        /// <summary>Returns null on success; otherwise a failure message.</summary>
        public static string Run()
        {
            var services = new PostV1Services(useRecordingUiDoubles: true);

            var set = new CategoryTabSnapshotSet
            {
                Tabs =
                {
                    new CategoryTabSnapshot
                    {
                        Kind = CategoryTabKind.StockCategoryTab,
                        FilterKey = "engines",
                        DisplayName = "Engines",
                        ItemCount = 40,
                        SourceTag = "SelfCheck"
                    },
                    new CategoryTabSnapshot
                    {
                        Kind = CategoryTabKind.StockSubcategoryTab,
                        FilterKey = "engines›liquid",
                        ParentDisplayName = "Engines",
                        DisplayName = "Liquid",
                        IsSubcategory = true,
                        ItemCount = 12,
                        SourceTag = "SelfCheck"
                    }
                }
            };

            services.CategoryIndex.BuildFromSnapshot(set);
            List<CategoryTabSuggestion> catHits = services.CategoryIndex.Match("eng", 10).ToList();
            if (catHits.Count == 0)
            {
                return "Category Match('eng') returned no rows.";
            }

            var nav = services.CategoryNavigator.Apply(new CategoryTabApplyRequest
            {
                FilterKey = "engines",
                Kind = CategoryTabKind.StockCategoryTab,
                DisplayText = "Engines"
            });
            if (!nav.Succeeded)
            {
                return "CategoryTabNavigator failed: " + nav.FailureReason;
            }

            services.SubassemblyIndex.BuildFromSnapshot(new SubassemblyCraftSnapshotSet
            {
                Crafts =
                {
                    new SubassemblyCraftSnapshot
                    {
                        CraftPath = @"C:\fake\Boosters.craft",
                        Title = "Boosters",
                        Description = "Asparagus boosters",
                        Author = "Jeb",
                        FacilityFolder = "VAB",
                        IsValidated = true
                    }
                }
            });

            List<SubassemblyCraftSuggestion> saHits = services.SubassemblyIndex.Match("boost", 5).ToList();
            if (saHits.Count == 0)
            {
                return "Subassembly Match('boost') returned no rows.";
            }

            services.InstallLifecycleHooks();
            ((SubassemblyLifecycleEventBus)services.SubassemblyEvents).RaiseDeleted(@"C:\fake\Boosters.craft");
            if (services.SubassemblyIndex.EntryCount != 0)
            {
                return "Delete refresh did not remove craft.";
            }

            services.UninstallLifecycleHooks();

            string globalFailure = RunGlobalSearchChecks();
            if (globalFailure != null)
            {
                return globalFailure;
            }

            string historyFailure = RunSearchHistoryChecks();
            if (historyFailure != null)
            {
                return historyFailure;
            }

            string v2Failure = RunV2Checks();
            if (v2Failure != null)
            {
                return v2Failure;
            }

            return null;
        }

        /// <summary>Global Search Halt (Phase D) — matcher, halt, apply, coexistence skip.</summary>
        public static string RunGlobalSearchChecks()
        {
            var global = new GlobalSearchServices(useRecordingPorts: true, fullKoobalPresent: true);

            if (!global.Orchestrator.ShouldSkipSurface(SearchBarSurface.EditorPartList))
            {
                return "GlobalSearch should skip EditorPartList when full Koobal is present.";
            }

            GlobalSearchApplyResult skipped = global.Orchestrator.OnEnter(
                SearchBarSurface.EditorPartList,
                "engine");
            if (skipped.Status != GlobalSearchApplyStatus.SurfaceSkipped)
            {
                return "Editor Enter should be SurfaceSkipped when full Koobal present.";
            }

            // Non-editor surface with full matcher path.
            var rdGlobal = new GlobalSearchServices(useRecordingPorts: true, fullKoobalPresent: true);
            var set = new SearchableItemSnapshotSet { Surface = SearchBarSurface.ResearchAndDevelopment };
            set.Items.Add(TightSearchMatcher.CreateBrowserRow(
                "basicRocketry",
                "Basic Rocketry",
                SearchBarSurface.ResearchAndDevelopment,
                secondary: "start",
                description: "Unlocks early engines and fuel tanks for testing."));
            set.Items.Add(TightSearchMatcher.CreatePartLikeItem(
                "liquidEngine",
                "LV-T30 Liquid Fuel Engine",
                "liquidEngine",
                SearchBarSurface.ResearchAndDevelopment,
                tags: new[] { "engine", "liquid" },
                description: "A powerful engine that should not match description-only queries."));
            rdGlobal.LoadSnapshot(set);

            rdGlobal.Orchestrator.OnTyping(SearchBarSurface.ResearchAndDevelopment, "value-changed");
            var halt = (RecordingSearchExecutionHalt)rdGlobal.Halt;
            if (!halt.IsTypingHaltActive(SearchBarSurface.ResearchAndDevelopment))
            {
                return "OnTyping did not activate halt for R&D.";
            }

            GlobalSearchApplyResult applied = rdGlobal.Orchestrator.OnEnter(
                SearchBarSurface.ResearchAndDevelopment,
                "rocketry");
            if (applied.Status != GlobalSearchApplyStatus.Applied || applied.MatchCount < 1)
            {
                return "R&D Enter 'rocketry' should apply tight matches.";
            }

            // Description-only must not qualify.
            var descOnly = new GlobalSearchServices(useRecordingPorts: true, fullKoobalPresent: false);
            var descSet = new SearchableItemSnapshotSet { Surface = SearchBarSurface.EditorPartList };
            descSet.Items.Add(TightSearchMatcher.CreatePartLikeItem(
                "mystery",
                "Zzz Part",
                "mysteryPart",
                SearchBarSurface.EditorPartList,
                description: "uniquewordinthedescriptiononly"));
            descOnly.LoadSnapshot(descSet);
            GlobalSearchApplyResult noDesc = descOnly.Orchestrator.OnEnter(
                SearchBarSurface.EditorPartList,
                "uniquewordinthedescriptiononly");
            if (noDesc.Status != GlobalSearchApplyStatus.NoMatches)
            {
                return "Description-only Enter query must not match.";
            }

            List<SearchableItemSnapshot> filtered = SnapshotListFilter.Filter(
                set.Items,
                SnapshotListFilter.ToIdSet(rdGlobal.Matcher.Match("rocketry").ToList()));
            if (filtered.Count < 1)
            {
                return "SnapshotListFilter failed to keep matched ids.";
            }

            if (SearchSurfaceRegistry.Get(SearchBarSurface.EditorPartList)?.Confidence
                != SearchSurfaceConfidence.Proven)
            {
                return "EditorPartList registry confidence must be Proven.";
            }

            return null;
        }

        /// <summary>Target ~0.9 — history store remove/clear + cfg codec + chrome bind.</summary>
        public static string RunSearchHistoryChecks()
        {
            int idSeq = 0;
            System.Func<string> ids = () => "id" + (++idSeq);

            var memory = new MemoryHistoryPersistence();
            memory.Seed(
                new[]
                {
                    new SearchHistoryEntry("keep-a", "alpha"),
                    new SearchHistoryEntry("drop-b", "bravo"),
                    new SearchHistoryEntry("keep-c", "charlie")
                });

            var store = new SearchHistoryStore(memory, idFactory: ids);
            if (store.Count != 3)
            {
                return "SearchHistoryStore should load 3 seeded entries.";
            }

            if (!store.Remove("drop-b") || store.Count != 2)
            {
                return "Remove(entryId) should drop bravo.";
            }

            if (store.Snapshot[0].Id != "keep-a" || store.Snapshot[1].Id != "keep-c")
            {
                return "Remove should preserve order of remaining entries.";
            }

            string alphaIdBefore = store.Snapshot[0].Id;
            if (!store.Remember("ALPHA"))
            {
                return "Remember casing change should move/update alpha.";
            }

            if (store.Snapshot[0].Id != alphaIdBefore)
            {
                return "Remember dedupe must preserve stable id.";
            }

            if (store.Snapshot[0].Query != "ALPHA")
            {
                return "Remember should refresh query casing to newest form.";
            }

            if (!store.RemoveAt(1) || store.Count != 1)
            {
                return "RemoveAt should drop the second entry.";
            }

            bool migrated;
            List<SearchHistoryEntry> parsed = HistoryCfgCodec.Parse(
                new[] { "legacy engine", "idfixed\tsolid fuel", "", "  " },
                out migrated,
                idFactory: ids);
            if (!migrated || parsed.Count != 2)
            {
                return "HistoryCfgCodec should migrate bare lines and keep id\\tquery.";
            }

            if (parsed[1].Id != "idfixed" || parsed[1].Query != "solid fuel")
            {
                return "HistoryCfgCodec should parse id\\tquery lines.";
            }

            string[] formatted = HistoryCfgCodec.Format(parsed);
            if (formatted.Length != 2 || formatted[1] != "idfixed\tsolid fuel")
            {
                return "HistoryCfgCodec.Format should emit id\\tquery.";
            }

            var history = new SearchHistoryServices(
                useRecordingPorts: true,
                seedEntries: new[]
                {
                    new SearchHistoryEntry("row-1", "engine"),
                    new SearchHistoryEntry("row-2", "wing")
                });

            history.BindChromeFromSnapshot();
            var chrome = (RecordingHistoryRowChrome)history.RowChrome;
            if (chrome.LastBoundRows.Count != 2)
            {
                return "BindChromeFromSnapshot should bind 2 rows.";
            }

            chrome.SimulateRemoveClick("row-1");
            if (history.Store.Count != 1 || history.Store.Snapshot[0].Id != "row-2")
            {
                return "Chrome remove click should call Store.Remove.";
            }

            history.Store.ClearAll();
            if (history.Store.Count != 0)
            {
                return "ClearAll should empty the store.";
            }

            var historyMemory = (MemoryHistoryPersistence)history.Persistence;
            if (historyMemory.SaveCount < 1)
            {
                return "Store mutations should persist via IHistoryPersistence.Save.";
            }

            return null;
        }

        /// <summary>V2 — settings store, slide-expand compose, Track R go/no-go, Recording hosts.</summary>
        public static string RunV2Checks()
        {
            var v2 = new V2Services(useRecordingPorts: true);

            if (SettingsTabInformationArchitecture.Sections.Count < 4)
            {
                return "V2 Settings IA should expose Parts List, Search, History, Advanced.";
            }

            KoobalSettingsModel defaults = v2.SettingsStore.Current;
            if (defaults.ArchitectureTrack != PartsListArchitectureTrack.SlideExpand)
            {
                return "V2 defaults must prefer Track S (SlideExpand).";
            }

            if (!v2.SettingsStore.ApplyPatch(new KoobalSettingsPatch
            {
                SlideExpandEnabled = true,
                ExpandWidthAmount = 120f,
                ExpandHeightAmount = 40f,
                IconSize = PartsListIconSize.Compact
            }))
            {
                return "V2 ApplyPatch should change settings.";
            }

            string[] formatted = SettingsCfgCodec.Format(v2.SettingsStore.Current);
            KoobalSettingsModel roundTrip = SettingsCfgCodec.Parse(formatted);
            if (!roundTrip.SlideExpandEnabled
                || roundTrip.ExpandWidthAmount < 119f
                || roundTrip.IconSize != PartsListIconSize.Compact)
            {
                return "V2 SettingsCfgCodec round-trip failed.";
            }

            var dropdown = new DropdownSlideContribution
            {
                IsDropdownOpen = true,
                OffsetX = -80f,
                WidthDelta = -40f
            };

            PartsListPanelGeometryIntent intent = v2.ApplySlideExpandFromStore(dropdown);
            if (!intent.IncludesUserExpand || !intent.IncludesDropdownContribution)
            {
                return "V2 compose must include user expand + dropdown contribution together.";
            }

            if (intent.WidthDelta < 80f)
            {
                return "V2 compose should add user width and dropdown width delta.";
            }

            var slide = (RecordingPartsListSlideExpandController)v2.SlideExpand;
            if (slide.ApplyCount < 1)
            {
                return "V2 slide-expand controller should ApplyGeometry.";
            }

            // Track R remains no-go without evidence.
            PartsListTrackDecision noGo = PartsListTrackGoNoGo.Evaluate(new PartsListTrackEvidence
            {
                ResearchSpikeCompleted = false,
                SlideExpandMissedLagTarget = true
            });
            if (noGo.GoRebuild)
            {
                return "V2 go/no-go must not recommend Rebuild without spike + enough criteria.";
            }

            PartsListTrackDecision go = PartsListTrackGoNoGo.Evaluate(new PartsListTrackEvidence
            {
                ResearchSpikeCompleted = true,
                SlideExpandMissedLagTarget = true,
                SoftReflowHitPrefabCeiling = true,
                DropdownComposeTooFragile = true,
                StockBindHasNoRecycleHook = true,
                ProductAcceptsRebuildRisk = true
            });
            if (!go.GoRebuild || go.RecommendedTrack != PartsListArchitectureTrack.Rebuild)
            {
                return "V2 go/no-go should recommend Rebuild when criteria + spike met.";
            }

            // Effective track: Rebuild preference ignored unless AllowRebuildExperimental.
            v2.SettingsStore.ApplyPatch(new KoobalSettingsPatch
            {
                ArchitectureTrack = PartsListArchitectureTrack.Rebuild,
                AllowRebuildExperimental = false
            });
            if (PartsListLayoutPreferences.ResolveEffectiveTrack(v2.SettingsStore.Current)
                != PartsListArchitectureTrack.SlideExpand)
            {
                return "V2 EffectiveTrack must stay SlideExpand until AllowRebuildExperimental.";
            }

            v2.SettingsStore.ApplyPatch(new KoobalSettingsPatch
            {
                AllowRebuildExperimental = true
            });
            if (PartsListLayoutPreferences.ResolveEffectiveTrack(v2.SettingsStore.Current)
                != PartsListArchitectureTrack.Rebuild)
            {
                return "V2 EffectiveTrack should be Rebuild when allowed.";
            }

            var tab = (RecordingSettingsTabHost)v2.SettingsTabHost;
            tab.RegisterTab("Koobal Search Engine");
            tab.BindFromStore(v2.SettingsStore);
            if (!tab.IsTabRegistered || tab.BindCount != 1)
            {
                return "V2 RecordingSettingsTabHost should register and bind.";
            }

            var org = (RecordingPartsListOrganizerBridge)v2.OrganizerBridge;
            org.Seed(new PartsListOrganizerDetection
            {
                PrimaryKind = PartsListOrganizerKind.CommunityCategoryKit,
                DetectedKinds = new[] { PartsListOrganizerKind.CommunityCategoryKit },
                Detail = "CCK"
            });
            if (!org.PreferLayoutOnly(org.Detect(), compatibilityModeOn: true))
            {
                return "V2 organizer bridge should prefer layout-only for CCK when compatibility on.";
            }

            // V2FeatureGate.* are const false by design — do not assert via if (const) (CS0162).
            return null;
        }
    }
}
