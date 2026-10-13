# Changelog

## 2.0.0

A full review of the code base: bugs fixed, the architecture reworked, new features and a new design.

### Bugs fixed

- **Weekly statistics were wrong on Sundays.** The start of the week was computed as the *next* Monday, so the weekly view showed an empty week every Sunday.
- **History could be lost.** `stats.json` was rewritten non-atomically; a crash mid-write left a damaged file, which was then silently treated as "no data" and overwritten by the next save. Writes are now atomic with a rolling backup, and a damaged file is restored from the backup or quarantined as `stats.corrupt-*.json`.
- **Possible crash from a data race.** The tracker mutated the application dictionary from a timer thread while the UI enumerated it, and timer callbacks could overlap. All tracking now runs single-threaded.
- **Skewed totals.** Every tick added exactly one second regardless of how long it really took. Elapsed time now comes from the wall clock and gaps caused by sleep, hibernation or clock changes are not counted.
- **Settings needed a restart.** The eye-rest interval was read once at start-up. Settings are now one shared object that notifies the rest of the app.
- **The reminder stole keyboard focus** from the window the user was typing in, and also hid the main window. It now opens without activating.
- **Resource waste.** A `Process` object was created and never disposed every second, the audio device was re-enumerated every second, and the whole statistics file was re-read and re-written every minute. Process names are cached, the device is reused and the history is cached in memory.
- **Midnight rollover lost up to a minute** of the previous day because it switched days without saving first; the day is now saved before switching.
- **Interface text.** "Moon" instead of "Month", a Turkish "Tarih Filtresi" label in the English UI, a garbled tray tooltip, `%62` percentages in English and `g/s/d` duration units.
- **Date picker reset the view mode.** Picking a date always switched back to the day view; the date now applies to the selected period.
- **Auto-start** used a hard-coded executable path and opened the full window at every login; it now uses the real process path, starts quietly in the tray and repairs its entry when the executable is moved or replaced.
- **CSV formula injection.** Cells starting with `-`, a tab or a carriage return were not neutralised; every OWASP trigger character is now covered.
- **Closing the window ended the application.** It now hides to the tray; only *Quit* exits.
- **Several instances** could run at once, double-counting time and fighting over the data file. A single-instance guard prevents it.
- **PDF export** froze the UI while it rendered, labelled every report with today's date whatever period was shown, depended on the view model and reported errors only to the console. It now runs in the background, is titled with the selected period and shows the result.

### Architecture

- Models, services, view models and views are separated; a composition root builds the services.
- `TrackerEngine` has no timer or UI dependencies and is fully unit-tested with a fake clock and tracker.
- `StatsAggregator` / `ReportBuilder` are pure functions; exporters depend on a report model, not on a view model.
- Platform abstraction for the tracker (`WindowsActivityTracker`, `NullActivityTracker`), `[SupportedOSPlatform]` guards instead of warnings.
- Runtime localization (EN/TR) with a `{loc:T Key}` markup extension, theme tokens instead of hard-coded colours, shared icon geometry.
- 50 unit tests, a headless UI smoke test, CI, a solution file and a `.gitignore`.
- A release workflow: pushing a `v*` tag publishes a self-contained single-file executable (nothing to install) and a small framework-dependent one, with checksums.
- Updated Avalonia to 11.3, CommunityToolkit.Mvvm to 8.4 and QuestPDF to 2026.9; reduced NAudio to the Wasapi package.

### New

- Liquid Glass interface in light and dark mode.
- Daily focus goal with an activity ring, day streak, hourly/daily/monthly trend chart and highlights (peak hour, longest focus, top app, daily average).
- Day / Week / Month / Year navigation with previous/next, a calendar picker and "back to today".
- 20-20-20 reminder with countdown, snooze and dismiss; a real break resets the timer.
- Pause/resume tracking, pin the window open, tray menu, keyboard shortcuts and show/hide animation.
- PDF reports for the selected period and CSV export, saved through a file dialog.
- Settings: language, theme, goal, idle threshold, eye-rest interval; all applied live.
- Friendly application names, colour-coded usage bars.
- New application icon.
