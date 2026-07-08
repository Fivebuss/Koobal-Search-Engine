# Profile 03 — Main install sample ~50

Larger compatibility slice (~50 part mods + shared deps). See `profile.json` for full folder list.

## Apply

```powershell
.\apply-profile.ps1 -Profile profile-03-main-sample50
```

Enable `dumpIndexStats` in DebugSettings.cfg — index build will be noticeably slower.

## Exit criteria

- All TEST_PROTOCOL queries pass
- `parse-test-log.ps1` exit code 0
- No HarmonyException in KSP.log
