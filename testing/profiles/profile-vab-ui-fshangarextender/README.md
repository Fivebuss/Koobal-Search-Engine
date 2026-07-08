# Profile — Hangar Extender (main install)



**Exclusive group:** `vab-ui-organizer` — test alone, not with VABOrganizer/PartCatalog.



CKAN id: `HangerExtenderExtended` → GameData folder `FShangarExtender`.



## Required dependencies



FShangarExtender is junctioned from the main install; **deps are junctioned via `sharedDeps`** (same pitfall as Hangar without AT-Utils).

| Folder | CKAN id | Required for |
|--------|---------|--------------|
| `001_ToolbarControl` | `ToolbarController` | **HangerExtenderExtended.dll load** — hard dependency |
| `000_ClickThroughBlocker` | `ClickThroughBlocker` | ToolbarController dependency |



Also present on every ModTest profile: `000_Harmony`, `CommunityCategoryKit` (base junction), `KoobalSearchEngine`.



## Known profile pitfall (fixed)



If only `FShangarExtender` is junctioned without `ToolbarController`, KSP.log shows:



```

AssemblyLoader: Assembly 'FShangarExtender' has not met dependency 'ToolbarController' V1.0.0

AssemblyLoader: Assembly 'FShangarExtender' is missing 1 dependencies

```



Symptoms: no extend/shrink toolbar buttons in VAB/SPH; parts catalog panel does not resize.



## Apply



```powershell

.\apply-profile.ps1 -Profile profile-vab-ui-fshangarextender

```



Re-apply after profile changes so `sharedDeps` junctions `001_ToolbarControl` and `000_ClickThroughBlocker` from main.



## Test focus



1. **Koobal Search Engine** — search dropdown, categorizer, collapse (primary mod matrix goal)

2. **Hangar Extender** — only after deps present: toolbar extend/shrink icons in VAB, panel resize works, dropdown not clipped by layout change



If Hangar Extender still fails after deps are installed, treat as mod issue — not Koobal Search Engine — and note INCONCLUSIVE for extender-specific behavior.

