# ActiveRest

**See where your computer time goes, on your own computer.** ActiveRest is a Windows activity timer built with C#, .NET 10 and Avalonia. Start tracking when you want to measure focused time, audio activity while idle, and rest. Pause whenever you like; review process totals and export JSON or PDF reports.

![ActiveRest main window with tracking off, captured on Windows 11](docs/windows-main-preview.jpg)

*Real Windows capture from 23 September 2026. The current build also offers light/dark themes and larger text; this image shows the earlier layout. The [report capture](docs/windows-report-preview.jpg) likewise shows an empty report before tracking.*

The current **0.3.0-preview.2** source targets .NET 10. Its packaged Windows build passed 38 tests and selected native tracking, appearance, pause, report and restart checks on 27 September 2026. There is no public installer yet; [runtime results and remaining checks](docs/RUNTIME.md) identify the exact package. A [workflow diagram](docs/workflow.svg) explains the three time categories.

## Try it

Requires Windows with a stable .NET 10 SDK for source builds. `global.json` selects stable .NET 10 feature bands starting at 10.0.100. From the repository root:

```powershell
dotnet run --project ActiveRest.csproj
```

The app opens with tracking **off**. Select **Start tracking** in the main window to begin, then use **Pause tracking** or **Resume tracking** as needed. Each launch starts with tracking off, including launches triggered by the optional Windows auto-start setting. **Settings** offers the reminder interval, Monday/Sunday week start, Windows auto-start, **Export JSON**, **Open folder**, and **Delete history**. Deleting history also stops tracking until you start it again. The tray menu can reopen or exit the app. `dotnet test ActiveRest.Tests/ActiveRest.Tests.csproj -c Release` runs the behavior tests.

There is no current public installer for this revision. A source build is the reproducible path until the Windows release package passes a clean-machine UI check. Do not interpret an older ZIP or image as this revision.

## Appearance and text size

Open **Settings → Appearance**, choose **Light/Dark** and **100%/150%/200%**, then **Save Changes**. The preferences are stored atomically in that profile's `settings.json` and apply to open/new windows and the next launch. Text size changes actual font sizes independently of Windows DPI/display scaling. Layouts scroll vertically and wrap actions; the date field can scroll horizontally if its larger three-part control needs more room. Surfaces are opaque: no Acrylic/Mica/background blur is implemented. Decorative custom transitions and pressed scaling are removed.

Source palette calculations meet normal text ≥4.5:1 and control/focus boundaries ≥3:1 in both themes. The complete persistence suite passes 38 tests, including the earlier 26 tracking/profile checks. These checks do not establish native focus visibility or no clipping at 200%; record those separately using [appearance acceptance](docs/APPEARANCE.md). Saving appearance alone leaves Windows startup unchanged; separate profiles cannot modify startup at all. Older settings default to Light/100%; incompatible main/backup settings stay preserved. An older app does not understand these new fields, so copy the entire profile before downgrading.

For a separate disposable or portable profile, pass an absolute data directory:

```powershell
dotnet run --project ActiveRest.csproj -- --data-directory E:\ActiveRest-TestProfile
# Or: ActiveRest.exe --data-directory E:\ActiveRest-TestProfile
```

This profile uses its own history and settings without copying the normal `%APPDATA%\ActiveRest` files. The main, Settings and report titles identify the separate profile; Settings displays its actual folder. Windows startup is disabled for it, and saving its settings cannot change the normal Run registry entry. The argument must be complete and absolute, cannot overlap the normal data directory, and cannot use linked files/folders. Invalid arguments stop startup rather than falling back to personal data. These are separate data directories, not separate OS users or a security sandbox.

## How the numbers are calculated

| Display | Current rule |
| --- | --- |
| Focused work | Input idle time is under 180 seconds. The foreground **process name** receives the observed duration. |
| Comfortable working | Input has been idle for at least 180 seconds while the default output device's audio peak is above a threshold. This is an audio activity heuristic, not proof that media is playing. |
| Rest | Input has been idle for at least 180 seconds and the audio heuristic is false. |

Elapsed time between ordinary samples is measured rather than assuming every timer callback represents exactly one second. Unobserved gaps longer than five seconds, including sleep and long stalls, are **excluded**, so totals can be lower than wall-clock time. A sample crossing local midnight is split between days; the previous day is saved first. The weekly view uses Monday by default, with a Sunday preference. A reminder is based on consecutive observed focus time and does not diagnose or prevent eye strain.

## Privacy and data control

ActiveRest runs locally and has no account, analytics service, or upload path in this source. It reads the foreground process name, Windows input idle duration and output-device audio peak. It does **not** read window titles, keystrokes, screenshots, microphone input, or audio content. Process names can still reveal personal behavior; treat exported files as private.

After you start tracking, daily duration totals and process names are kept in `%APPDATA%\ActiveRest\stats.json`; the previous valid write is in `stats.json.bak`. Reminder, week, theme and text-size settings are in `settings.json`. Data remains until you delete it. The app saves about once per observed minute and on pause, day change, and exit while tracking. Merely opening and closing the app does not create a new history record. Settings → **Export JSON** copies the recorded history to a file you choose. Settings → **Delete history** stops tracking and removes the main history and backup after an in-app confirmation. Exported copies and older `.corrupt-*` recovery files are separate files; delete those from **Open folder** if you no longer need them. Normal filesystem deletion is not secure erasure on SSDs.

If the main history file is malformed, ActiveRest tries the last valid backup and preserves the damaged copy with a `.corrupt-*` suffix. If neither copy is readable, tracking pauses and the app leaves both files untouched for manual recovery. A valid JSON file with unrecognized fields/format or duplicate days is also preserved; it is not silently replaced by an older backup. Delete history cannot discard unreadable history or a damaged backup. Damaged or unsupported settings show safe defaults and a recovery message; saving cannot overwrite the original. Copy, repair or move those files manually, then restart the application. See [data recovery](docs/DATA.md).

## PDF reports and limits

The **Detailed Report** view exports a PDF to a location you choose. The report summarizes the selected date range and process totals; it is not a health, performance or productivity score. The app uses Windows-specific activity and startup APIs and an approximate audio rule. The foreground process total covers active samples only; it does not prove which document or browser tab was used. Native date selection, Monday/Sunday week totals and PDF saving were exercised on Windows 11 in a separate synthetic profile. Day/week/month/year reports identify the chosen period. The measured-time bar now uses numeric widths; a 100% focus fixture fills it rather than showing three equal segments.

## Development

```powershell
dotnet restore ActiveRest.Tests/ActiveRest.Tests.csproj
dotnet test ActiveRest.Tests/ActiveRest.Tests.csproj -c Release
dotnet publish ActiveRest.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

The source tree includes [tracking tests](ActiveRest.Tests/TrackingTests.cs) for midnight, sleep gaps, pause/resume, opt-in startup, settings reloaded from disk, week boundaries, backup recovery and deletion. [Profile/runtime tests](ActiveRest.Tests/RuntimeProfileTests.cs) check separate storage, the normal startup-entry guard, newer/damaged data preservation, a real timer with synthetic activity, culture-independent weekly view totals and direct PDF rendering. Those checks do not substitute for native UI interaction. The main, settings and empty-report screens were visually inspected on Windows; isolated native tracking/settings/export/date flows also passed locally. Normal registry startup, tray, destructive UI confirmation and clean-machine checks remain release gates. See [native acceptance steps](docs/NATIVE_PROFILE_CHECK.md), [support and release checks](docs/SUPPORT.md), [contributing](CONTRIBUTING.md), and [changelog](CHANGELOG.md). Licensed under [MIT](LICENSE).

## Türkçe

**Ayarlar → Appearance** bölümünde açık/koyu tema ile **%100 / %150 / %200 yazı boyutunu** seçip **Save Changes** ile kaydedebilirsin. Bu gerçek font boyutunu değiştirir; Windows DPI veya ekran ölçeğini değiştirmez. Tercihler yalnız seçilen profilin ayar dosyasında atomik olarak saklanır. Yüzeyler opaktır; blur uygulanmaz, kozmetik geçişler kaldırılmıştır. Büyük metinde kaydırma ve satır sarma vardır; geniş tarih alanı yatay kaydırılabilir. Önceki .NET 9 görünümünün geniş tema/font/klavye matrisi tarihsel kayıttır. `1e60969` .NET 10 kesin ZIP'inde 38 temiz klon testi ve seçili Dark/%200 Start, takipte Light/%200 Save, Pause/sabit sayaç, eşleşen hafta/rapor/PDF ve takip kapalı yeniden açılış 27 Eylül 2026'da geçti. Bu belge güncellemesi yeni derleme değildir; paket hash'i ve native manifest [runtime belgesindedir](docs/RUNTIME.md). Farklı DPI, ekran okuyucu, tam yeni-runtime hata/görünüm matrisi, hatırlatıcı, silme onayı, fiziksel uyku/gece yarısı ve temiz makine kabulü açıktır. Eski ayarlar açık tema/%100 ile açılır; bozuk veya uyumsuz ana/backup dosyaları değiştirilmez. Eski sürüme dönmeden önce profilin tamamını yedekle.

`--data-directory E:\ActiveRest-TestProfile` ile ayrı bir deneme profili açabilirsin: geçmiş ve ayarlar yalnız seçilen klasörde kalır, kişisel veriler kopyalanmaz ve Windows başlangıç kaydı değişmez. Pencere başlığı ayrı profili belirtir. Yol tam olmalı ve normal veri klasörüyle çakışmamalıdır. Okunamayan veya yeni sürüme ait veri/ayarlar sessizce silinmez; klasörden kopyalayarak kurtarıp uygulamayı yeniden açmalısın. Ayrı profilde gerçek takip/duraklat/devam, ayar kaydı, tarih seçimi, hafta toplamı, JSON ve PDF dışa aktarma kontrol edildi; sistem başlangıcı, tepsi ve temiz makine kurulumu ayrı yayın kapılarıdır.

ActiveRest, Windows üzerinde çalışan **yerel bir etkinlik zamanlayıcısıdır**. Gözlenen etkin süreyi, ses çıkışı varken boşta geçen süreyi ve mola süresini gösterir. Ön plandaki **işlem adını** kaydeder; pencere başlığı, tuş vuruşu, ekran görüntüsü, mikrofon veya ses içeriği toplamaz. Varsayılan veri konumu `%APPDATA%\ActiveRest` klasörüdür. Uygulama her açılışta takip kapalı başlar; ana ekrandaki **Start tracking** düğmesine basınca ölçüm başlar. Takibi duraklatabilir, Ayarlar’dan JSON dışa aktarabilir veya geçmişi onaylayarak silebilirsin. Geçmişi silmek takibi de durdurur. Bu sürüm sağlık ya da verimlilik sonucu vaat etmez. Ana ekran, ayarlar, gerçek takip/duraklat/devam, seçilen hafta ve PDF gerçek Windows oturumunda ayrı test verileriyle kontrol edildi. Rapor başlığı tarih aralığını gösterir; yüzde çubuğu süreleri doğru oranlar. Tepsi, normal Windows başlangıcı ve temiz makine kurulumu yayın öncesi açıktır.
