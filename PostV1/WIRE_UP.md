# PostV1 wire-up checklist

**Do not execute this checklist until after v1.0 ships and Phase A is intentionally enabled.**

This document lists the exact shipping files to touch, in order, when flipping PostV1 from architecture → ModTest. Feature gates stay off until ModTest validation.

## Preconditions

1. Keep `PostV1FeatureGate.*` **false** until ModTest smoke passes.
2. Remove or narrow `<Compile Remove="PostV1\**\*.cs" />` in `PartSearchSuggest.csproj` **only** when starting Phase A implementation (prefer compile include + gate still off).
3. Deploy to **ModTest** only — never Main until Phase B acceptance (stock category buttons still work after apply).
4. Do **not** call PostV1 from shipping paths while gates are off (dead code OK; no runtime enable path).

## Later band (do not start here)

Global Enter-halt across non-editor search bars: see [`GlobalSearch/WIRE_UP.md`](GlobalSearch/WIRE_UP.md) and [`../docs/POST_V1_GLOBAL_SEARCH_HALT.md`](../docs/POST_V1_GLOBAL_SEARCH_HALT.md). Run **after** categories/SA Phase A–C ModTest acceptance.

## Separate ~0.9 band (not a v1 blocker)

Per-item search history delete: see [`SearchHistory/WIRE_UP.md`](SearchHistory/WIRE_UP.md) and [`../docs/V0_9_HISTORY_ITEM_DELETE.md`](../docs/V0_9_HISTORY_ITEM_DELETE.md). May ship before Phase A–D; schedule independently.

## Later: V2 band (not a v1 blocker)

Slide-expand parts list + Settings tab (optional Track R rebuild after go/no-go): see [`V2/WIRE_UP.md`](V2/WIRE_UP.md) and [`../docs/V2_PARTS_LIST_AND_SETTINGS.md`](../docs/V2_PARTS_LIST_AND_SETTINGS.md).

## Wire-up order

### 1. Shared guards bridge

| File | Change |
|------|--------|
| `SuggestionQueryGuards.cs` | Optionally route PostV1 to shipping guards, or delete `PostV1/Shared/PostV1QueryGuards.cs` and reference shipping constants. |
| `SuggestionTokenQuality.cs` | Optional: share denylist tokens with `CategorySuggestionIndexConfig.DenylistKeys` if desired. |

### 2. Phase A — suggest-only (editor entry index)

| File | Change |
|------|--------|
| New adapter (e.g. `PostV1/Categories/LiveCategoryTabSnapshotAdapter.cs`) | Enumerate `PartCategorizer` tabs → `CategoryTabSnapshotSet`; cfg bootstrap merge. |
| `GameLoadIndexService.cs` / `GameLoadBootstrap.cs` | **Do not** build category tabs at save-load. Keep hangar-free. |
| `EditorSearchHook.cs` (editor-ready path) | When gate on: build `CategorySuggestionIndex.BuildFromSnapshot` once PartCategorizer ready (frame-sliced). |
| `EditorSearchHook.cs` (`CollectSuggestions` / merge) | Merge `CategoryIndex.Match` into organic list; `OrderBy RankScore`. |
| Dropdown row factory (`SearchDropdownPanel.cs` / row builder) | Map `CategoryTabSuggestion` → row (DisplayText, MatchReason, KindTag, icon via existing icon helpers). |
| `SuggestionKind` / `PartSuggestion` | Add kinds **or** keep parallel DTO + adapter until kinds merge (product choice). |
| `SuggestionDedupHelper.cs` | Dedup tab rows vs `FilterCategory` / `FilterFunction` (plan R9). |

Phase A click: no-op or fill search field only — **no** `CategoryTabNavigator.Apply` until Phase B.

### 3. Phase B — navigate/apply

| File | Change |
|------|--------|
| New `LiveEditorCategoryUi : IEditorCategoryUi` | Stock-equivalent button activation; **no** permanent `displayType` Harmony; **no** `OnTrue` Prefix writers; **no** SearchRoutine skip. |
| `EditorSearchHook.ApplySuggestion` | Branch for category kinds → `CategoryTabNavigator.Apply`; on failure `RecoverAfterFailedApply`. |
| `StockSearchHelper.cs` | Reuse clear-custom-filter + `RecoverAfterFailedApply`; bridge `IApplySafetyRails` to existing suppress flags. |
| Harmony | Prefer none. If observe-only postfix is required, fail-open and never rewrite `displayType` on stock click path. |

### 4. Phase C — subassemblies

| File | Change |
|------|--------|
| New folder scanner adapter | Editor-entry scan → `SubassemblyCraftSnapshotSet` / incremental `AddOrUpdate`. |
| `EditorSearchHook.cs` | Merge `SubassemblyIndex.Match`; apply → `SubassemblyApplyPort.Apply`. |
| New `LiveSubassemblyEditorUi : ISubassemblyEditorUi` | Tab switch + select/load; clear matrix on dismiss / tab / focus / 45s timeout / **before delete dialog**. |
| Delete observe | Harmony **postfix-only** (or event from stock confirm) → `ISubassemblyLifecycleEvents.RaiseDeleted` → `SubassemblyDeleteRefreshService`. **No** mid-dialog `RefreshSubassemblies`. |
| `GameLoadIndexService.cs` | Still must **not** rebuild part/metadata/categorizer on SA lifecycle. |

### 5. Verification before Main

- [ ] ModTest: VAB + SPH suggest category rows (Phase A).
- [ ] ModTest: click category === manual tab; stock Structural/RCS still filter after many applies (Phase B).
- [ ] ModTest: SA save appears, delete vanishes, delete dialog closable (Phase C).
- [ ] KSP.log clean of categorizer / SearchStart NRE.
- [ ] Feature gate off path: zero behaviour change vs core-only.

## Explicit non-touch until Phase C/B respectively

- Do not add `displayType` Prefix Harmony.
- Do not Prefix-skip `SearchRoutine`.
- Do not rebuild part indexes on SA delete/save.
- Do not deploy architecture-only DLL to Main/`GameData/KoobalSearchEngine` as an “enable”.
