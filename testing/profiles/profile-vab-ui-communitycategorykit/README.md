# Profile — Community Category Kit (main install organizer)

**Exclusive group:** `vab-ui-organizer`

This is the user's **primary category/filter system** on the main install (`CommunityCategoryKit/CCK.dll`). Test this **first** among VAB-UI profiles.

## Apply

```powershell
.\apply-profile.ps1 -Profile profile-vab-ui-communitycategorykit
```

## CKAN

```powershell
Tools\ckan.exe install --instance KSP-ModTest --headless CommunityCategoryKit
```

Junction from main avoids re-download; CKAN verifies dependencies.

## VAB-organizer checks

See TEST_PROTOCOL.md § VAB-UI organizer checklist.
