# .NET 10 local candidate

The first working-tree Release run on 27 September 2026 passed **38/38 tests**, zero failures/skips, on .NET 10. This includes actual local-file persistence/recovery and timer/PDF-service behavior with synthetic inputs; clean-clone, dependency-guard and final-package native results are separate delivery checks.

**0.3.0-preview.2** targets `net10.0` for the application and tests. The Windows CI source requests .NET 10. `global.json` permits stable .NET 10 feature bands starting at 10.0.100, excluding preview SDKs and .NET 11. Avalonia 11.2.7, data/settings formats and opt-in tracking behavior remain unchanged.

Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), checked 27 September 2026, lists .NET 9 support ending 10 November 2026 and .NET 10 LTS ending 14 November 2028. Self-contained packages still need reviewed runtime patch updates; the LTS label does not keep an older ZIP current.

The earlier NuGet graph reported vulnerable **Tmds.DBus.Protocol 0.20.0**, brought in by Avalonia, against the maintainer's [GHSA-xrw6-gwf8-vvr9](https://github.com/tmds/Tmds.DBus/security/advisories/GHSA-xrw6-gwf8-vvr9). Version **0.21.3** backports the security fix and is selected directly. Restore audits direct and transitive packages for app/tests and treats NU1901–NU1904 as errors. An audit can only report known advisories from the available feed; it is not exploit reproduction, unknown-flaw proof or Linux compatibility verification. ActiveRest remains Windows-only.

```powershell
dotnet --version
dotnet list ActiveRest.csproj package --vulnerable --include-transitive
dotnet test ActiveRest.Tests/ActiveRest.Tests.csproj -c Release --logger 'trx;LogFileName=net10.trx' --results-directory TestResults\net10
dotnet publish ActiveRest.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o TestResults\publish-net10
```

Repeat from a clean Git clone, record test skips/warnings and inspect `ActiveRest.runtimeconfig.json` for the exact bundled runtime patch. Preserve the prior ZIP and the entire profile before any upgrade or downgrade. For native checks use an absolute disposable `--data-directory` with synthetic history only: verify paused startup, explicit Start/Pause, appearance Save while tracking, stable totals while paused, period/report rendering and restart with tracking off. Compare the known normal-profile file hashes and startup entry digest before/after. Do not copy personal history into a test package.

The .NET 9 native records in APPEARANCE.md and NATIVE_VALIDATION.md remain historical. A successful restore/build/audit does not substitute for the new runtime's native acceptance. Physical sleep/midnight, reminder timing, native deletion confirmation, tray/normal startup, full screen-reader/DPI, another account and clean-machine install remain separate gates. No new public release or remote CI run has occurred.

## Türkçe

**0.3.0-preview.2** .NET 10 LTS'ye geçer; Avalonia, veri biçimi ve açıkça başlatılan takip davranışı korunur. Bilinen D-Bus bağımlılık açığı **0.21.3** ile giderilir; uyarı bastırılmaz. Doğrudan/dolaylı NuGet açıkları restore hatasıdır. Temiz klon testleri, paketteki kesin runtime ve ayrı profilde gerçek uygulama kontrolleri tarihli kaydedilmelidir. Eski .NET 9 ekran/test kayıtları yeni runtime kabulü sayılmaz. Normal geçmişi kopyalama; önceki paketi ve profil yedeğini koru. Temiz makine, fiziksel uyku/gece yarısı ve yayın kapıları açıktır.
