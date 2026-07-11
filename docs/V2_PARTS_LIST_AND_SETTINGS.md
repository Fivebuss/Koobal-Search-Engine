# V2 Plan — Parts List Experience + Mod Settings Tab

**Status:** Vision + architecture scaffold only — **post-v1 / V2 band** — **do not ship / do not enable until after v1.0 and after earlier PostV1 bands are intentionally scheduled**  
**Mod:** Full Koobal Search Engine (`GameData/KoobalSearchEngine/`)  
**Source:** `Source/PartSearchSuggest/PostV1/V2/`  
**Written:** 2026-07-11  
**Updated:** 2026-07-11 — dual-track framing (**Track S** slide-expand preferred; **Track R** full rebuild optional with go/no-go)  
**Baseline today:** Core-only parts list (stock `EditorPartList` + Koobal search overlay). Dropdown open → `PartsPanelCollapseHelper` slides/collapses the parts panel to make room. Settings are cfg / PluginData only — **no** first-class KSP Settings tab.

### Mission alignment

Canonical mission: [`MISSION.md`](MISSION.md). Settings and slide-expand (Track R rebuild gated) make the parts list itself faster to use and more controllable—layout serving organization and usefulness, not a wholesale editor replacement. Prefer Track S; only pursue Track R when go/no-go says the mission needs a rebuild.

Related: [`POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md`](POST_V1_CATEGORIES_SUBASSEMBLIES_PLAN.md) (earlier PostV1), [`POST_V1_GLOBAL_SEARCH_HALT.md`](POST_V1_GLOBAL_SEARCH_HALT.md) (**later** global halt), [`V0_9_HISTORY_ITEM_DELETE.md`](V0_9_HISTORY_ITEM_DELETE.md) (**~0.9** history QoL), [`../PostV1/V2/README.md`](../PostV1/V2/README.md), [`../PostV1/README.md`](../PostV1/README.md).

### Scaffold status (architecture only; not wired)

| Piece | Location | Readiness |
|-------|----------|-----------|
| Settings model + store | `V2/ModSettings/` | Done — defaults, memory store, cfg codec |
| Settings tab host (Unwired) | `ISettingsTabHost` + section ports | Done — boundary docs; no Unity |
| Parts list layout prefs | `PartsListExperience/` enums + pure resolver | Done — icon size / list style / slide-expand |
| Slide-expand controller (Track S) | `IPartsListSlideExpandController` | Done — offset/width/height intent; composes with dropdown |
| Rebuild host port (Track R) | `IPartsListRebuildHost` | Done — optional path; gated; Unwired |
| Organizer bridge | `IPartsListOrganizerBridge` | Done — stub compatibility matrix |
| Virtualization port | `IPartsListVirtualizationPort` | Done — usable by S (in-place) or R (owned list) |
| Track selection | `PartsListArchitectureTrack` + go/no-go helper | Done — pure criteria |
| Composition / gate | `V2Services`, `V2FeatureGate` | Done — gate **always false** |
| Wire-up checklist | `V2/WIRE_UP.md` | Done |

**Excluded from shipping compile** via `PartSearchSuggest.csproj` (`<Compile Remove="PostV1\**\*.cs" />`). Not referenced by `EditorSearchHook`, Harmony, ModTest, or Main `GameData`.

Optional compile-check: `PostV1/PostV1.Architecture.csproj` (outputs `PostV1/bin/`, never GameData).

---

## 0. Dual-track architecture (prominent)

V2 presents **two deliberate tracks**. They share Settings, layout preference enums, organizer compatibility, and performance ports. They differ in **who owns the icon/row surface**.

```
                    ┌─────────────────────────────────────┐
                    │  Settings tab + KoobalSettingsModel │
                    │  (required for either track)        │
                    └──────────────┬──────────────────────┘
                                   │
              ┌────────────────────┴────────────────────┐
              ▼                                         ▼
┌─────────────────────────────┐       ┌─────────────────────────────────┐
│ Track S — Slide expand      │       │ Track R — Rebuild parts list    │
│ PREFERRED / default path    │       │ OPTIONAL / parallel research    │
│                             │       │                                 │
│ • Slide-grow panel geometry │       │ • New list UI hosts icons/rows  │
│   (same family as dropdown) │       │ • Stock EditorPartList may be   │
│ • Soft icon size / style    │       │   hidden or starved of binds    │
│ • Optimize stock list in    │       │ • Still consume same part set / │
│   place (cache, defer,      │       │   filters / organizer predicates│
│   virtualize if feasible)   │       │ • Higher risk/reward            │
│ • Compose w/ dropdown slide │       │ • Explicit go/no-go gate        │
└─────────────────────────────┘       └─────────────────────────────────┘
```

| | **Track S (Slide expand)** | **Track R (Rebuild)** |
|--|----------------------------|------------------------|
| **Stance** | **Default preferred UX** | Deliberate optional path — **not** the default assumption, **not** forbidden |
| **Surface** | Stock `EditorPartList` stays owner; Koobal changes **geometry** + soft reflow | Koobal owns a **new** list UI; “KSP is old” — rebuild if research justifies |
| **Interaction** | Slide/grow like today’s dropdown shift — **not** stock maximize/fullscreen chrome-swap | Custom scroll/virtual list; still editor-embedded, not a fake “maximize game” mode |
| **When** | Ship first (V2-1…V2-5) | Only after Track S miss on lag/UX **or** research proves S ceilings are hard |
| **Gate** | `EnablePartsListSlideExpand` / layout / perf | `EnablePartsListRebuild` — **off until go/no-go passes** |

### Track S — preferred first

- **Slide-grow expansion** of the parts list panel (horizontal and/or vertical), same family as shipping `PartsPanelCollapseHelper` dropdown slide.
- Icon size, list style, organizer compatibility, Settings tab.
- Optimize the **stock** list in place: fewer rebuilds, icon cache, defer Refresh, virtualization **if** stock bind path allows.
- **Compose** user expand + dropdown slide via one geometry intent (no fighting restores).

**Not Track S:** stock maximize / F11-style fullscreen swap / replace-the-editor-chrome mode.

### Track R — optional rebuild (go/no-go)

Building a **whole new and better parts list** is on the table if research shows it’s worth it. Treat as:

1. Architecture port always present (`IPartsListRebuildHost`) so design isn’t boxed into S-only.
2. Feature gate **false** until an explicit go decision.
3. Higher QA: filters, CCK, subassemblies, search apply, delete dialogs, input focus.

#### Go/no-go criteria (choose R over continuing S)

| # | Criterion | Signal to prefer R |
|---|-----------|-------------------|
| G1 | **Lag ceiling** | After S geometry + cache + defer (+ attempted in-place virtualization), large catalogs still miss target frame time / time-to-search |
| G2 | **Density ceiling** | Soft reflow cannot achieve desired icon sizes / columns without fighting stock prefabs every KSP patch |
| G3 | **Compose pain** | Dropdown slide + expand + organizers force continual Restore fights that a owned list would avoid |
| G4 | **Research spike** | Phase V2-0 (or dedicated spike) shows stock icon bind is inherently O(n) with no recycle hook |
| G5 | **Product willingness** | Accept higher regression risk and ModTest cycle for a step-change UX |

**No-go (stay on S)** if: S meets lag/UX goals; organizers break badly under R prototypes; or bandwidth should stay on search features instead.

Either track: **Settings tab remains required** for icon size, list style, expand prefs (S), rebuild experimental toggles (R), compatibility, performance.

---

## 1. Vision

| # | Theme | Feel |
|---|--------|------|
| V1 | **Slide-expand / widen the parts list** (Track S default) | More icons; same editor chrome; composes with dropdown |
| V2 | **Icon size** + **list style** | Dense / comfortable / large; grid vs compact vs hybrid |
| V3 | **Organizer compatibility** (CCK, filter extensions) | Coexist or soft-yield |
| V4 | **Reduce lag / time-to-search** | Cache, defer, virtualize (S in-place or R owned) |
| V5 | **Mod options tab** in KSP Settings | First-class UI for either track |
| V6 | **Optional Track R** | New list only after go/no-go — better list if stock can’t deliver |

**Success metric (S):** Preferred expand + search without lag regression; dropdown open/close slides cleanly; organizers work in compatibility mode.  
**Success metric (R, if chosen):** Same search/filter semantics with measurably better scroll/bind performance and density control, without breaking stock category clicks / SA delete.

---

## 2. Non-goals (v1 / 0.9 / current shipping)

| Band | Non-goal |
|------|----------|
| **Shipping core (~0.8.x → v1.0)** | No user slide-expand, no icon-size rewrite, no Settings tab, no list rebuild |
| **~0.9 history delete** | Dropdown chrome only |
| **PostV1 categories / SA / Global halt** | Do not own icon grid |
| **This V2 scaffold** | No GameData deploy; gates false |
| **Track S** | No stock maximize / chrome-swap “fullscreen” |
| **Track R** | Not assumed default; not started before go/no-go |

Hard rule until V2 enabled: **stock parts list remains authoritative** (Track S). Track R would change that only behind an explicit gate after research.

---

## 3. Research notes — stock list + today’s slide

> No KSP/Unity refs in the architecture assembly. Confirm with dnSpy / runtime in V2-0.

### 3.1 Key types

| Type | Role |
|------|------|
| `EditorPartList` | Icon grid owner; `Refresh` / `RefreshSubassemblies` / filters |
| `PartIcon` | Per-part visual; bind cost dominates large catalogs |
| `PartCategorizer` | Categories, search field, custom filters |
| `UIPanelTransition` | In/Out states — used by `PartsPanelCollapseHelper` |
| `PartsPanelCollapseHelper` | **Shipping** dropdown slide/collapse |
| `PartsPanelTransitionGuard` | Prevents stock transitions fighting dropdown |
| `UIPartAction*` | Out of scope |

### 3.2 Rebuild vs geometry

1. Filter/search → predicate → `EditorPartList.Refresh` → bind icons (**Track S optimizes / soft-reflows this**).
2. Panel position via transition / `RectTransform` (**Track S slide-expand extends this** — same family as dropdown).
3. **Track R** replaces (1)’s icon host while still consuming the same predicate/part set where feasible.

### 3.3 Settings patterns

PluginData cfg backend + `ISettingsTabHost` (GameSettings preferred, DialogGUI fallback). Toolbar may mirror expand on/off.

---

## 4. Performance hypotheses

| # | Hypothesis | Track | Risk |
|---|------------|-------|------|
| P1 | In-place **virtualization** if stock recycles | S | May be impossible → feeds G4 for R |
| P2 | **Icon / texture cache** | S or R | Memory |
| P3 | **Defer / coalesce Refresh** | S | One-frame flicker |
| P4 | Density via icon size / style | S or R | Prefab fights on S |
| P5 | Organizer yield / layout-only | S or R | Detection |
| P6 | Geometry-first widen | S | Need icon reflow for new cells |
| P7 | Owned virtual list | R | Ownership / filter / SA races |

---

## 5. Compatibility matrix (organizers)

| Organizer | Track S geometry | Track S icon reflow | Track R owned list |
|-----------|------------------|---------------------|--------------------|
| Stock | OK | Careful | Feasible |
| CCK | Usually OK | Careful; yield tabs | Must consume CCK filters/tabs |
| Filter extensions | Layout-only mode | Risk | Must apply same predicates |
| Custom category UIs | Coexist w/ PostV1 navigate | Same | Same |
| Reskins | Conflict possible | Conflict | Detect; disable or warn |

---

## 6. Settings tab IA (both tracks)

**Host:** KSP Settings → **Koobal Search Engine**.

| Section | Options | Notes |
|---------|---------|--------|
| **Parts List** | Slide-expand enable; width/height amounts; icon size; list style; compatibility; **architecture track** (S default; R experimental if gated) | No “maximize game” toggle |
| **Search** | Future density / halt diagnostics | |
| **History** | Max entries; ~0.9 delete link | |
| **Advanced** | Perf toggles; force S; allow R experimental; organizer override; reset | Go/no-go surfaces here for ModTest |

---

## 7. Phased approach

| Phase | Name | Track | Gate |
|-------|------|-------|------|
| **V2-0** | Research spike (bind cost, recycle hooks, CollapseHelper axes) | S+R decision input | Docs |
| **V2-1** | Settings shell | Shared | `EnableSettingsTab` |
| **V2-2** | Slide-expand geometry + dropdown compose | **S** | `EnablePartsListSlideExpand` |
| **V2-3** | Soft icon reflow (size/style) | **S** | `EnablePartsListLayout` |
| **V2-4** | Organizer bridge | Shared | `EnableOrganizerBridge` |
| **V2-5** | Perf soft (cache, defer, in-place virtualize attempt) | **S** | `EnablePerfSoft` |
| **V2-G** | Go/no-go review vs §0 criteria | Decision | Written decision in changelog |
| **V2-6** | Rebuild host prototype (ModTest only) | **R** | `EnablePartsListRebuild` |

Never skip to V2-6 as the default plan. Never add a stock maximize/chrome-swap phase under either track.

---

## 8. Risks

| Approach | Pros | Cons |
|----------|------|------|
| **Track S geometry** | Same family as shipping dropdown slide; lowest conceptual risk | CollapseHelper restore must learn user-expand rest state |
| **Track S soft reflow** | Density without owning Refresh | Prefab / update fragility |
| **Track R rebuild** | Virtualization + density control; may beat stock ceilings | Replays ownership pain; organizer/filter/SA QA; higher cost |

---

## 9. Open questions

1. Expand width: px steps, % screen, or presets (Normal / Wide / ExtraWide)?
2. Dropdown open + user expand: max / sum / temporary shrink?
3. Taller list: grow down only vs reclaim vertical chrome without hiding categorizer?
4. Icon size for subassemblies too?
5. One `KoobalSettings.cfg` vs split files?
6. GameSettings vs DialogGUI for first Settings ship?
7. Organizer detect without hard assembly refs?
8. After go on R: hide stock list entirely or keep as fallback?
9. Shared predicate adapter so R and S search apply stay identical?
10. Memory budget for icon cache on 5k+ parts?

---

## 10. Architecture map

```
PostV1/V2/
  ModSettings/             ← model, store, cfg, ISettingsTabHost
  PartsListExperience/     ← Track S slide-expand, Track R rebuild port,
                             layout prefs, organizer, virtualization, track enum
  V2Services.cs / V2FeatureGate.cs
  README.md / WIRE_UP.md
```

Namespace: `PartSearchSuggest.PostV1.V2.*` · Phase: `PostV1Phase.E_V2PartsListAndSettings`

---

## 11. Rules when later included

1. Gates off until ModTest per sub-phase.
2. No architecture DLL deploy to GameData.
3. **Track S is default**; Track R only after go/no-go.
4. Track S: slide-expand only — no maximize/chrome-swap.
5. One geometry composer for dropdown + user expand.
6. Settings model is source of truth for both tracks.
7. Do not break stock category clicks, SA delete, or search halt.
