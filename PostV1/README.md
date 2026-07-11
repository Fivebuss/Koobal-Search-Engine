# PostV1 architecture scaffolding

**Mission:** Organize the user's parts list and make it quickly and universally accessible and useful. (See [`../docs/MISSION.md`](../docs/MISSION.md).)

**Status:** Near-implementation architecture — **excluded from the shipping `KoobalSearchEngine.dll` build.**

This folder holds:

- Post-v1 category / subcategory / subassembly search (see `docs/POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md`)
- **Later** global Enter-halt across KSP search bars (see `docs/POST_V1_GLOBAL_SEARCH_HALT.md`)
- **Target ~0.9** per-item search history delete (see `docs/V0_9_HISTORY_ITEM_DELETE.md`) — pre-full-v1 QoL; **not** a v1.0 blocker; under `PostV1/` for compile-exclude only
- **V2 later** slide-expanded parts list + Settings tab (optional Track R rebuild) — see `docs/V2_PARTS_LIST_AND_SETTINGS.md` and [`V2/`](V2/)

It is **not** wired into `EditorSearchHook`, `SuggestionIndex`, `GameLoadIndexService`, Harmony patches, ModTest, or Main `GameData`.

## Exclusion from shipping compile

`PartSearchSuggest.csproj` contains:

```xml
<Compile Remove="PostV1\**\*.cs" />
```

Release / Debug builds of the live mod therefore ignore every `.cs` file under `PostV1/`. Do not remove that exclude until after the relevant band is intentionally enabled (categories after v1; history delete ~0.9 when scheduled).

## Compile-check (does not deploy)

Optional project: `PostV1/PostV1.Architecture.csproj`

```powershell
dotnet build "Source\PartSearchSuggest\PostV1\PostV1.Architecture.csproj" -c Release
```

- Outputs to `PostV1/bin/` (not GameData).
- Compiles **only** PostV1 sources — no KSP/Unity references required for the snapshot/recording path.
- Does **not** rebuild or copy `KoobalSearchEngine.dll`.

## What’s done (nearly ready)

### Phase A — Category suggestion index

| Piece | Status |
|-------|--------|
| `CategoryTabSnapshot` / `CategoryTabSnapshotSet` | Done — inject plain data, no PartCategorizer |
| `CategoryTabSnapshotBuilder` | Done — flat entries, nested subs, dedup by FilterKey |
| `CategorySuggestionMatch` | Done — pure score/dedup/kind tags (`StockCategory` / `Subcategory` / `CustomCategory`) |
| `CategorySuggestionIndex` + `SnapshotCategoryTabMatcher` | Done — `BuildFromSnapshot`, empty-row policy, denylist |
| `CategoryTabSuggestion` | Done — DisplayText, MatchReason, RankScore, ApplyPayloadId, IsValid(matcher) |
| Live PartCategorizer adapter | **Left for wire-up** |

### Phase B — Navigator

| Piece | Status |
|-------|--------|
| `IEditorCategoryUi` + handles | Done — forbidden ops documented |
| `UnwiredEditorCategoryUi` / `RecordingEditorCategoryUi` | Done — NotImplemented vs recording double |
| `CategoryApplyFilterPlan` | Done — pure clear-matrix steps |
| `CategoryTabNavigator` | Done — find → visible → activate → verify + result codes |
| Live PartCategorizer / radio activation | **Left for wire-up** |

### Phase C — Subassemblies

| Piece | Status |
|-------|--------|
| Craft snapshot + name/description/author ranking | Done |
| `SubassemblySuggestionIndex` incremental API | Done |
| `SubassemblyApplyPort` workflow + clear reasons | Done |
| `SubassemblyDeleteRefreshService` + lifecycle event bus | Done — no Harmony class yet |
| Folder scan / ShipTemplate / stock select UI | **Left for wire-up** |

### Cross-cutting

| Piece | Status |
|-------|--------|
| `PostV1Services` composition root | Done — unused by shipping |
| `IApplySafetyRails` | Done — suppress / null-editor hooks |
| `PostV1FeatureGate` | Still **all false** (includes `EnableGlobalSearchHalt`, `EnableHistoryItemDelete`, `EnableV2PartsListAndSettings`) |
| `GlobalSearch/` | Phase D scaffold — halt/matcher/orchestrator; see [`GlobalSearch/README.md`](GlobalSearch/README.md) |
| `SearchHistory/` | Target ~0.9 — per-row history delete; see [`SearchHistory/README.md`](SearchHistory/README.md) |
| `V2/` | **V2 later** — slide-expand parts list + Settings (+ optional Track R); see [`V2/README.md`](V2/README.md) |
| `WIRE_UP.md` | Ordered shipping file checklist (categories/SA) |
| `GlobalSearch/WIRE_UP.md` | Ordered checklist for global halt (after categories/SA) |
| `SearchHistory/WIRE_UP.md` | Ordered checklist for per-item history delete (~0.9) |
| `V2/WIRE_UP.md` | Ordered checklist for V2 settings / Track S / optional Track R |

## What’s left for wire-up

See [`WIRE_UP.md`](WIRE_UP.md). Summary:

1. Live snapshot adapters (PartCategorizer, Subassemblies folders).
2. Merge Match results in `EditorSearchHook` + dropdown row factory.
3. `LiveEditorCategoryUi` / `LiveSubassemblyEditorUi` implementing ports.
4. Bridge safety rails to `StockSearchHelper` suppress / recover.
5. Phase C postfix-only delete observe → event bus.
6. Flip compile include + ModTest; keep feature gates off until smoke passes.

**Separately (~0.9):** per-item history delete chrome — [`SearchHistory/WIRE_UP.md`](SearchHistory/WIRE_UP.md). Not a v1.0 blocker.

## Layout vs plan phases

| Folder | Plan phase | Contents |
|--------|------------|----------|
| `Categories/` | **A** | Snapshots, builder, match/score, concrete index, DTOs |
| `Apply/` | **B** | Navigator, UI port, filter plan, result enums |
| `Subassemblies/` | **C** | Index, apply workflow, delete-refresh, lifecycle events |
| `Safety/` | B/C | Apply suppress / editor-null rails |
| `Shared/` | A–C | Query guards + text scoring (shipping-aligned, no KSP deps) |
| `GlobalSearch/` | **D** | Enter-halt + tight matcher across KSP search bars (no chrome) — after categories/SA |
| `SearchHistory/` | **~0.9** | Per-row history delete store + chrome port — may ship before A–D |
| `V2/` | **V2 later** | Slide-expand parts list (Track S) + Settings; optional rebuild (Track R) |

## Later: global search halt

After v1 and after categories/subassemblies work: extend the **native/stock search improvements** core (Enter-to-search halt + tightened matching) to **every** stock search bar — **no** branding, **no** dropdowns on those bars. Full Koobal ships this as part of its core (plus full UI elsewhere). **Native Search** is the standalone slice of that same core and gets parity when these fixes ship — not exclusive ownership of “global search.” Scaffold stays here until extracted — not wired into live products yet. Scaffold: [`GlobalSearch/`](GlobalSearch/). Plan: [`../docs/POST_V1_GLOBAL_SEARCH_HALT.md`](../docs/POST_V1_GLOBAL_SEARCH_HALT.md). Native note: [`../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md`](../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md). Mission: [`../docs/MISSION.md`](../docs/MISSION.md).

## Pre-v1 QoL: history item delete (~0.9)

Editor dropdown only: remove one recent-search row without clear-all. Keep header trash + branding empty state. Scaffold: [`SearchHistory/`](SearchHistory/). Plan: [`../docs/V0_9_HISTORY_ITEM_DELETE.md`](../docs/V0_9_HISTORY_ITEM_DELETE.md). Frame as **Target: ~0.9**, not a v1 tonight blocker.

## Later: V2 parts list + Settings

**Track S (preferred):** slide-grow the parts list like today’s dropdown shift — not maximize/chrome-swap — plus icon size / list style / organizer compatibility. **Track R (optional):** full rebuild only after go/no-go. Settings tab required either way. Scaffold: [`V2/`](V2/). Plan: [`../docs/V2_PARTS_LIST_AND_SETTINGS.md`](../docs/V2_PARTS_LIST_AND_SETTINGS.md).

## Rules when this folder is later included

1. **Index predicate === apply predicate** (same as core suggestion checklist).
2. Stock UI remains authoritative — navigate/observe; do not own category/delete state machines.
3. No permanent Harmony Prefix writers on stock `OnTrue*` / `displayType` paths.
4. Subassembly lifecycle: folder-scoped incremental index only; postfix refresh after confirmed delete.
5. Wire through ModTest first; keep a compile or PluginData gate for rollback.
6. History per-row delete: isolate ✕ clicks from row apply; last delete → branding empty.

## Namespace

`PartSearchSuggest.PostV1.*` — matches shipping `RootNamespace` (`PartSearchSuggest`).
