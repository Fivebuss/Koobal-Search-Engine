# Koobal Search Engine — VAB Test Protocol

Manual checklist for ModTest profile validation (v0.7.8 → v0.9.0).

**Instance:** `F:\SteamLibrary\steamapps\common\Kerbal Space Program - ModTest`  
**Log file:** `{ModTest}\KSP.log`  
**Parser:** `testing/parse-test-log.ps1`  
**CKAN CLI:** `{ModTest}\Tools\ckan.exe` — **source of truth** for dependencies and KSP version compatibility.

---

## Before each session

| Step | Done |
|------|:----:|
| Apply profile: `.\apply-profile.ps1 -Profile <id>` (prints loaded mods, conflict check, test briefing) | |
| Sync DLL after build: `robocopy GameData\KoobalSearchEngine "..\Kerbal Space Program - ModTest\GameData\KoobalSearchEngine" /MIR /XF *.pdb` | |
| Delete or archive prior `KSP.log` for clean parse | |
| (Optional) Enable index dump: `PluginData/DebugSettings.cfg` → `dumpIndexStats = true` | |
| Note profile id + Koobal Search Engine version in session record | |

### CKAN rules

- **CKAN resolves deps — use it.** `apply-profile.ps1` resolves the full dependency tree before first launch.
- **Never junction a mod from main without also resolving its CKAN dependency tree** (script does this automatically from main-install registry + `ckan show` fallback).
- **Preflight before launch:** Step 2 conflict check must not be RED (missing CKAN dep folders block launch).
- Profiles use `ckan install --instance KSP-ModTest --headless` with **automatic dependency resolution** (do not pass `--no-recommends` unless debugging).
- Junction from main when GameData folders exist on main; CKAN fills any deps not on main.
- Use `ckanIdByFolder` in profile.json when CKAN id differs from GameData folder name (e.g. `FShangarExtender` → `HangerExtenderExtended`).
- **Never modify** ModTest `CKAN/GUIConfig.json` or global CKAN settings.

### VAB-UI organizer rule (critical)

**Never install two inventory/organizer mods in the same ModTest profile.** These are mutually exclusive in real installs:

- CommunityCategoryKit
- VABOrganizer
- PartCatalog
- CategoryParts
- EditorExtensions

Apply **one** `profile-vab-ui-*` or `profile-vaborganizer` at a time. Wipe third-party GameData between organizer passes (`apply-profile.ps1` does this by default).

---

## Profile matrix (test order)

| Order | Profile | Purpose |
|------:|---------|---------|
| 1 | `profile-00-baseline` | Squad + Harmony + Koobal Search Engine |
| 2 | `profile-vab-ui-communitycategorykit` | **User's main organizer** (CCK on main install) |
| 3 | `profile-vab-ui-kspcommunityfixes` | FasterEditorPartList / editor patches |
| 4 | `profile-vab-ui-hangar` | Editor storage overlay |
| 5 | `profile-vab-ui-fshangarextender` | Panel layout extender |
| 6 | `profile-vaborganizer` | VABOrganizer (CKAN only — not on main) |
| 7+ | `profile-01-main-top10` … | Parts-mod sweeps **after** VAB-UI passes |

Optional CKAN-only matrix (one profile each, not on main): PartCatalog, CategoryParts, EditorExtensions.

---

## VAB load gate

| Check | Pass | Fail | Notes |
|-------|:----:|:----:|-------|
| Game reaches VAB without crash | | | |
| Log: `[Koobal] Editor scene detected` | | | |
| Log: `[Koobal] Hooked native editor search field` | | | |
| Log: `[Koobal] Indexed N editor-available parts` (N > 0) | | | |
| Wait ≥3 s after hook before typing | | | |
| No `HarmonyException` in log | | | |

---

## Query checklist (all profiles)

| # | Query | Expected | Pass | Fail | Notes |
|---|-------|----------|:----:|:----:|-------|
| 1 | `engine` | Dropdown with categorizer and/or part rows | | | |
| 2 | `intake` | Categorizer rows; parts if indexed | | | |
| 3 | `lis` | Rows only if Lisias mods in profile | | | |
| 4 | `harmony` | No part rows; empty or history only | | | |
| 5 | `a` | Dropdown scrolls; no spurious suite-A mod row | | | |
| 6 | *(author)* | Partial author → `author · N parts` row | | | |
| 7 | *(mod)* | Partial mod name → mod row | | | |
| 8 | Click part row | Single part; log `ApplyPrecisePart` | | | |
| 9 | Click author row | Author filter; log `ApplyModAuthorFilter` | | | |
| 10 | Click categorizer row | Category filter; log `ApplyCategorizerFilter` | | | |
| 11 | Enter then re-click bar | History/suggestions; no stuck overlay | | | |
| 12 | Escape / dim overlay | Dropdown dismisses; catalog visible | | | |

---

## VAB-UI organizer checklist (`profile-vab-ui-*`, `profile-vaborganizer`)

Run **in addition** to the query checklist. These are P0 compatibility risks.

| # | Check | Pass | Fail | Notes |
|---|-------|:----:|:----:|-------|
| V1 | Dropdown appears above search bar (not clipped/hidden) | | | |
| V2 | Dropdown scroll works with many suggestions | | | |
| V3 | Parts catalog collapse/slide (`UIPanelTransition`) after dropdown open | | | |
| V4 | Click categorizer suggestion → parts list **not blank** | | | |
| V5 | Organizer sidebar/tabs still visible after filter apply | | | |
| V6 | Re-open search after filter → dropdown still functional | | | |
| V7 | `profile-vaborganizer`: VABO subcategory tabs render | | | |
| V8 | `profile-vab-ui-communitycategorykit`: custom CCK tabs still switchable | | | |

---

## KSP.log — what to grep

**Required:**

```
[Koobal] Editor scene detected
[Koobal] Indexed
[Koobal] Hooked native editor search field
```

**Index dump (v0.7.8+, when `dumpIndexStats = true`):**

```
[Koobal] IndexStats:
```

**Failure signals:**

```
[Koobal] ERROR
HarmonyException
NullReferenceException
Categorizer match 'intake': no rows indexed
matched=0
```

---

## Automated log parse

```powershell
cd "F:\SteamLibrary\steamapps\common\Kerbal Space Program\Source\PartSearchSuggest\testing"
.\parse-test-log.ps1 -LogPath "F:\SteamLibrary\steamapps\common\Kerbal Space Program - ModTest\KSP.log"
```

Exit **0** = pass. Non-zero = review before marking profile pass.

---

## Promotion gates (v0.7.8 → v0.9.0)

| Gate | Requirement |
|------|-------------|
| v0.7.8 | profile-00 + profile-vab-ui-communitycategorykit pass; index dump verified |
| v0.8.x | All VAB-UI profiles pass; profile-01 pass |
| v0.9.0 | profile-02/03 pass OR main-install smoke; no open blockers |

---

## Session record template

```
Date:
Profile:
Koobal Search Engine version:
KSP version:
VAB-UI checks passed: /8
Queries passed: /12
parse-test-log exit code:
Blockers:
```
