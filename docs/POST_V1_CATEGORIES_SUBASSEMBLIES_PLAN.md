# Post-v1 Plan — Categories, Subcategories & Subassemblies

**Status:** Architecture fleshed out (near wire-up) — **do not ship / do not enable until after v1.0**  
**Mod:** Full Koobal Search Engine (`GameData/KoobalSearchEngine/`)  
**Source:** `Source/PartSearchSuggest/`  
**Written:** 2026-07-11  
**Updated:** 2026-07-11 — PostV1 near-implementation scaffold (indexes, navigator, SA workflow); still excluded from shipping compile  
**Baseline today:** Core-only since **v0.8.4.0** (current packaged line ~v0.8.5.1-beta). Rollback artifact: git tag `v0.8.4.0` + `ReleaseArchive/KoobalSearchEngine_v0.8.4.0_CORE_STABLE.zip`.

### Mission alignment

Canonical mission: [`MISSION.md`](MISSION.md). Category and subassembly suggestions organize the parts list through the editor’s own taxonomy so builders can reach the right tab or craft quickly from the same dropdown. That advances universal access and usefulness without replacing stock category UI.

Related docs: [`ROLLBACK.md`](../ROLLBACK.md), [`ROADMAP.md`](../ROADMAP.md) (partially stale — still marks subassembly “implemented”), [`POST_V1_GLOBAL_SEARCH_HALT.md`](POST_V1_GLOBAL_SEARCH_HALT.md) (**later:** Enter-halt + tight match on every KSP search bar — after this categories/SA band), [`V0_9_HISTORY_ITEM_DELETE.md`](V0_9_HISTORY_ITEM_DELETE.md) (**~0.9 QoL:** per-row history delete — not a v1 blocker), [`V2_PARTS_LIST_AND_SETTINGS.md`](V2_PARTS_LIST_AND_SETTINGS.md) (**V2 later:** slide-expand parts list + Settings; optional Track R rebuild), [`.cursor/rules/koobal-search-engine-suggestions.mdc`](../../../.cursor/rules/koobal-search-engine-suggestions.mdc).

### Scaffold status (near-implementation; not wired)

Fleshed-out architecture under [`../PostV1/`](../PostV1/) (`PartSearchSuggest.PostV1.*`). **Excluded from shipping compile** via `PartSearchSuggest.csproj` (`<Compile Remove="PostV1\**\*.cs" />`). Not referenced by `EditorSearchHook`, indexes, Harmony, ModTest, or Main `GameData`. Feature gates remain off. See [`../PostV1/README.md`](../PostV1/README.md) and [`../PostV1/WIRE_UP.md`](../PostV1/WIRE_UP.md).

Optional compile-check only (does not deploy): `PostV1/PostV1.Architecture.csproj`.

| Phase | Scaffold location | Readiness |
|-------|-------------------|-----------|
| A — suggest-only category/subcategory | `PostV1/Categories/` | Concrete `CategorySuggestionIndex` from snapshots + pure match/score; live PartCategorizer adapter left |
| B — tab navigate/apply port | `PostV1/Apply/` | Full `CategoryTabNavigator` vs `IEditorCategoryUi`; `Unwired*` / `Recording*` ports; live activation left |
| C — subassembly index / apply / delete-refresh | `PostV1/Subassemblies/` | Snapshot index, apply workflow, delete-refresh service + event bus; no Harmony yet |
| Cross-cutting | `PostV1Services`, `Safety/`, `Shared/` | Composition root unused by shipping |

---

## 1. Goals / user stories

Restore the ability to **find and jump to** stock/custom category surfaces and player subassemblies from the same predictive dropdown that already handles parts, categorizer filters, and mod/author/suite — without re-breaking stock UI.

| # | Story | Feel |
|---|--------|------|
| U1 | As a builder, I type part of a **stock category or subcategory** name and see it ranked with other organic suggestions. | Same dropdown language as filters/mods; icons OK when they map to real tabs. |
| U2 | As a builder, I type a **custom part/subassembly category** (cfg / CCK / advanced tabs) and find it. | Parent › subcategory labels when names collide; counts honest. |
| U3 | I **click** a category/subcategory row and the editor **switches to that tab** (same parts/crafts the tab shows). | Apply-on-select; no blank list; no stuck custom filter. |
| U4 | I type a **subassembly craft name** and click to **select/place** it the way stock does. | Tab switch + craft focus; not a random part text search. |
| U5 | Category / subassembly rows **coexist** with parts, FilterFunction/Tag/etc., ModName/Author/Suite, and history. | Organic RankScore; dedup; min query length / broad-match guards. |
| U6 | Empty / invalid / **0-result** rows never appear (or for categories that are legitimately empty, product decision — see open questions). | Index predicate === apply predicate; `IsValid()` + subtitle refresh. |

**Success metric:** Clicking a suggestion produces the same visible list as clicking the matching stock/CCK/custom tab (or stock subassembly icon), and stock category buttons / delete / subassembly browser keep working with Harmony fail-open or no permanent patches on those paths.

---

## 2. What existed before / why it was stripped

### 2.1 Feature arc (v0.8.0 → v0.8.3.17)

| Band | What shipped | Pain points (evidence) |
|------|----------------|-------------------------|
| **v0.8.0.x** | Subassembly search: folder scan, incremental save/delete, early `TapIcon` apply → later tab + craft-path filter | Transient `KoobalSearchEngine_Subassembly` filter leaked; delete dialog races; crafts vanished after apply |
| **v0.8.1.x** | Custom categories: cfg bootstrap (`PartCategories.cfg` / `SubassemblyCategories.cfg`), live merge, icons, tab navigation | CTD on `SearchStop` recursion; typing vs `SearchRoutine` races; zero-count / resolve gating churn |
| **v0.8.2.x** | Navigate-only custom category apply (no transient parts filter); icons | Tab apply still unreliable vs stock |
| **v0.8.3.x** | `StockTabNavigator`, `displayType` snapshot/restore, delete-dialog guard, FilterFunction→tab navigation attempts | **v0.8.3.16:** writing `Category.displayType` on stock click path → stock category buttons stopped filtering (blank via `filterGenericNothing`). **v0.8.3.17:** patched fail-open but still deep Harmony on categorizer internals |

Transcript themes (same session lineage as [Koobal search build](91b396b8-680d-4380-bee3-5ea05c58baff)): tab clicks not navigating; subassembly tab broken after apply; custom category label/parent confusion; repeated Harmony/UI integration firefights.

### 2.2 Core-only cut — v0.8.4.0

**Intent:** de-risk for v1. Strip every deep stock-integration feature that repeatedly broke the hangar.

**Removed (per `ROLLBACK.md`):**

- Subassembly stack: `SubassemblyMatcher`, `SubassemblyEntry`, `SubassemblySuggestionIndex`, `StockSubassemblyHelper`, `SubassemblyLifecycleHooks`, `AllSubassemblies`
- Custom category stack: `CustomCategoryMatcher`, `CustomCategoryEntry`, `CustomCategorySuggestionIndex`, `StockCustomCategoryHelper`, `CustomCategoryLifecycleHooks`, `CustomCategoryCfgReader`
- Tab orchestration: `StockTabNavigator`, `PartCategorizerUiHelper`
- **All** Harmony touching category / subassembly / refresh / delete / lifecycle / `displayType`
- Extra `SuggestionKind` values for those surfaces

**Kept:** predictive dropdown; parts + categorizer **metadata filters** (`FilterCategory` = `part.category` enum filter, **not** tab navigation); manufacturers/tags/modules/resources/tech; mod/author/suite; history; branding; **read-only** category display icons from live `PartCategorizer` buttons; minimal `StockSearchGuard` (apply/custom-filter race only).

**Hard rule since then:** stock parts list, filter buttons, category tabs, subassembly tab, and delete are **100% native**. Only stable interaction is passive reads + v0.7-era apply (`searchField` + `Refresh(PartSearch)` / `SearchFilterResult`).

### 2.3 Leftovers in current tree (hooks / comments, not features)

| Artifact | Role today |
|----------|------------|
| `CustomCategoryIconHelper` / `StockCategorizerIconHelper` | Icon resolution for **FilterFunction / FilterCategory** display icons — not custom-category search |
| `SuggestionKind.FilterCategory` | Predicate on `AvailablePart.category` via `PartFilterMatcher` — **not** stock tab switch |
| `StockSearchGuard` comments | Document SearchRoutine-null / SearchStart NRE lesson (v0.8.5.1) — still mandatory for any future apply |
| Cursor rule + `ROADMAP.md` | Still describe Subassembly / CCK apply paths as if present — **stale relative to v0.8.4+**; update when implementing |

No live `Subassembly*` / `CustomCategorySuggestion*` / `StockTabNavigator` types in the active source tree.

---

## 3. Technical inventory

### 3.1 KSP / UI types

| Area | Types / surfaces | Notes |
|------|------------------|-------|
| Search / categorizer | `PartCategorizer`, `BasePartCategorizer`, `searchField`, `SearchStart` / `SearchRoutine` / `SearchStop`, `SearchFilterResult` | Koobal already hooks `searchField`; guard must **never** Prefix-skip `SearchRoutine` (returns null → `StartCoroutine(null)`) |
| Parts list | `EditorPartList`, `Refresh`, `RefreshSubassemblies`, `SearchFilterParts`, `TapIcon` | Subassembly apply historically fought filters + refresh mid-delete |
| Categories | Nested `PartCategorizer.Category`, buttons, `subcategories`, `filters`, `filterFunction` | `displayType` enum: `PartsList=0`, `SubassemblyList=1`, `CustomPartList=2`, `PartSearch=3` — stock branches on this; wrong writes blank the list |
| Custom categories | `PartCategories.cfg`, `SubassemblyCategories.cfg`, live `LoadCustom*` merge | Cfg bootstrap vs live resolve was a major 0.8.1 bug class |
| CCK | `CommunityCategoryKit` registration → custom tabs | Planned in `ROADMAP.md` §1; not the same as Squad cfg custom categories |
| Subassemblies | `Saves/{save}/Subassemblies/` (VAB/SPH layout), craft/`ShipTemplate`, stock SA browser | Index **editor-entry + incremental**; never rebuild part/metadata/categorizer indexes on SA lifecycle |

### 3.2 Harmony risk zones (do not casually reintroduce)

| Patch target | Historical failure |
|--------------|-------------------|
| `OnTrueCATEGORY` / `OnTrueSUB` / `OnFalse*` | `displayType` desync → silent blank filters |
| `SearchStop` / typing pipeline | StackOverflow via virtual re-entry; list refresh side-effects |
| `SearchRoutine` Prefix return false | NRE on SearchStart |
| `EditorPartList.Refresh` / `RefreshSubassemblies` during delete dialog | Popup NRE, UI lock |
| Permanent `displayType` writes outside scoped apply | Stock click path broken |

**Preferred apply strategy (learned):** navigate by simulating stock button state / calling the same activation path stock uses, with **transient** scoped state only inside an apply IDisposable — **never** leave Harmony prefixes that rewrite state on every stock click. Prefer postfix/observe or pure managed API over Prefix skip.

### 3.3 Indexing timing

| Data | When | Rule |
|------|------|------|
| Parts / metadata / categorizer filters | Save-load (`GameLoadBootstrap` → `GameLoadIndexService`) | Hangar must **not** start builds (`EditorSearchHook` wait/UI only) — v0.8.5.1 hangar-free lock fix |
| Custom / CCK category **names** (read-only Phase A) | Prefer editor entry once `PartCategorizer` ready (+ cfg bootstrap if needed) | PartCategorizer often absent at save-load |
| Subassemblies | Editor entry scan + incremental save/delete | Folder-scoped only |

---

## 4. Proposed architecture (phased)

Guiding rules (unchanged):

1. **Index predicate === apply predicate** (cursor checklist).
2. **Stock UI remains authoritative** — Koobal suggests and navigates; it does not own category/delete state machines.
3. **Ship each phase behind a compile/feature flag or version gate**; ModTest before main.
4. **Full mod only** — Native Search standalone is a separate product; no mutual-exclusivity conflict for this work (see §4.4).

### Phase A — Read-only index + suggestions (safest)

**Ship:** Suggest stock category/subcategory names, custom cfg categories/subcategories, and (optional stretch) CCK tab **labels** — **display + rank only**. Click may no-op or only fill the search field **without** tab switching (product choice; prefer no-op apply until Phase B).

**Work:**

- New kinds e.g. `StockCategoryTab`, `StockSubcategoryTab`, `CustomPartCategory`, `CustomSubassemblyCategory` (names TBD).
- Index sources: live `PartCategorizer` category tree + cfg bootstrap for customs; CCK enumeration if API known.
- Ranking: first-class alongside categorizer/metadata (high RankScore band), subject to min length / broad-match guards.
- Icons: reuse `StockCategorizerIconHelper` / `CustomCategoryIconHelper`.
- Subtitles: `parent › sub · N parts` (or `N crafts` for SA categories) via shared matcher counts when available; hide if invalid.

**Out of scope:** Harmony on category clicks; `displayType` writes; subassembly craft apply.

**Exit:** Typing finds the right row; stock hangar behavior unchanged vs core-only; no new SearchStart races.

### Phase B — Apply = switch category / filter without nuking stock UI

**Ship:** Click navigates to the matching stock/custom/CCK tab (and subcategory) so the parts list matches a manual tab click.

**Work:**

- Rebuild a **thin** navigator (spiritual successor to `StockTabNavigator`) with lessons baked in:
  - No Harmony Prefix that mutates `displayType` on stock `OnTrue*` paths.
  - Apply scope: activate parent then subcategory radio buttons (correct `UIRadioButton.SetState` signature); restore any captured state on dispose.
  - Fail-open try/catch; on failure call `RecoverAfterFailedApply`.
- Shared matchers for counts + `IsValid()`.
- Clear Koobal custom filters before tab apply; clear on stock SearchStart / focus-typing (existing 0.8.5.1 pattern).
- Dedup vs `FilterFunction` / `FilterCategory` rows (tab navigate vs predicate filter — decide which wins when both match “Engines”).

**Hard bans this phase:** delete-dialog Harmony; `RefreshSubassemblies` blocking; SearchRoutine skip; permanent displayType patches.

**Exit:** Category click === manual tab click; stock Structural/RCS/… buttons still work after many applies; no blank list.

### Phase C — Subassemblies search + apply (safety rails)

**Ship:** Index crafts under current save Subassemblies folders; suggest by title; apply selects/places via stock-equivalent path.

**Work:**

- Restore incremental `SubassemblySuggestionIndex` + lifecycle hooks (**folder-scoped only**).
- Apply: switch to subassembly tab + **short-lived** craft-path filter (or proven stock select API) — clear filter on dismiss, tab change, search focus, timeout (historical 45s), and **before delete dialog**.
- Prefer **postfix-only** index updates after stock confirms delete — do not Prefix-delete or mid-dialog refresh.
- Validate craft/`ShipTemplate`; drop missing files from index.
- Optional: `AllSubassemblies` root tab as its own kind (was v0.8.3.12).

**Exit:** Save → search → appear; delete → vanish; click → place/select without emptying SA browser; delete popup closable; large SA libraries stay responsive (incremental, not full reindex).

### 4.4 Native Search / product split

| Product | Relevance |
|---------|-----------|
| **Full Koobal Search Engine** | This plan applies. |
| **Koobal Native Search / standalone** | Explicitly out of CKAN full-mod submission; **N/A** for mutual exclusivity here. If Native Search ever gains category features, keep shared matcher libs but separate GameData folders / Harmony IDs. |

---

## 5. Risk register

| ID | Risk | Severity | Mitigation |
|----|------|----------|------------|
| R1 | `displayType` desync blanks parts list / kills stock category buttons | **P0** | Phase A first; Phase B no stock-click Prefix writers; scoped restore only |
| R2 | SearchStart / SearchRoutine Harmony races (null coroutine, typing leak) | **P0** | Keep minimal StockSearchGuard; never skip SearchRoutine; reuse RecoverAfterFailedApply |
| R3 | Delete-dialog + RefreshSubassemblies NRE / UI lock | **P0** | Phase C: no mid-dialog refresh; postfix index only |
| R4 | Transient SA/custom filters stick → crafts/parts “disappear” | **P0** | Explicit clear matrix + timeout; never leave filter across tab changes |
| R5 | Hangar freezes / stuck build locks from editor-time indexing | **P1** | Keep GameLoadIndexService sole builder for parts/meta; SA/category scans frame-sliced at editor entry |
| R6 | Cfg vs live PartCategorizer resolve mismatch (ghost / missing rows) | **P1** | Bootstrap + deferred merge; don’t gate Match on resolve until live ready (or show pending carefully) |
| R7 | Performance with hundreds of subassemblies | **P2** | Incremental index; title-first match; cap rows; optional deferred part-count |
| R8 | Save/craft corruption | **P1** | Read-only craft parse for index; never write `.craft` from Koobal; apply only stock placement APIs |
| R9 | Dedup collisions (FilterCategory enum vs tab “Engine”) | **P2** | Explicit priority table in Phase B |
| R10 | Stale ROADMAP / cursor rule causing wrong reimplementation | **P2** | Update docs when Phase A starts; prefer this plan + `ROLLBACK.md` over old “implemented” checkboxes |

---

## 6. Acceptance criteria per phase

### Phase A

- [ ] Query (≥2 chars) surfaces stock and custom category/subcategory rows with correct labels (parent › child when needed).
- [ ] Rows respect zero/invalid guards (per agreed empty-category policy).
- [ ] Coexist with parts / filters / mods in one ranked list; icons optional for real category rows.
- [ ] **No** change to stock tab click, delete, or SA browser behavior vs pre-A build.
- [ ] ModTest smoke: VAB + SPH; KSP.log clean of categorizer exceptions.

### Phase B

- [ ] Click category/subcategory → same visible set as manual tab click (spot-check stock + custom + CCK if included).
- [ ] After apply, clicking stock category buttons still filters correctly (regression for v0.8.3.16).
- [ ] Failed apply recovers (dropdown dismiss, list usable, no stuck suppress).
- [ ] No permanent Harmony on `OnTrueCATEGORY`/`OnTrueSUB` that writes `displayType`.
- [ ] Subtitle count === visible part count after apply (log assert).

### Phase C

- [ ] New SA appears same session after save (incremental).
- [ ] Deleted SA disappears after stock confirms delete; delete dialog closable.
- [ ] Click apply places/selects intended craft; SA tab not emptied afterward.
- [ ] No full part/metadata/categorizer rebuild on SA lifecycle.
- [ ] Stress: ≥50–100 SAs indexed without hitch on editor entry (frame budget).

---

## 7. Explicit non-goals for v1

Keep the **current core stable** through public v1.0:

- Do **not** restore custom-category search, stock tab navigation, or subassembly search in any v0.9 / v1.0 release candidate.
- Do **not** reintroduce delete-tab / `displayType` / SearchStop typing-pipeline Harmony.
- Do **not** move indexing back onto hangar entry for parts/metadata/categorizer.
- Do **not** treat `FilterCategory` predicate rows as “category tab restore” — they stay metadata filters until Phase B kinds exist.
- Do **not** block v1 on CCK API research.
- Thumbnails remain deferred/abandoned (separate doc).

v1 ships the core-only value prop: predictive parts + organic filters + mod/author/suite + history + branding, with stock UI untouched.

---

## 8. Open questions (for Tim)

1. **Empty custom categories:** Show `· 0 parts` rows (old v0.8.1.6–7 behavior) or hide until count &gt; 0 (core filter rule)?
2. **Category click semantics:** Prefer **tab navigation only** (v0.8.2.1) forever, or allow a secondary “filter parts like this category” predicate when no live tab exists?
3. **Subassembly click:** Filter-to-craft in SA browser first, or jump straight into **placement** (stock icon click)?
4. **CCK priority:** Phase A/B include CCK tabs in the first post-v1 slice, or ship Squad cfg + stock tabs first and CCK as Phase B.1?
5. **Feature flag:** Opt-in `PluginData` toggle for post-v1 category/SA features (safe rollback without downgrade), or hard version-gated releases only?

---

## 9. Suggested implementation order (when post-v1 starts)

1. Refresh this plan + fix stale `ROADMAP.md` / cursor-rule “implemented” sections.  
2. Phase A on ModTest (icons + suggest only).  
3. Phase B navigator with automated regression checklist (stock buttons after apply).  
4. Phase C SA with delete-dialog torture tests.  
5. Optional CCK / organizer providers.  
6. **Later (separate band):** global search halt — [`POST_V1_GLOBAL_SEARCH_HALT.md`](POST_V1_GLOBAL_SEARCH_HALT.md) / `PostV1/GlobalSearch/`.  
7. Tag + ReleaseArchive zip per `RELEASE_PROCESS.md`; keep v0.8.4.0 as emergency core rollback until the new band has its own verified baseline.

---

*Architecture near-implementation under PostV1/ — excluded from shipping DLL; feature gates off. No plugin behaviour changes until wire-up.*
