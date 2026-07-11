# Post-v1 Plan — Global Search Halt + Tightened Matching

**Status:** Architecture fleshed out (near wire-up) — **do not ship / do not enable until after v1.0 and after categories/subassemblies PostV1 work**  
**Capability:** Core **native/stock search improvements** extended to every KSP search bar — Enter-to-search halt + tightened matching (+ SearchStart-style guards where needed). **No** predictive chrome on global bars.  
**Products:** Full Koobal ships this as part of its native-search core (alongside dropdown/history/etc.). **Koobal Native Search** is the lightweight standalone slice of that same core and gets the same improvements when they ship — kept in parity with **any relevant** full-mod fixes. Native does **not** exclusively own global search.  
**Source (scaffold today):** `Source/PartSearchSuggest/PostV1/GlobalSearch/` — stays here until extracted; Native adopts later via the standalone parity policy (see §7).  
**Written:** 2026-07-11  
**Baseline today:** Editor Enter-halt + tightened matching already proven in:

| Product | Path | Behaviour |
|---------|------|-----------|
| **Koobal Native Search** | `Source/KoobalNativeSearch/` | Standalone slice: Enter-to-search halt, `NativeEnterMatcher`, SearchStart guards; branding only on main editor bar — **no** dropdown |
| **Full Koobal** | `Source/PartSearchSuggest/` | Same native-search core **plus** predictive dropdown, history, suggestions |

This plan extends **halt + tight match only** to **every** stock search bar. That fits Native’s “native search improvements” scope and is also part of full Koobal’s core — not a Native-only feature.

### Mission alignment

Canonical mission: [`MISSION.md`](MISSION.md). Global Enter-halt + tight match makes search deliberate and consistent on every KSP search bar, not only the editor. That is universal access via the **native search improvements** core: full Koobal has it (plus full UI); Native Search is the standalone slice and stays in parity when these fixes land.

Related: [`POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md`](POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md) (earlier PostV1 band), [`../PostV1/GlobalSearch/README.md`](../PostV1/GlobalSearch/README.md), [`../PostV1/GlobalSearch/WIRE_UP.md`](../PostV1/GlobalSearch/WIRE_UP.md), Native future note [`../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md`](../../KoobalNativeSearch/docs/FUTURE_GLOBAL_SEARCH.md).  
Also see (unrelated QoL, target ~0.9): [`V0_9_HISTORY_ITEM_DELETE.md`](V0_9_HISTORY_ITEM_DELETE.md).  
**V2 later** (slide-expand parts list + Settings; optional rebuild): [`V2_PARTS_LIST_AND_SETTINGS.md`](V2_PARTS_LIST_AND_SETTINGS.md).

### Scaffold status (near-implementation; not wired)

| Piece | Location | Readiness |
|-------|----------|-----------|
| Surface enum + registry | `GlobalSearch/SearchBarSurface.cs`, `SearchSurfaceRegistry.cs` | Done — inventory + confidence tags |
| Halt port | `ISearchExecutionHalt` + Unwired/Recording | Done — algorithm for cancel/suppress bookkeeping |
| Tight matcher | `ITightSearchMatcher` + `TightSearchMatcher` | Done — NativeEnterMatcher-style pure scoring on snapshots |
| Apply port | `ISearchResultApplier` + Unwired/Recording | Done — id-set filter apply bookkeeping |
| Orchestrator | `GlobalSearchOrchestrator` | Done — type→halt, Enter→match→apply |
| Composition / gate | `GlobalSearchServices`, `GlobalSearchFeatureGate` | Done — gate **always false** |
| Wire-up checklist | `GlobalSearch/WIRE_UP.md` | Done |

**Excluded from shipping compile** via `PartSearchSuggest.csproj` (`<Compile Remove="PostV1\**\*.cs" />`). Not referenced by `EditorSearchHook`, Harmony, ModTest, or Main `GameData`.

Optional compile-check: `PostV1/PostV1.Architecture.csproj` (outputs `PostV1/bin/`, never GameData).

---

## 1. Goals

| # | Goal | Feel |
|---|------|------|
| G1 | **No live-as-you-type lag** on any hooked search bar — stock deferred/search-coroutine work is cancelled while typing. | Typing is free; list does not thrash. |
| G2 | **Enter (or explicit submit) runs search** with **tightened** matching (no description-only / junk hits where the surface supports fielded scoring). | Same quality bar as Native Search Enter on parts. |
| G3 | **Zero Koobal chrome** on non-editor (and optionally editor) surfaces: no wordmark branding panel, no suggestion dropdown, no predictive UI. | Stock UI + better function only. |
| G4 | One native-search core for **full Koobal** and the **Native standalone slice** — avoid divergent matchers/guards. | Parity policy: port **any relevant** full-mod fixes into Native. |

**Success metric:** Typing never starts stock search work; Enter produces a result set that matches tightened rules; stock UI still works when the feature is off; no SearchRoutine-null / SearchStart NRE class bugs on editor path; other surfaces fail-open.

---

## 2. Non-goals

- **No branding** (wordmark / empty-field chrome) on global surfaces.
- **No dropdowns / predictive suggestions** as part of this feature.
- **No** change to shipping Native Search or full Koobal behaviour in this architecture band (standalone parity / shared-core adoption is later — see §7). Do **not** wire GlobalSearch into live Native or full Koobal yet.
- **No** wiring into current shipping `KoobalSearchEngine` / ModTest / Native GameData until after v1 + categories/SA PostV1.
- **No** full working Harmony patches in the scaffold — ports + pure algorithms only.
- **Not** a replacement for categories/subassemblies dropdown work — that remains a separate PostV1 band.

---

## 3. Proven patterns (editor) — source of truth

### 3.1 Enter-to-search halt

From `KoobalNativeSearch` / full Koobal `StockSearchHelper` + `EditorSearchHook`:

1. On `onValueChanged` / focus: **`CancelPendingStockSearchForTyping`**
   - Clear custom-filter race guard when not mid-suppress.
   - **Stop** `BasePartCategorizer.searchRoutine` coroutine via reflection; set field null.
   - Reset typing search flags (`searchTimer` / `searching` as appropriate).
2. On Enter / `onSubmit`: **`ApplyEnterSearch`** with tightened matcher → custom `EditorPartListFilter` via `SearchFilterResult`.
3. **`StockSearchGuard`:** block void `SearchStart` only while apply-suppressed; **never** Prefix-skip `SearchRoutine` (IEnumerator → null → `StartCoroutine(null)` NRE — v0.8.5.1 lesson).

### 3.2 Tightened matching (`NativeEnterMatcher`)

- Index fields: title, name, tags, category, module, manufacturer, tech, auto-tags; **description indexed but excluded** from Enter qualification (`EnterSearchMaxAggregateScore` = `AutoTagContains`; best field must not be Description).
- Multi-word: every word must match; aggregate takes worst (highest) score.
- Short queries (≤2 chars): title-first priority.
- Min query length: 2 (`SuggestionQueryGuards`).
- No-match: **do not** fall back to loose stock text search (list unchanged).

Full Koobal additionally prefers metadata filter kinds on Enter when the dropdown/index says so; **global halt feature** stays Native-style: fielded item match only, per surface.

### 3.3 What to extract as shared core

| Shared | Not shared (per surface) |
|--------|---------------------------|
| Score tables / field kinds / qualify rules | How to cancel that UI’s pending search |
| Snapshot item model + `TightSearchMatcher` | How to apply an id-set filter to the list |
| Halt bookkeeping (suppress depth, “typing active”) | Harmony targets / TMP listeners |
| Orchestrator: type→halt, Enter→match→apply | Scene lifecycle / when to build index |

---

## 4. Surface inventory

Confidence: **Proven** = already shipping for editor; **Likely** = KSP has a known UI with a filter/search field; **Needs runtime confirmation** = verify class names, events, and whether search is live-as-you-type before patching.

| Surface | Enum | Confidence | Notes / likely stock types |
|---------|------|------------|----------------------------|
| VAB/SPH part search | `EditorPartList` | **Proven** | `PartCategorizer.searchField`, `SearchStart` / `SearchRoutine` / `SearchFilterResult`. Full Koobal dropdown already owns chrome here — global feature must **not** double-hook Enter/halt when full Koobal is active (see §7). |
| R&D tech tree | `ResearchAndDevelopment` | Needs runtime confirmation | Tech node filter field on RD UI; often live filter. Confirm `RDController` / related TMP or InputField + list rebuild cost. |
| Tracking Station | `TrackingStation` | Needs runtime confirmation | Vessel list filter; confirm Space Tracking UI search/filter control. |
| KSPedia | `KSPedia` | Needs runtime confirmation | Knowledge base / KSPedia search; may be its own scene UI. |
| Settings / mods lists | `SettingsOrModsList` | Needs runtime confirmation | Difficulty options, in-game settings search, or mod list filters if stock exposes them. |
| Agency / contracts | `AgenciesOrContracts` | Needs runtime confirmation | Mission Control / contracts browser — **may lack** a search bar; confirm before spending Harmony. |
| Save / load / craft browser | `CraftBrowser` | Needs runtime confirmation | `CraftBrowserDialog` name filter — often filters as you type. |
| Action Groups | `ActionGroups` | Needs runtime confirmation | AGX-less stock AG editor part/action filter if present. |
| Astronaut Complex / kerbal lists | `KerbalOrRoster` | Needs runtime confirmation | Roster / hire UI filters. |
| Flag / other browsers | `OtherBrowser` | Needs runtime confirmation | Catch-all after audit. |

**Inventory method when implementing:** scene walk + TMP_InputField dump in each facility; log `onValueChanged` subscribers; classify live vs submit-only. Prefer listener cancel over broad Harmony.

---

## 5. Per-surface approach

### Shared algorithm (all surfaces)

```
onType / onFocus  →  ISearchExecutionHalt.CancelPending(surface, reason)
onEnter / onSubmit →  matches = ITightSearchMatcher.Match(query, snapshot)
                     if any → ISearchResultApplier.Apply(surface, matches, query)
                     else   → leave list unchanged (no loose stock fallback)
```

### Editor (`EditorPartList`) — Phase G1 (already proven; reuse)

- Halt = existing `CancelPendingStockSearchForTyping` / `CancelSearchRoutine`.
- Match = `NativeEnterMatcher` or shared `TightSearchMatcher` on part snapshots.
- Apply = `SearchFilterResult` + custom filter id + `StockSearchGuard`.
- **Full Koobal:** editor already has halt+Enter+dropdown — do **not** install a second halt listener. Global module **skips** `EditorPartList` when full Koobal’s editor dropdown path is active.
- **Native Search standalone:** already implements the editor native-search core; later extends that same slice to global bars and stays in parity with relevant full-mod fixes.

### Non-editor surfaces — Phases G2+

| Concern | Approach |
|---------|----------|
| Halt | Cancel debounce coroutine / clear dirty flag / unsubscribe stock live filter for the duration of typing; restore on Enter or clear. Prefer public events; Harmony Prefix only if stock has no cancel API. |
| Match | Build snapshot of list rows (id, title, tags, secondary fields). Reuse `TightSearchMatcher` with surface-specific field maps (e.g. tech: title+techId; vessel: name+type). Description-only exclusion where descriptions exist. |
| Apply | Filter visible rows by matched id set, or invoke stock “apply filter” if it accepts a predicate. Prefer id HashSet like editor. |
| Fail-open | If reflection/Harmony fails, leave stock live search alone; log once. |

---

## 6. Harmony / risk

| Risk | Severity | Mitigation |
|------|----------|------------|
| Prefix-skip `SearchRoutine` (or any IEnumerator search) | **P0** | Never skip IEnumerator methods; cancel coroutine instance instead; block void entry points only while suppressed. |
| Double-hook with full Koobal dropdown on editor | **P0** | Surface registry: `EditorPartList` owned by full Koobal when present; global module skips. |
| Conflict with Native Search standalone | **P1** | Soft exclusivity (existing `FullKoobalDetector`); install one product. Prefer shared native-search core + Native parity updates over dual divergent patches. |
| Wrong field hooked (settings vs search) | **P1** | Runtime confirmation + descriptor allowlist; no “hook every TMP”. |
| Apply leaves list empty permanently | **P1** | Clear filter on empty query / Escape / scene exit; Recording ports assert clear paths. |
| Perf: rebuilding snapshots every keystroke | **P2** | Snapshot on surface open / data change; Match is pure O(n·fields·words). Halt must be O(1) cancel. |
| Mod UI conflicts (Filter Extensions, etc.) | **P2** | Fail-open; document known conflicts; Harmony IDs unique per product. |

---

## 7. Product relationship & coexistence

**Framing:**

| Product | What it is |
|---------|------------|
| **Full Koobal Search Engine** | Full product (dropdown, suggestions, history, later categories/V2, …) **plus** the core native/stock search improvements. |
| **Koobal Native Search** | Standalone slice of **some** of that core: native/stock search improvements (Enter-halt, tight results, SearchStart guards, …). Branding only on the **main** editor bar. No dropdown / history / other full-Koobal UI. |

Future global-bar halt/tighten is more of the same **native search improvements** core — both products get it when shipped. Native does **not** exclusively own global search. **Ongoing policy:** continue updating the standalone with any relevant fixes from full Koobal (parity for shared core search behavior).

Architecture scaffolding stays in `PartSearchSuggest/PostV1/GlobalSearch/` until extracted. Do **not** wire into live products in this band.

### 7.1 Full Koobal

| Surface | Behaviour |
|---------|-----------|
| Editor part search | Halt + Enter + **dropdown** + branding — already handled. Global Search Halt module **does not** double-attach on the editor bar. |
| All other bars | Native-search core only: halt + tight Enter — **no** dropdown, **no** branding. |

### 7.2 Native Search standalone

- Today: editor halt + tight Enter + SearchStart guards + branding on the main bar only.
- Future: same native-search-improvements scope extends to other KSP search bars (halt + tighten). Still **no** dropdown, history, suggestions, or other full-Koobal UI; branding stays on the main editor bar only (no new chrome on global bars).
- Parity: when full Koobal lands relevant core search fixes, port them into Native so the standalone stays aligned.
- Implementation options later: shared project, copy-with-parity, or adapters from this PostV1 scaffold — product decision, not “Native owns the feature.”

Do **not** change Native Search or full Koobal shipping behaviour in this architecture band. Do **not** wire GlobalSearch into live Native or full Koobal yet.

---

## 8. Performance (“no lag”)

1. **Halt is the product:** keystroke must not start stock scan/coroutine/list rebuild.
2. **Match runs on Enter only** — never on `onValueChanged`.
3. **Snapshots** built once per surface session (or on data change), not per key.
4. **No predictive UI work** on these bars — zero dropdown layout/rank cost.
5. Editor lessons: cancel orphaned `SearchRoutine` each keystroke; do not call `SearchStop` if it forces a refresh (historical typing-leak / CTD paths).

---

## 9. Phased rollout

| Phase | Scope | Exit criteria |
|-------|--------|----------------|
| **G0** | Architecture scaffold (this folder) + plan | Architecture project compiles; self-check Match/Halt/Apply bookkeeping passes; gates off; Compile Remove intact |
| **G1** | Editor — already proven; optional shared-core swap behind gate | Behaviour parity between full Koobal Enter path and Native standalone |
| **G2** | R&D tech tree | Typing no lag; Enter tight filter; fail-open; ModTest only |
| **G3** | Tracking Station | Same |
| **G4** | Craft browser (save/load) | Same |
| **G5** | KSPedia + remaining confirmed surfaces | Same; skip surfaces with no search bar |
| **G6** | Native standalone parity — port global halt+tighten (+ any relevant full-mod core fixes) into Native | Standalone stays the lightweight slice of the same native-search core |

**Order rule:** never start G2+ until G0 solid and categories/SA PostV1 band is done (or explicitly deprioritized by product owner). Editor-proven patterns first.

---

## 10. Explicit bans (this band)

- Do not ship GlobalSearch in v1.0.
- Do not deploy architecture DLL to GameData / Main / ModTest as an “enable”.
- Do not add branding or dropdowns for global bars.
- Do not Prefix-skip SearchRoutine / IEnumerator search methods.
- Do not fall back to loose stock search on zero tight matches.
- Do not double-hook editor when full Koobal is active.
- Do not rebuild part/`SuggestionIndex` on non-editor surface open.

---

## 11. Open questions

1. Should editor branding remain Native-only / full-Koobal-only forever (global feature never adds chrome)? **Recommendation: yes.**
2. Per-surface PluginData toggles vs one master `EnableGlobalSearchHalt`?
3. Craft browser: tighten name-only, or also description/author like SA matcher?
4. Promote shared core before G2, or duplicate thin adapters until G2 proves value? (Either way: Native remains the standalone slice and gets parity updates for relevant core search fixes.)

---

*Architecture near-implementation under `PostV1/GlobalSearch/` — excluded from shipping DLL; feature gates off. No plugin behaviour changes until wire-up.*
