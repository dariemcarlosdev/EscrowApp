# QF-18 — WDAC blocks the unsigned apphost `.exe` ("Access is denied" on launch)

**Layer**: Environment / Setup — `EscrowApp`
**Feature**: Run / debug (F5, `dotnet run`, direct `.exe`)
**Severity**: High (app won't launch from the built executable)
**Status**: ✅ Fixed
**Date**: 2026-07-29

---

## Summary

Launching `EscrowApp` failed with a Windows block:

```
An error occurred trying to start process
'...\EscrowApp\bin\Debug\net10.0\EscrowApp.exe' ... Access is denied.
```

`dotnet run` inherits the failure because its launch profile (`commandName: "Project"`) starts the app through the apphost `EscrowApp.exe`. Right-click → **Properties → Unblock** does **not** help — there is no Mark-of-the-Web to clear.

---

## Root Cause

**WDAC (Windows Defender Application Control)** — a corporate code-integrity policy on this Enterprise-managed machine — denies execution of **unsigned** binaries from user-writable paths. The .NET build emits an unsigned **apphost** (`EscrowApp.exe`), which WDAC blocks.

```powershell
Get-CimInstance Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard
#   CodeIntegrityPolicyEnforcementStatus = 2   # 0=Off, 1=Audit, 2=Enforced
```

`dotnet.exe` itself is Microsoft-signed and allowed — only the app-generated apphost is blocked.

> Same root cause and fix as SmartMenuOptimizer BUG-009 (`WDAC-BLOCKS-APPHOST-EXE`), same machine.

---

## Fix Applied

Stop emitting the apphost. Both CLI and the built output then run through the signed `dotnet.exe` host over the managed `.dll`.

`EscrowApp/EscrowApp.csproj` `PropertyGroup`:

```xml
<!-- WDAC (corporate code-integrity policy) blocks the unsigned apphost .exe. Launch via signed dotnet.exe host instead. -->
<UseAppHost>false</UseAppHost>
```

Clean the stale exe + rebuild:

```bash
rm -f EscrowApp/bin/Debug/net10.0/EscrowApp.exe
dotnet build EscrowApp/EscrowApp.csproj
```

With `UseAppHost=false` the build emits **no** `.exe`; `dotnet run` and F5 fall back to `dotnet EscrowApp.dll`.

---

## Verification

```
dotnet build EscrowApp/EscrowApp.csproj  -> 0 Errors
ls EscrowApp/bin/Debug/net10.0/*.exe     -> no exe (good)
dotnet EscrowApp/bin/Debug/net10.0/EscrowApp.dll  -> launches (WDAC no longer denies)
```

After the fix the process starts and reaches startup DB seeding — confirming the apphost block is gone. (A separate local-Postgres credential issue may surface next; that is unrelated to WDAC.)

---

## Prevention / Notes

- **Do not** rely on Properties → Unblock — it only clears Mark-of-the-Web, not WDAC. Check `CodeIntegrityPolicyEnforcementStatus` to confirm WDAC.
- Self-contained / standalone publish still emits an apphost and will be blocked. Use framework-dependent publish (`dotnet EscrowApp.dll`) or get the binary signed.
- Permanently allowing unsigned exes requires IT to amend the WDAC policy — outside developer control.

Env: Windows 11 Enterprise (WDAC Enforced), .NET 10.
