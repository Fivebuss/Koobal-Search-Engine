# Plan — Per-item Search History Delete

**Status:** Architecture fleshed out (near wire-up) — **do not ship / do not enable until intentionally scheduled**  
**Target version:** **~0.9.x** (pre-full-v1 QoL — **not** a v1.0 blocker tonight)  
**Mod:** Full Koobal Search Engine (`GameData/KoobalSearchEngine/`) — editor dropdown only  
**Source:** `Source/PartSearchSuggest/PostV1/SearchHistory/`  
**Written:** 2026-07-11  
**Baseline today:**

| Behaviour | Status |
|-----------|--------|
| Remember on typed Enter / blur commit | Shipping (`EditorSearchHook.CommitSearchHistory`) |
| Remember on clicked suggestion | Shipping (`RememberClickedSuggestion`) |
| Clear-**all** trashcan on history header | Shipping (`OnClearHistoryRequested` → `SearchHistory.Clear`) |
| Per-**row** delete | **Missing** — this plan |

### Mission alignment

Canonical mission: [`MISSION.md`](MISSION.md). Per-row history delete keeps recent searches organized and useful—users prune noise without clearing everything. That preserves speed of return to intent while making the history list itself more useful.

Related: [`../PostV1/SearchHistory/README.md`](../PostV1/SearchHistory/README.md), [`../PostV1/SearchHistory/WIRE_UP.md`](../PostV1/SearchHistory/WIRE_UP.md), [`../PostV1/README.md`](../PostV1/README.md).  
Out of scope: **Koobal Native Search** standalone (no history). Categories / Global Search Halt remain separate bands.  
**V2 later** (parts list slide-expand + Settings): [`V2_PARTS_LIST_AND_SETTINGS.md`](V2_PARTS_LIST_AND_SETTINGS.md).

### Scaffold status (near-implementation; not wired)

| Piece | Location | Readiness |
|-------|----------|-----------|
| Entry model + stable id | `SearchHistoryEntry` | Done |
| Persistence port + cfg codec | `IHistoryPersistence`, `HistoryCfgCodec`, `MemoryHistoryPersistence` | Done — pure remove/save path |
| Store | `ISearchHistoryStore` + `SearchHistoryStore` | Done — Remember / Remove / RemoveAt / ClearAll / Snapshot / Match |
| Row chrome port | `IHistoryRowChrome` + Unwired/Recording | Done — cosmetic boundary only |
| Composition / gate | `SearchHistoryServices`, `SearchHistoryFeatureGate` | Done — gate **always false** |
| Wire-up checklist | `SearchHistory/WIRE_UP.md` | Done |

**Excluded from shipping compile** via `PartSearchSuggest.csproj` (`<Compile Remove="PostV1\**\*.cs" />`). Not referenced by `EditorSearchHook`, Harmony, ModTest, or Main `GameData`.

Optional compile-check: `PostV1/PostV1.Architecture.csproj` (outputs `PostV1/bin/`, never GameData).

---

## 1. Goals

| # | Goal | Feel |
|---|------|------|
| H1 | Delete **one** recent-search row without clearing the rest. | Hover/control → gone; list updates. |
| H2 | Persist removal to `PluginData/History.cfg` immediately (same durability as Remember / Clear). | Restart still missing that entry. |
| H3 | Keep header **clear-all** trashcan. | Bulk wipe still one click. |
| H4 | When the last history row is removed (or clear-all), fall through to the existing **branding empty** dropdown. | Same empty-field chrome as today. |
| H5 | Data-layer remove-by-id / remove-by-index is trivial; **UI chrome is the bulk of later wire-up**. | Architecture makes store work “done”; cosmetics land at ~0.9. |

**Success metric:** Per-row delete updates in-memory list + cfg; clear-all unchanged; branding empty state still shows when history is empty; feature gate off ⇒ zero behaviour change; no accidental apply of the history suggestion when clicking delete.

---

## 2. Non-goals

- **No** history in Native Search standalone.
- **No** cloud sync / multi-profile history.
- **No** undo stack (delete is immediate; user can re-type).
- **No** shipping compile include until ~0.9 wire-up is intentional.
- **No** GameData deploy / Main / ModTest wiring from this scaffold alone.
- **Not** categories, subassemblies, or global search halt.

---

## 3. UX

### Recommended (primary)

**Hover / focus affordance: small ✕ (or trash) on each history row**, right-aligned, tooltip “Remove from history”.

| Rule | Detail |
|------|--------|
| Visibility | Show on hover (desktop) and when the row is keyboard-highlighted; always-visible is acceptable if hover is flaky in Unity IMGUI/uGUI. |
| Hit target | Prefer ≥16×16 (reuse programmatic trash sprite pattern from header clear-all if helpful). |
| Click isolation | Delete control must **not** fire row apply / fill-search. Stop propagation / separate button. |
| After delete | Refresh open dropdown from store snapshot; if empty + query empty → branding-only (header/clear hidden). |
| Clear-all | Keep existing header trash; do not remove it when per-row lands. |

### Optional later (secondary)

| Option | Notes |
|--------|-------|
| Right-click → “Remove” | Extra discoverability; Unity context menus are awkward — defer unless hover X proves insufficient. |
| Shift+click / long-press | Skip for v0.9 unless playtest demands. |

### Coexistence with branding empty state

Today (`EditorSearchHook.ShowSuggestions`):

- Empty query + history entries → history mode + clear-all.
- Empty query + **no** history → branding footer, no suggestion rows, header often hidden (`brandingOnly`).

Per-item delete must call the same refresh path as clear-all (`ShowSuggestions(..., preferHistory: true, source: "remove-history-item")`) so deleting the last row lands on branding-only without a blank stuck header.

---

## 4. Persistence (`History.cfg`)

### Today (shipping)

- Path: `GameData/KoobalSearchEngine/PluginData/History.cfg`
- Format: one trimmed query string per line
- Cap: 12 entries; Remember dedupes case-insensitively and moves-to-top
- Clear writes empty file (or empty line set)

### Target shape (stable ids across sessions)

Stable ids are required so UI rows bind to `Remove(entryId)` without depending on list order after concurrent Remember / refresh.

**Line format (keep text-file simplicity):**

```text
<id>\t<query>
```

| Field | Rules |
|-------|--------|
| `id` | Non-empty, opaque, stable for the life of that remembered query. Prefer GUID `"N"` (32 hex) minted once. |
| `query` | Trimmed display/search text (same semantics as today). Tab characters in queries are rejected / normalized away on Remember. |
| Blank lines | Ignored on load. |
| Legacy bare line (no tab) | Treat entire line as `query`; **mint a new id on load**; rewrite with ids on next Save so migration is one-shot. |

**Remember dedupe:** if an existing entry matches query case-insensitively, **keep its id**, move row to index 0, Save. Do not mint a new id for the same logical query.

**Clear-all / Remove:** Save immediately after mutation (same as shipping `SearchHistory`).

Ids are **not** secrets; they exist for stable UI binding and safe remove under reordering.

---

## 5. Architecture (ports)

```
┌─────────────────────────────────────────────────────────┐
│ EditorSearchHook (wire-up later)                        │
│   OnRemoveHistoryItem(entryId) → store.Remove → refresh │
│   OnClearHistory → store.ClearAll → refresh             │
└───────────────┬───────────────────────────▲─────────────┘
                │                           │
                ▼                           │
┌───────────────────────────┐   ┌───────────┴──────────────┐
│ ISearchHistoryStore       │   │ IHistoryRowChrome        │
│  Snapshot / Match         │   │  Bind per-row ✕ control  │
│  Remember / Remove / …    │   │  (cosmetic; bulk of UX)  │
└───────────┬───────────────┘   └──────────────────────────┘
            │
            ▼
┌───────────────────────────┐
│ IHistoryPersistence       │
│  Load / Save              │
│  + HistoryCfgCodec        │
└───────────────────────────┘
```

| Port / type | Responsibility |
|-------------|----------------|
| `SearchHistoryEntry` | `Id` + `Query`; immutable DTO for snapshot rows. |
| `ISearchHistoryStore` | In-memory list + mutations; **pure** vs persistence injection. |
| `IHistoryPersistence` | Load/Save entry lists (file adapter at wire-up; memory for tests). |
| `HistoryCfgCodec` | Line parse/format + legacy bare-line migration. |
| `IHistoryRowChrome` | Cosmetic: attach/hide per-row delete control; raise remove requests by id. |
| `SearchHistoryFeatureGate` | **All false** until ~0.9 ModTest smoke. |
| `SearchHistoryServices` | Composition root (unused by shipping). |

Data-layer remove-by-id/index is intentionally small; **most remaining work is `SearchDropdownPanel` row chrome + event wiring**.

---

## 6. Risks

| Risk | Mitigation |
|------|------------|
| Delete click applies the history suggestion | Separate `Button` / raycast target; do not use the row’s primary click handler. |
| Id loss on legacy cfg | Codec mints ids on load; next Save rewrites tab format. |
| Tab / malformed lines | Reject tabs in Remember; skip empty; log once on corrupt line. |
| Race: delete while Remember | Store is single-threaded (Unity main); mutate then Save then refresh UI. |
| Empty list UX regression | Reuse clear-all refresh → branding-only path. |
| Accidental delete | Tooltip + small control; no confirmation dialog for v0.9 (clear-all also has none). |

---

## 7. Wire-up order (summary)

Full checklist: [`../PostV1/SearchHistory/WIRE_UP.md`](../PostV1/SearchHistory/WIRE_UP.md).

1. Bridge `SearchHistoryStore` + file `IHistoryPersistence` over existing path / migration helpers (or adapt shipping `SearchHistory` behind the interface).
2. `SearchDropdownPanel`: per-history-row delete control + `OnRemoveHistoryItemRequested(string entryId)`.
3. `EditorSearchHook`: subscribe → `Remove` → refresh suggestions (mirror `ClearSearchHistory`).
4. Map snapshot entries → `PartSuggestion` with stable id payload (extend DTO or parallel field).
5. Feature gate still off until ModTest; then flip for ~0.9 package.
6. Keep `<Compile Remove="PostV1\**\*.cs" />` until intentionally including — **or** promote store into shipping tree and leave only chrome ports under PostV1 (product choice at wire-up).

---

## 8. Versioning / scheduling

| Band | When | Notes |
|------|------|-------|
| **This feature** | **Target ~0.9.x** | Pre-full-v1 QoL. Docs + scaffold now; wire when cosmetics are scheduled. |
| Full v1.0 | Later | Do **not** block v1 on per-row history delete. |
| PostV1 categories / SA / global halt | After v1 (or as scheduled) | Separate plans; this folder only shares the PostV1 **compile-exclude** tree. |

Frame all work as **“Target: ~0.9”**, not “must land before tonight’s v1 cut.”

---

## 9. Explicit non-touch (scaffold phase)

- Do not wire into `EditorSearchHook` / `SearchDropdownPanel` yet.
- Do not rebuild or copy `KoobalSearchEngine.dll` for this scaffold.
- Do not deploy architecture DLL to GameData / Main / ModTest.
- Do not delete or overwrite users’ `History.cfg` outside Load→migrate→Save semantics.
- Do not add history to Native Search.
