# Submitting Koobal Search Engine to CKAN

Local drafts live in this folder. **You** publish the GitHub Release and open the NetKAN PR — nothing has been submitted upstream yet.

Official guides:

- [Adding a mod to the CKAN](https://github.com/KSP-CKAN/CKAN/wiki/Adding-a-mod-to-the-CKAN)
- [NetKAN repo](https://github.com/KSP-CKAN/NetKAN)
- Optional web helper: https://ksp-ckan.github.io/metadata-webtool

## Recommended hosting path

**GitHub Releases** (already have a public repo). SpaceDock is optional later (nice for discoverability + a second `$kref` document), not required for first indexing.

Do **not** rely on zips committed into the repo root — NetKAN’s `#/ckan/github/...` indexer only consumes **Release assets**.

## Current readiness snapshot (2026-07-11)

| Item | Status |
|------|--------|
| GameData root folder `KoobalSearchEngine/` | OK |
| AVC `.version` (KSP 1.12.0–1.12.99, version 0.8.5.1) | OK |
| MIT `LICENSE` in GameData | OK |
| README in GameData | OK |
| Harmony dependency CKAN id | **`Harmony2`** (confirmed; NetKAN file is frozen but still the live identifier) |
| Identifier `KoobalSearchEngine` free on NetKAN / CKAN-meta | OK (not present) |
| GitHub **Releases** with downloadable assets | **BLOCKER — none published** |
| Local git remote → GitHub | **BLOCKER — local `PartSearchSuggest` has no `git remote`** |
| Remote tags on GitHub | **BLOCKER — none** (tags exist only locally: `v0.8.5.1`, `v0.8.5.1-beta`, …) |
| Player + SOURCE Release assets (local) | **Ready** — `KoobalSearchEngine_v0.8.5.1.zip` (GameData only) + `KoobalSearchEngine_v0.8.5.1_SOURCE.zip` on `Desktop\KSE\` / `ReleaseArchive` |
| Package labeled `-beta` / Pre-release | Soft blocker — prefer a **non-prerelease** Release for first index, or enable `x_netkan_github.prereleases: true` |

## Blockers and how to fix

### 1. No GitHub Release (hard)

Repo currently has zips in the tree (`KoobalSearchEngine_v0.8.5.1-beta.zip` on `main`) but **zero Releases**. NetKAN will not index those.

**Fix:** Create a Release, attach a zip asset.

### 2. Local repo not linked / tags not on GitHub (hard)

`Source/PartSearchSuggest` has annotated tags locally but **no remotes**, and GitHub has **no tags**.

**Fix (example):**

```powershell
cd "F:\SteamLibrary\steamapps\common\Kerbal Space Program\Source\PartSearchSuggest"
git remote add origin https://github.com/timbrwolf1121-cpu/Koobal-Search-Engine.git
# Push the branch you want as the canonical source tree, then:
git push -u origin master:main   # or push main if you rename
git push origin v0.8.5.1
```

### 3. Upload both Release assets; CKAN uses the player zip only

Attach **both** zips to the GitHub Release:

| Asset | Role |
|-------|------|
| `KoobalSearchEngine_v0.8.5.1.zip` | **Player / CKAN install** — `GameData/KoobalSearchEngine/` only |
| `KoobalSearchEngine_v0.8.5.1_SOURCE.zip` | Legal/community sources — **not** installed by CKAN |

NetKAN `$kref` `asset_match` selects the player zip and **skips** any asset whose name contains `_SOURCE`. Do not upload a combined forum-style zip as the CKAN asset.

### 4. Beta / prerelease naming (soft)

`.version` reports **0.8.5.1** (good). README/package say **v0.8.5.1-beta**.

For first CKAN listing:

- Prefer GitHub Release tag **`v0.8.5.1`**, **not** marked Pre-release, asset without `-beta` in the filename if possible.
- If you must ship a Pre-release, uncomment in the `.netkan`:

```yaml
x_netkan_github:
  prereleases: true
```

### 5. No forum thread (optional)

`resources.homepage` currently points at GitHub. If you open a KSP forum thread later, update homepage to that URL (CKAN reviewers like a support thread).

## Checklist — what you do

1. **Wire git remote** and push source + tag `v0.8.5.1` to GitHub (see above).
2. **Release assets are already built** (`Desktop\KSE\` + `ReleaseArchive`):
   - Player: `C:\Users\timbr\Desktop\KSE\KoobalSearchEngine_v0.8.5.1.zip` (GameData only — **this is what CKAN indexes**)
   - Source: `C:\Users\timbr\Desktop\KSE\KoobalSearchEngine_v0.8.5.1_SOURCE.zip` (upload for legal/community; skipped by `asset_match`)

3. **GitHub → Releases → Draft a new release**
   - Tag: `v0.8.5.1` (existing annotated tag)
   - Title: e.g. `Koobal Search Engine 0.8.5.1`
   - Attach **both** zips above
   - Leave **Pre-release unchecked** for first CKAN index
4. **Open a PR** on [KSP-CKAN/NetKAN](https://github.com/KSP-CKAN/NetKAN):
   - Path: `NetKAN/KoobalSearchEngine.netkan`
   - Contents: copy from `CKAN/KoobalSearchEngine.netkan` in this repo
5. **PR body** (paste/adapt):

   ```markdown
   ## Add Koobal Search Engine

   - **Identifier:** KoobalSearchEngine
   - **Author:** timbrwolf1121 (I am the author)
   - **License:** MIT
   - **Depends:** Harmony2 (`000_Harmony`)
   - **KSP:** 1.12.x via KSP-AVC `.version` (`$vref`)
   - **Host:** GitHub Releases — https://github.com/timbrwolf1121-cpu/Koobal-Search-Engine
   - **Install:** `find: KoobalSearchEngine` → `GameData`
   - **Notes:** Predictive suggestions for the stock VAB/SPH search bar. Player zip is GameData-only; any `_SOURCE` asset is excluded via asset_match.
   ```

6. Wait for NetKAN CI validation (green checks). A CKAN team member merges; the bot then indexes the Release into CKAN-meta.
7. After merge, refresh CKAN / wait for the next bot pass; search for **Koobal Search Engine**.

### Easier alternatives (if you do not want to hand-write NetKAN)

- Open a [CKAN “add mod” issue](https://github.com/KSP-CKAN/CKAN/issues/new/choose) with the Release URL, or
- Use https://ksp-ckan.github.io/metadata-webtool and let the team finish the file.

## Files created locally

| Path | Purpose |
|------|---------|
| `Source/PartSearchSuggest/CKAN/KoobalSearchEngine.netkan` | Draft metadata for the NetKAN PR |
| `Source/PartSearchSuggest/CKAN/CKAN_SUBMIT.md` | This checklist |

## Out of scope

KoobalNativeSearch / Native Search standalone — not part of this submission.
