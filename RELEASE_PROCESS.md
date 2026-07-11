# Koobal Search Engine — Release Process (MANDATORY)

**Why this exists:** A verified build was once lost because "rollbacks" existed only as
prose in `ROLLBACK.md` with no real, restorable artifact. That cost money. Never again.
Every release from now on MUST have **two independent, restorable copies**: a git tag and
an on-disk player zip inside the repo.

## Rules for EVERY future release (no exceptions)

For any version you ship (even a tiny fix), do ALL of the following:

1. **Bump versions** in `PartSearchSuggest.csproj` (`<Version>`) and the KSP `.version`
   file, and update the README changelog + `ROLLBACK.md` version lineage table.
2. **Build clean** (0 errors). Verify the built DLL reports the expected assembly version.
   - NOTE: the csproj `OutputPath` points at the **main install**
     (`..\..\GameData\KoobalSearchEngine\Plugins\`). If you must build without touching the
     main install, redirect output: `dotnet build -c Release -o <tempdir>`.
3. **Commit** the source with a clear message (local working tree under this folder).
4. **Annotated git tag**: `git tag -a vX.Y.Z.B -m "short description"`.
   (Annotated — not lightweight — so the tag carries a message + date.)
5. **Drop a built player zip in `ReleaseArchive/`** named
   `KoobalSearchEngine_vX.Y.Z.B_<LABEL>.zip` containing the CKAN-style payload
   `GameData/KoobalSearchEngine/` (DLL, `.version`, README, **LICENSE**,
   `PluginData/BrandingSettings.cfg`). LICENSE is mandatory in every player package.
   Branding PNGs are unused in-game (programmatic wordmark) and live in `BrandingAssets/`
   as essential project assets — not shipped in the player zip. This `ReleaseArchive/`
   player zip is the redundant, git-independent on-disk copy.
6. **Also drop the player handoff zip** into the Desktop KSE folder:
   `C:\Users\timbr\Desktop\KSE\`
   e.g. `KoobalSearchEngine_vX.Y.Z.B.zip`.
   Do **not** leave full-Koobal zips on the Desktop root.
7. **Do NOT** create local `*_SOURCE.zip` / `-with-source` courtesy archives, and do
   **not** attach SOURCE zips to GitHub Releases. Source-of-truth is this working tree;
   push/update the GitHub **git repository** only when the user asks.
8. **Keep necessary assets only**: LICENSE, production `.cs`, csproj, `.version`,
   shipping PluginData configs, and BrandingAssets needed to rebuild wordmarks/textures.
   Do not keep duplicate unpacked SOURCE trees or bulk source archives locally.

> A release is NOT done until BOTH the annotated tag AND the `ReleaseArchive/` **player**
> zip exist. No version may ever be "only-prose" again.

## Quick command reference

```powershell
$src = "F:\SteamLibrary\steamapps\common\Kerbal Space Program\Source\PartSearchSuggest"
cd $src

# after building + updating version files:
git add -A
git commit --trailer "Co-authored-by: Cursor <cursoragent@cursor.com>" -m "vX.Y.Z.B: <summary>"
git tag -a vX.Y.Z.B -m "<short description>"

# archive the built PLAYER package only (build/package your zip first):
$kse = "C:\Users\timbr\Desktop\KSE"
New-Item -ItemType Directory -Path $kse -Force | Out-Null
Copy-Item "<path to built player package zip>" "$src\ReleaseArchive\KoobalSearchEngine_vX.Y.Z.B_<LABEL>.zip" -Force
Copy-Item "<path to player zip>" "$kse\KoobalSearchEngine_vX.Y.Z.B.zip" -Force
# (no *_SOURCE.zip / -with-source copies)
```

## Restoring a release

Either method fully restores a version:

- **From zip (fastest):** unzip `ReleaseArchive\KoobalSearchEngine_vX.Y.Z.B_*.zip` so its
  `GameData\KoobalSearchEngine\` merges into your KSP `GameData\`.
- **From git:** `git checkout vX.Y.Z.B` then rebuild (see build note above).

See `ROLLBACK.md` for the current verified-safe baseline and per-version details.
