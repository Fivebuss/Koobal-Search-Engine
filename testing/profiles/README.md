# ModTest profiles — build tracking

Record which **Koobal Search Engine user label** (v0.7.8.0x) each profile was validated against. Instance-specific conflict fixes bump the **letter suffix** only — not the minor version — until v0.8.0.0 thumbnails.

## SemVer mapping (testing phase)

| User label | csproj `<Version>` | KSP `.version` |
|------------|-------------------|----------------|
| v0.7.8.0a | `0.7.8.1` | `0.7.8` |
| v0.7.8.0b | `0.7.8.2` | `0.7.8` |
| v0.7.8.0c | `0.7.8.3` | `0.7.8` |
| v0.7.9.0 | `0.7.9.0` | `0.7.9` | Deferred indexing optimization |

Full lineage: `Source/PartSearchSuggest/ROLLBACK.md`.

## Profile test matrix

**Dependency policy:** Every profile apply runs CKAN dependency resolution + preflight before first launch. See `TEST_RUN_WORKFLOW.md` § CKAN policy.

| Profile | Purpose | Tested against | Notes |
|---------|---------|----------------|-------|
| `profile-00-baseline` | Harmony2 + PSS + CCK base layer | v0.7.8.0a | Baseline gate before VAB-UI profiles |
| `profile-vab-ui-communitycategorykit` | CCK-focused validation | v0.7.8.1b | CCK now on all profiles; this profile adds explicit CCK notes |
| `profile-vab-ui-kspcommunityfixes` | FasterEditorPartList | — | CCK + KSPCF |
| `profile-vab-ui-hangar` | Hangar editor UI | — | |
| `profile-vab-ui-fshangarextender` | FShangarExtender layout | — | |
| `profile-vaborganizer` | VABOrganizer (CKAN-only) | — | |
| `profile-01-main-top10` | Top-10 parts mods from main | — | |
| `profile-02-main-top25` | Top-25 parts mods | — | |
| `profile-03-main-sample50` | ~50-mod stress + index dump | — | Enable `dumpIndexStats` |
| `profile-main-full` | Full main install mirror (~130 mods) | — | ModTest parity; definitive test on main KSP |
| `profile-05-legacy` | Legacy / unmaintained mods | — | As needed |

**Column `Tested against`:** fill with the user label (e.g. `v0.7.8.0a`) after a passing session per `TEST_PROTOCOL.md`. If a profile fails and requires a code fix, ship **v0.7.8.0b** (or next letter), re-test failed profiles, then update this table.

## Planned suffix assignments (reference)

| Label | Intended fix / milestone |
|-------|--------------------------|
| v0.7.8.0a | Index dump + testing framework baseline (current) |
| v0.7.8.0b | profile-vab-ui-communitycategorykit conflict fix |
| v0.7.8.0c | Next instance-specific fix |
| v0.8.0.0 | Thumbnails experiment track (bumps `.version` to `0.8.0`) |
