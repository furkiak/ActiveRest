# Changelog

## Unreleased

### Local runtime candidate 0.3.0-preview.2

- Target .NET 10 LTS for app/tests and Windows CI source; select stable .NET 10 SDK feature bands with global.json.
- Replace vulnerable transitive Tmds.DBus.Protocol 0.20.0 with patched 0.21.3; audit the full app/test dependency graph and fail restore on NU1901–NU1904.
- Retain Avalonia 11.2.7, opt-in tracking and existing data/settings formats. Record runtime-specific tests and native package checks independently; preserve prior packages and profiles.

### Earlier appearance candidate 0.3.0-preview.1

- Add Light/Dark and 100/150/200% font preferences stored atomically in the selected profile; legacy settings default to Light/100%, unsupported values/backup files remain preserved.
- Apply appearance resources to open/new windows without changing Windows DPI, activity history or tracking state. Leave an unchanged normal startup preference untouched when saving.
- Use opaque theme surfaces with measured text/control/focus tokens, wrapping actions and scrollable main/settings/report layouts; scale the existing date picker and its flyout resources.
- Remove decorative custom transitions and pressed scaling. Exercise native Light/Dark, actual 100/150/200% fonts, keyboard scrolling, date cancellation, restart persistence and PDF export in a separate Windows profile; device-wide accessibility and release checks remain open.
- Wrap the main title and Settings action at minimum width; give the date flyout and its accept/cancel buttons explicit accessibility names.
- Extend persistence/reload checks to 38 passing tests, preserving the original 26 behavior checks. Prepare the local 0.3.0-preview.1 candidate; no public tag or installer has been published.

- Keep the selected day/week/month/year period visible in the main view, detailed report and PDF, including year boundaries and changed week-start preferences.
- Use numeric GridLength values for the activity bar; fix the incorrect equal thirds for a 100% focus report. Increase PDF secondary-text contrast.
- Verify native separate-profile tracking, Pause/Resume, reminder persistence, JSON export, date selection and weekly PDF saving on Windows 11.

- Support an explicit absolute separate data directory across main tracking, Settings, JSON and report windows; disable Windows startup registration for separate profiles.
- Preserve unreadable/newer history and settings instead of deleting or silently overwriting them, including a damaged backup.
- Test real timer pause/resume snapshots with synthetic activity, disk-reloaded reminders, weekly view totals in two cultures and direct PDF generation.

- Open with tracking off; require an explicit start each launch, and stop tracking when history is deleted.
- Keep a normal taskbar window open after focus changes; replace the template icon, clarify empty reports and use consistent English labels.
- Measure observed elapsed time, split midnight samples and exclude long unobserved gaps.
- Apply reminder settings without restart and make tracking start/stop idempotent.
- Restore damaged history from an atomic-write backup or pause writes when recovery is impossible.
- Add pause, JSON export, confirmed history deletion and a configurable week start.
- Clarify Windows-only scope, data handling, heuristic categories and release limits.

No public release tag has been created for this revision.
