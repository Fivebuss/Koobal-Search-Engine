# Profile — VABOrganizer only

**Exclusive group:** `vab-ui-organizer` — never install two organizer mods in the same ModTest pass.

VABOrganizer is **not** on the main install. Installed via CKAN; one junctioned parts mod (`NearFuturePropulsion`) supplies VABO subcategory patches.

## Apply

```powershell
.\apply-profile.ps1 -Profile profile-vaborganizer
```

Equivalent CKAN command:

```powershell
Tools\ckan.exe install --instance KSP-ModTest --headless VABOrganizer
```

CKAN auto-installs: ModuleManager, Harmony2 (may skip if 000_Harmony present).

## Test order

Run after `profile-00-baseline` and `profile-vab-ui-communitycategorykit` (user's main category system).

## Optional matrix pass

To test PartCatalog or CategoryParts (not on main install), create a **separate** profile and apply with a clean GameData wipe — never alongside VABOrganizer.
