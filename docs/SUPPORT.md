# Support and release gates

| Area | Status for this source revision |
| --- | --- |
| Windows source build | Release build and unit tests run locally. |
| Other desktop OS | Unsupported: Win32 input, audio and startup APIs are used. |
| Timer, day rollover, week boundary, history recovery | Automated tests cover synthetic clocks and files. |
| Separate data directory | Main and Settings share the selected profile. Automated tests cover startup-entry preservation, rejected invalid paths and protected incompatible data. Native tracking, settings persistence, JSON, dates and PDF paths passed locally on Windows 11; newer-format Start/Save refusal preserved the fixture bytes. |
| Real timer and PDF service | Real timer pause/resume with synthetic activity and direct PDF rendering are tested; native Pause/Resume and real foreground activity, native JSON/PDF save pickers also passed in separate profiles. |
| Native main, settings and empty report UI | Visually inspected on Windows 11 on 23 September 2026; tracking remained off and existing history/settings hashes stayed unchanged. |
| Native tracking/settings/date/JSON/PDF | Passed on Windows 11, 27 September 2026; isolated live and synthetic history. No normal history was used. |
| Earlier .NET 9 appearance revision 0.3.0-preview.1 | 38 source/persistence checks and bounded native Light/Dark, actual 100/150/200 fonts, keyboard scrolling, restart, tracking-state and PDF checks passed in a disposable profile. Full device/accessibility coverage remains open; see APPEARANCE.md. |
| .NET 10 candidate 0.3.0-preview.2 | App/tests/CI source target .NET 10, with patched D-Bus and full-graph vulnerability auditing. Clean tests, exact bundled runtime and native final-package checks must be recorded independently of historical .NET 9 evidence. See RUNTIME.md. |
| Full keyboard/scaling, tray, normal registry startup, destructive confirmation | Separate native checks still required before release. |
| Published installer or GitHub release | Not yet prepared or published for this revision. |

Before tagging a release, test a clean user profile and a previous-history upgrade, verify a downloaded package hash, and record the exact Windows version. Keep a rollback copy of `%APPDATA%\ActiveRest` before upgrading. If a new build fails, close it, restore the copy and use the previous published package. A public description must match the tested features, not the workflow diagram.

Follow [appearance acceptance](APPEARANCE.md) in a separate profile. Palette calculations cover application-owned normal states; native popup, disabled and platform states need screen validation. There is no native background blur, so no blur-performance claim is made. Fonts scale independently of DPI; source/build/persistence checks are not native %200 acceptance.
