# Koobal Search Engine — Test Run Workflow

Three-step process the agent runs **before each ModTest profile session**. The user launches VAB and executes the checklist; the agent automates apply + briefing.

**Automation entry point:**

```powershell
cd "F:\SteamLibrary\steamapps\common\Kerbal Space Program\Source\PartSearchSuggest\testing"
.\apply-profile.ps1 -Profile <profile-id>
```

`apply-profile.ps1` performs all three steps and prints output to the terminal (parent agent relays summary to user).

---

## Step 1 — Report what's loaded

After applying the profile, the script scans `ModTest/GameData` and prints:

| Field | Source |
|-------|--------|
| Profile id + name | `profiles/<id>/profile.json` |
| Koobal Search Engine version | `GameData/KoobalSearchEngine/*.version` |
| Harmony version | `GameData/000_Harmony/*.version` |
| Per-mod table | Folder name, display name from `.version`, version, source |

**Source column:**

| Value | Meaning |
|-------|---------|
| `junction` | Directory junction → main install (no duplicate download) |
| `copy/ckan` | Folder exists on main; may be CKAN-managed copy |
| `ckan/local` | Installed by CKAN or local only on ModTest |

Protected folders always present: `Squad`, `000_Harmony`, `KoobalSearchEngine`.

**Permanent base layer (all profiles):** `CommunityCategoryKit` — junction from main install unless profile sets `"skipBaseCck": true`. CCK only affects filter tabs when other mods register categories; it is included so categorizer/filter suggestions match the user's main install.

---

## Step 2 — Pre-run conflict check

Runs **before** telling the user to launch KSP. Output: **GREEN / YELLOW / RED**.

### RED (do not launch)

| Check | Condition |
|-------|-----------|
| Missing Harmony | `000_Harmony/0Harmony.dll` absent |
| Missing Koobal Search Engine | `KoobalSearchEngine/Plugins/KoobalSearchEngine.dll` absent |
| Missing CCK base layer | `CommunityCategoryKit/CCK.dll` absent (YELLOW unless `skipBaseCck`) |
| Missing CKAN dependency | Hard dep folder for a profile mod absent (e.g. `000_AT_Utils` for Hangar, `001_ToolbarControl` for FShangarExtender) |
| Multiple organizers | More than one of: CommunityCategoryKit, VABOrganizer, PartCatalog, CategoryParts, EditorExtensions |
| Broken junction | Junction target unreachable |

### YELLOW (launch with caution)

| Check | Condition |
|-------|-----------|
| Expected organizer missing | `exclusiveGroup: vab-ui-organizer` profile but no organizer DLL found |
| Multiple editor-surface mods | KSPCommunityFixes + Hangar + CCK etc. in same profile |
| CKAN warnings | `ckan list` reports incompatible/conflicting modules |
| Dry-run mode | Checks skipped |

### GREEN

No RED issues; warnings only if informational.

### Editor-surface mods tracked

These patch or alter `PartCategorizer` / editor part list (P0 conflict surface):

- `KSPCommunityFixes` (FasterEditorPartList)
- `CommunityCategoryKit`
- `VABOrganizer`
- `Hangar`
- `HangerExtenderExtended` (folder: `FShangarExtender`)

---

## Step 3 — Test briefing

Per-profile subset from `TEST_PROTOCOL.md`. Printed automatically; defaults in `apply-profile.ps1` (`Get-DefaultBriefing`), overridable via `testBriefing` in `profile.json`.

### Always include (VAB load gate)

1. Launch ModTest → VAB
2. Confirm log lines: `Editor scene detected`, `Hooked native editor search field`, `Indexed N`
3. Wait ≥3 s before typing
4. No `HarmonyException`

### Query subsets by profile

| Profile | Queries | VAB-UI checks | ~Time |
|---------|---------|:-------------:|------:|
| `profile-00-baseline` | engine, intake, harmony, a | no | 5 min |
| `profile-vab-ui-communitycategorykit` | engine, intake, a, nertea, near future | **yes** | 10 min |
| `profile-vab-ui-kspcommunityfixes` | engine, intake, harmony, a | **yes** | 8 min |
| `profile-vab-ui-hangar` | engine, intake, a | **yes** | 8 min |
| `profile-vab-ui-fshangarextender` | engine, intake, a | **yes** | 8 min |
| `profile-vaborganizer` | engine, intake, a | **yes** | 10 min |
| `profile-01-main-top10` | engine, intake, lis, harmony, a, author, mod | no | 10 min |
| `profile-02-main-top25` | engine, intake, harmony, a, nertea, rockomax | no | 15 min |
| `profile-03-main-sample50` | same as -02 | no | 15 min |

### VAB-UI checks (when organizer in profile)

From `TEST_PROTOCOL.md` § VAB-UI organizer checklist (V1–V8):

- Dropdown visible, scrollable, not clipped
- Catalog collapse (`UIPanelTransition`) still works
- Categorizer suggestion → parts list **not blank**
- Organizer sidebar/tabs survive filter apply

### After session

```powershell
.\parse-test-log.ps1 -LogPath "F:\SteamLibrary\steamapps\common\Kerbal Space Program - ModTest\KSP.log"
```

Exit **0** = automated pass. User marks manual checklist in `TEST_PROTOCOL.md` session record.

---

## Mandatory test order

1. `profile-00-baseline`
2. VAB-UI profiles — **one at a time** (never two organizers):
   - `profile-vab-ui-communitycategorykit` ← user's main install organizer
   - `profile-vab-ui-kspcommunityfixes`
   - `profile-vab-ui-hangar`
   - `profile-vab-ui-fshangarextender`
   - `profile-vaborganizer` (CKAN-only)
3. Parts sweeps: `profile-01-main-top10` → `profile-02` → `profile-03`

---

## CKAN policy

**Rule: CKAN resolves deps — use it.** Every profile apply must satisfy the full CKAN dependency tree before the first KSP launch.

### Dependency resolution (apply-profile.ps1)

1. **Map junction folders → CKAN ids** using main-install `CKAN/registry.json`, global `ckanIdByFolder` overrides (e.g. `FShangarExtender` → `HangerExtenderExtended`), and profile `ckanIdByFolder` when needed.
2. **Resolve the full dependency tree** for all profile mods (primaries, junctioned content, and explicit `ckanInstall`).
3. **Junction from main first** — primary mods and all resolved deps whose GameData folders exist on the main install.
4. **CKAN install the remainder** — `ckan install --instance KSP-ModTest --headless <mods>` fills any folders still missing (CKAN auto-resolves nested deps).
5. **Preflight before launch** — conflict check verifies every hard CKAN dependency folder is present in ModTest GameData. **RED = do not launch.**

### Never do this

- **Never junction a mod from main without also resolving its CKAN dependency tree.**
- **Never list a CKAN id in `ckanPrimary` when the GameData folder name differs** (e.g. do not use `HangerExtenderExtended` in `ckanPrimary` when junctioning `FShangarExtender` — use `ckanIdByFolder` instead).
- **Never skip preflight** — if Step 2 is RED, fix deps before telling the user to launch VAB.

### Profile.json fields

| Field | Purpose |
|-------|---------|
| `ckanPrimary` | CKAN ids to install/resolve (when folder name matches id) |
| `ckanInstall` | Extra CKAN ids (legacy/explicit; prefer auto-resolve) |
| `junctionFromMain` / `junctionFolders` | GameData folders to junction from main |
| `sharedDeps` | Extra folders to junction (optional; auto-resolve covers CKAN deps) |
| `ckanIdByFolder` | Profile-specific folder → CKAN id map for name mismatches |
| `skipCkanResolve` | Set `true` only when debugging — disables auto dependency tree |

### CKAN CLI notes

- `ckan install --instance KSP-ModTest --headless <primaryMod>` — **default dependency resolution** (do not use `--no-recommends` unless debugging)
- Junction from main when folder exists; CKAN fills missing deps
- CKAN metadata = source of truth for KSP version compatibility
- `registry.locked` = CKAN GUI open — preflight shows YELLOW, non-blocking
- **Never** modify ModTest `CKAN/GUIConfig.json`

---

## Agent checklist (each profile cycle)

```
[ ] Run apply-profile.ps1 -Profile <id>
[ ] Confirm CKAN dependency tree was resolved (script prints resolved ids)
[ ] Relay Step 1 loaded-mods table to user
[ ] Relay Step 2 GREEN/YELLOW/RED — block launch if RED (includes missing CKAN deps)
[ ] Relay Step 3 test briefing (queries + VAB-UI if applicable)
[ ] User tests VAB (~5-15 min)
[ ] Run parse-test-log.ps1
[ ] Record session in TEST_PROTOCOL.md template
```

---

## Optional: index dump (v0.7.8+)

Enable before heavy profiles:

```
GameData/KoobalSearchEngine/PluginData/DebugSettings.cfg
  dumpIndexStats = true
```

Log will contain `[Koobal] IndexStats:` lines.

---

## Version bumps during mod testing (v0.7.8.0x)

While validating ModTest profiles (before v0.8.0.0 thumbnails), **instance-specific conflict fixes** use letter suffixes — not minor version bumps:

| Action | Version change |
|--------|----------------|
| First mod-testing release (index dump + framework) | **v0.7.8.0a** → csproj `0.7.8.1` |
| Fix for one profile (e.g. CCK conflict) | **v0.7.8.0b** → csproj `0.7.8.2` |
| Next profile-specific fix | **v0.7.8.0c** → csproj `0.7.8.3` |
| Thumbnails experiment track starts | **v0.8.0.0** → csproj `0.8.0.x`, `.version` → `0.8.0` |

Update on each suffix bump: csproj `<Version>`, README changelog (user label), `ROLLBACK.md`, `testing/profiles/README.md` (tested-against column). KSP `.version` stays **`0.7.8`** until v0.8.0.0.

Full mapping: `Source/PartSearchSuggest/ROLLBACK.md`.
