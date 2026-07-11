# Search History per-item delete — wire-up checklist

**Do not execute until ~0.9.x is intentionally scheduled.** This is **not** a v1.0 blocker.

Plan: [`../../docs/V0_9_HISTORY_ITEM_DELETE.md`](../../docs/V0_9_HISTORY_ITEM_DELETE.md)

## Preconditions

1. Keep `SearchHistoryFeatureGate.*` **false** until ModTest smoke passes.
2. Keep `<Compile Remove="PostV1\**\*.cs" />` until intentionally enabling include **or** promote store into the shipping tree and leave only chrome under PostV1 (product choice).
3. Deploy experiments to **ModTest only** — never Main.
4. Do **not** change Native Search standalone (no history there).
5. Preserve users’ `PluginData/History.cfg` — migrate in place; never wipe on upgrade.

## Wire-up order

### 1. Persistence adapter

| File | Change |
|------|--------|
| New `FileHistoryPersistence : IHistoryPersistence` (or adapt shipping `SearchHistory`) | Read/write `GameData/KoobalSearchEngine/PluginData/History.cfg` via `HistoryCfgCodec`; keep legacy path migrate from Koogle / PartSearchSuggest. |
| Shipping `SearchHistory.cs` | Either wrap behind `ISearchHistoryStore` or replace body with store + file persistence. Prefer one implementation. |

On first load of bare-line cfg: codec mints ids → Save rewrites `id\tquery`.

### 2. Suggestion DTO / dropdown payload

| File | Change |
|------|--------|
| `PartSuggestion` (or history-only parallel field) | Carry `HistoryEntryId` (string) when `Kind == History` / `IsHistory`. |
| `EditorSearchHook.ShowSuggestions` | Build history rows from `Store.Match` / `Snapshot` including ids. |

### 3. Row chrome (cosmetic bulk)

| File | Change |
|------|--------|
| `SearchDropdownPanel.CreateRow` (history path) | Add per-row ✕ / trash button; tooltip “Remove from history”. |
| `SearchDropdownPanel` | New event `OnRemoveHistoryItemRequested` (`Action<string entryId>`). |
| Click isolation | Delete button must not invoke row apply / fill-search. |
| Visibility | Hover and/or selected-row; reuse header trash sprite helper if useful. |
| Live `IHistoryRowChrome` (optional) | Thin adapter over panel, or wire events directly without the port. |

Keep header **clear-all** trash + `OnClearHistoryRequested` unchanged (`ClearAll`).

### 4. Hook wiring

| File | Change |
|------|--------|
| `EditorSearchHook` ctor / destroy | Subscribe/unsubscribe remove event. |
| New `RemoveSearchHistoryItem(string entryId)` | `Store.Remove` → clear `_lastCommittedQuery` if needed → `ShowSuggestions(..., preferHistory: true, source: "remove-history-item")`. |
| Existing `ClearSearchHistory` | Call `Store.ClearAll` (same refresh as today). |
| Branding empty | When count → 0 and query empty, existing `showBrandingFooter` path must still run. |

### 5. Feature gate

| Step | Change |
|------|--------|
| Compile include | Lift or narrow PostV1 exclude **or** move store into shipping compile. |
| `SearchHistoryFeatureGate.EnablePerRowDeleteChrome` | Flip only after ModTest: hover ✕ works, clear-all works, last-delete → branding, cfg has ids. |
| Package | Label ~0.9.x — do not hold v1.0 for this QoL. |

## Verification before Main

- [ ] Architecture self-check: Remember / Remove / RemoveAt / ClearAll / codec legacy migrate.
- [ ] ModTest: delete middle row; remaining order correct; cfg updated.
- [ ] ModTest: delete last row → branding empty (no stuck “Recent searches” header).
- [ ] ModTest: clear-all still works; Remember after delete does not resurrect removed id.
- [ ] ModTest: clicking ✕ does not apply the history query.
- [ ] Upgrade from bare-line History.cfg: ids appear on next save; queries preserved.
- [ ] Feature gate off path: zero behaviour change.

## Explicit non-touch

- Do not add history UI to Native Search.
- Do not deploy architecture-only DLL to GameData as an enable.
- Do not require confirmation dialogs for v0.9 (match clear-all).
- Do not block v1.0 release on this checklist.
