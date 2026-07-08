# Profile main-full — Full main install mirror

Junctions **all** third-party `GameData` folders from the main KSP install (~130 mods). Use for ModTest parity checks before/alongside the definitive stress run on the **main KSP instance**.

## Apply (ModTest)

```powershell
cd Source\PartSearchSuggest\testing
.\apply-profile.ps1 -Profile profile-main-full
```

First apply may take several minutes (CKAN dependency resolution for the full mod tree).

## Definitive stress test (main install)

Koobal v0.7.9.0+ is deployed to `GameData/KoobalSearchEngine/` on the main install. Launch **main KSP** (not ModTest) for the true ~150-mod profile.

## Expected index timing (v0.7.9.1 save-load indexing)

| Phase | Log line | When | Expected (~1700 parts) |
|-------|----------|------|------------------------|
| Save load (basic) | `[Koobal] Save-load index complete (basic)` | Loading screen after save select | ~1–3 s — acceptable lag |
| Save load (full) | `[Koobal] Save-load index complete (full)` | Same loading screen / early KSC | ~8–15 s total |
| VAB entry | `[Koobal] Search ready (basic)` | VAB/SPH open | **Near-instant** if save-load finished |
| VAB entry (full) | `[Koobal] Search ready (full)` | VAB/SPH open | Immediate if save-load finished; else placeholder until done |

**Main menu:** `[Koobal] Main menu — search indexes cleared` only — no indexing.

Search bar placeholder shows `Loading suggestions…` only if metadata/categorizer still building at VAB entry.

## Queries to run

`engine`, `intake`, `harmony`, `a`, `nertea`, `near future`, `rockomax`, `bluedog`

## Exit criteria

- `[Koobal] Search ready (basic)` before first search interaction
- `[Koobal] Search ready (full)` within acceptable lag (~15 s on main)
- All TEST_PROTOCOL queries pass
- `parse-test-log.ps1` exit code 0
