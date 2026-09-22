# Contributing

Please open an issue describing the observed behavior, Windows version, expected result and reproduction steps. Do not attach your real `stats.json` or exported reports; they contain process names. A synthetic, minimal example is preferred.

For code changes, branch from `main`, keep a focused diff and run:

```powershell
dotnet test ActiveRest.Tests/ActiveRest.Tests.csproj -c Release
```

UI changes also need a native Windows check: start/pause/resume, Settings save, Sunday/Monday week switch, export/delete confirmation, tray open/exit, and PDF. Document which paths were actually exercised. Keep health and productivity claims out of documentation unless supported by evidence.
