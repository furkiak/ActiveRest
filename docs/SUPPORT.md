# Support and release gates

| Area | Status for this source revision |
| --- | --- |
| Windows source build | Release build and unit tests run locally. |
| Other desktop OS | Unsupported: Win32 input, audio and startup APIs are used. |
| Timer, day rollover, week boundary, history recovery | Automated tests cover synthetic clocks and files. |
| Separate data directory | Main and Settings share the selected profile. Automated tests cover startup-entry preservation, rejected invalid paths and protected incompatible data. Native acceptance is pending. |
| Real timer and PDF service | Real timer pause/resume with synthetic activity and direct PDF rendering are tested; native controls/pickers and real foreground activity remain separate acceptance checks. |
| Native main, settings and empty report UI | Visually inspected on Windows 11 on 23 September 2026; tracking remained off and existing history/settings hashes stayed unchanged. |
| Tracking interaction, keyboard, tray, registry startup, PDF | Still need interactive Windows checks with isolated data before release. |
| Published installer or GitHub release | Not yet prepared or published for this revision. |

Before tagging a release, test a clean user profile and a previous-history upgrade, verify a downloaded package hash, and record the exact Windows version. Keep a rollback copy of `%APPDATA%\ActiveRest` before upgrading. If a new build fails, close it, restore the copy and use the previous published package. A public description must match the tested features, not the workflow diagram.
