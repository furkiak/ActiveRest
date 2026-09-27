# .NET 10 local candidate

The first working-tree Release run on 27 September 2026 passed **38/38 tests**, zero failures/skips, on .NET 10. This includes actual local-file persistence/recovery and timer/PDF-service behavior with synthetic inputs. The same day's subsequent clean-clone, dependency-guard and bounded final-package native results are recorded below.

**0.3.0-preview.2** targets `net10.0` for the application and tests. The Windows CI source requests .NET 10. `global.json` permits stable .NET 10 feature bands starting at 10.0.100, excluding preview SDKs and .NET 11. Avalonia 11.2.7, data/settings formats and opt-in tracking behavior remain unchanged.

Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), checked 27 September 2026, lists .NET 9 support ending 10 November 2026 and .NET 10 LTS ending 14 November 2028. Self-contained packages still need reviewed runtime patch updates; the LTS label does not keep an older ZIP current.

The earlier NuGet graph reported vulnerable **Tmds.DBus.Protocol 0.20.0**, brought in by Avalonia, against the maintainer's [GHSA-xrw6-gwf8-vvr9](https://github.com/tmds/Tmds.DBus/security/advisories/GHSA-xrw6-gwf8-vvr9). Version **0.21.3** backports the security fix and is selected directly. Restore audits direct and transitive packages for app/tests and treats NU1901–NU1904 as errors. An audit can only report known advisories from the available feed; it is not exploit reproduction, unknown-flaw proof or Linux compatibility verification. ActiveRest remains Windows-only.

```powershell
dotnet --version
dotnet list ActiveRest.csproj package --vulnerable --include-transitive
dotnet test ActiveRest.Tests/ActiveRest.Tests.csproj -c Release --logger 'trx;LogFileName=net10.trx' --results-directory TestResults\net10
dotnet publish ActiveRest.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o TestResults\publish-net10
```

For a future package, repeat from a clean Git clone, record test skips/warnings and inspect `ActiveRest.runtimeconfig.json` for the exact bundled runtime patch. Preserve the prior ZIP and the entire profile before any upgrade or downgrade. For native checks use an absolute disposable `--data-directory` with synthetic history only: verify paused startup, explicit Start/Pause, appearance Save while tracking, stable totals while paused, period/report rendering and restart with tracking off. Compare the known normal-profile file hashes and startup entry digest before/after. Do not copy personal history into a test package.

## Dated final-package validation

On 27 September 2026 a clean clone of source commit
`1e6096976cbf0aa673c635bbe9cc322a0aa5a36d` passed **38/38 tests**, zero failures
or skips, and warning-free self-contained Windows x64 publish with runtime
**10.0.12**. App/test direct-and-transitive audits reported no known vulnerable
packages. Restoring the old D-Bus dependency made restore fail with NU1903; the
source was restored before the successful checks.

The accepted package is `ActiveRest-0.3.0-preview.2-1e60969-win-x64-complete.zip`
(58,323,465 bytes), SHA-256
`80203D615E4195BD1DFA9906A0007FFD6F2809B14FE63DE377CC987AB6B55677`.
The exact ZIP opened with tracking off in an owned synthetic profile at Dark/200.
Explicit Start, Light/200 Settings Save while tracking, Pause and a 39-second
fixed-counter/history checkpoint passed. Main/report week totals matched, and the
native saved one-page PDF was rendered and read with matching periods/durations.
Restart retained Light/200, tracking off and byte-identical paused history. Both
owned test processes ended; the four known normal-profile file hashes and Run
entry digest stayed unchanged. No personal history was copied.

The dated validation records are retained with the local release candidate,
outside this source repository:

- `ActiveRest-package-1e60969-net10.json` binds the code commit, ZIP, clean-test
  and publish proof to the native result.
- `Runtime-net10-2026-09-27\ActiveRest-1e60969\native-result.json` records the
  exact-package observations and remaining boundaries.

The results above apply to the identified code commit and ZIP; later changes to
documentation do not constitute another runtime validation. Selected Dark/200 and Light/200 observations do not
establish the full new-runtime appearance/error matrix. The wider `66c48f5`
.NET 9 font/minimum-window/date-popup evidence remains historical.

The .NET 9 native records in APPEARANCE.md and NATIVE_VALIDATION.md remain historical. A successful restore/build/audit does not substitute for the new runtime's native acceptance. Physical sleep/midnight, reminder timing, native deletion confirmation, tray/normal startup, full screen-reader/DPI, another account and clean-machine install remain separate gates. No new public release or remote CI run has occurred.

## Türkçe

**0.3.0-preview.2** .NET 10 LTS'ye geçer; Avalonia, veri biçimi ve açıkça başlatılan takip davranışı korunur. Bilinen D-Bus bağımlılık açığı **0.21.3** ile giderilir; uyarı bastırılmaz. Doğrudan/dolaylı NuGet açıkları restore hatasıdır. `1e60969` temiz klonda 38/38 test, sıfır atlama/uyarı ve runtime 10.0.12 içeren kesin ZIP yukarıdaki hash ile kaydedildi. Aynı ZIP'in seçili Start/görünüm kaydı/Pause, hafta/PDF ve takip kapalı yeniden açılış kontrolleri 27 Eylül 2026'da geçti. Bu belge commit'i yeni derleme/paket kabulü değildir; eski .NET 9 geniş matrisi tarihsel kalır. Normal geçmişi kopyalama; önceki paketi ve profil yedeğini koru. Tam görünüm/hata/DPI/ekran okuyucu, hatırlatıcı/silme, fiziksel uyku/gece yarısı, tepsi/startup, başka hesap/temiz makine, uzak CI, push ve yayın kapıları açıktır. ActiveRest Windows-only kapsamını korur.
