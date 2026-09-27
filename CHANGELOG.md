# Changelog

## Unreleased

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
