# Part Search Suggest — v0.7.0 rollback baseline

This folder preserves the **v0.7.0 beta** build for manual rollback.
Do not edit these files in place — copy them to your live `GameData/PartSearchSuggest/` install when restoring.

**Location:** `Source/PartSearchSuggest/rollback/v0.7.0/` (source tree only — **not** under `GameData/`). KSP scans all `*.dll` under `GameData/`; keeping rollback DLLs here prevents accidental double-load.

## v0.7.0 changes (beta milestone)

- **Stock categorizer filter suggestions:** engines, control/RCS, fuel tanks, manufacturer, diameter, category, module, resource, and tech rows with click-to-filter.
- **Compact dropdown header:** smaller banner and X button (v0.6.9 UI polish).
- **Author/mod filtering:** unified attribution, CKAN enrichment, co-author tokenization (v0.6.8 and earlier).
- **Known limitation:** query `intake` matches **Aerodynamics** (`filterAero`) only; clicking an intake-related suggestion may produce a blank parts list. Fixed in v0.7.1a (unverified).

## Restore

See `Source/PartSearchSuggest/ROLLBACK.md` for step-by-step restore instructions for main KSP and ModTest.
