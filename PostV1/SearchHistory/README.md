# Search History — per-item delete (target ~0.9)

**Status:** Near-implementation architecture — **excluded from the shipping `KoobalSearchEngine.dll` build.**  
**Target:** **~0.9.x** (pre-full-v1 QoL — **not** a v1.0 blocker).

Plan: [`../../docs/V0_9_HISTORY_ITEM_DELETE.md`](../../docs/V0_9_HISTORY_ITEM_DELETE.md)

Delete individual recent-search rows in the editor dropdown while keeping header clear-all and the branding empty state. Native Search standalone has no history — out of scope.

## Exclusion from shipping compile

Parent folder is covered by `PartSearchSuggest.csproj`:

```xml
<Compile Remove="PostV1\**\*.cs" />
```

Do not remove that exclude for SearchHistory alone. Do not wire into `EditorSearchHook`, `SearchDropdownPanel`, ModTest, or Main `GameData` until ~0.9 work is intentionally scheduled.

## Compile-check (does not deploy)

```powershell
dotnet build "Source\PartSearchSuggest\PostV1\PostV1.Architecture.csproj" -c Release
```

Outputs to `PostV1/bin/` — never GameData.

## Layout

| File / type | Role |
|-------------|------|
| `SearchHistoryEntry` | Id + Query DTO (id stable across sessions once persisted) |
| `HistoryCfgCodec` | `id\tquery` parse/format + legacy bare-line migration |
| `IHistoryPersistence` + Memory / Unwired | Load/Save port |
| `ISearchHistoryStore` + `SearchHistoryStore` | Remember / Remove / RemoveAt / ClearAll / Snapshot / Match |
| `IHistoryRowChrome` + Unwired/Recording | Cosmetic per-row ✕ (bulk of later UX wire-up) |
| `SearchHistoryServices` | Composition root |
| `SearchHistoryFeatureGate` | **All false** |
| `WIRE_UP.md` | Shipping touch list (do not execute yet) |

## What’s done (nearly ready)

- Full store logic against injected persistence (dedupe, cap 12, min length 2, persist on mutate).
- Cfg codec with legacy bare-query → mint id.
- Recording chrome → `Remove(entryId)` for self-check.
- Clear-all maps to `ClearAll` (existing trashcan behaviour).

## What’s left for wire-up

See [`WIRE_UP.md`](WIRE_UP.md). Summary:

1. File `IHistoryPersistence` over `PluginData/History.cfg` (+ existing path migrate).
2. Per-row delete control on history rows in `SearchDropdownPanel` (cosmetic bulk).
3. `EditorSearchHook` subscribe → Remove → refresh (mirror clear-all).
4. Carry entry id on history `PartSuggestion` (or parallel payload).
5. Feature gates still off until ModTest smoke; then ~0.9 package.

## Rules

1. **Keep clear-all** header trash — per-row does not replace it.
2. **Last delete → branding empty** — same path as clear-all with empty history.
3. **Click isolation** — ✕ must not apply the history suggestion.
4. **Preserve id on Remember dedupe** — move-to-top keeps the same id.
5. **No Native Search history**.
6. Data remove is trivial; **UI chrome is the remaining bulk**.

## Namespace

`PartSearchSuggest.PostV1.SearchHistory` — tagged `[PostV1Phase(PostV1Phase.HistoryItemDelete_v09)]`.  
Lives under `PostV1/` for compile-exclude convenience only; chronologically may ship **before** Phase A–D.
