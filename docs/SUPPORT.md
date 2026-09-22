# Support and release gates

| Area | Status for this source revision |
| --- | --- |
| Windows source build | Release build and unit tests run locally. |
| Other desktop OS | Unsupported: Win32 input, audio and startup APIs are used. |
| Timer, day rollover, week boundary, history recovery | Automated tests cover synthetic clocks and files. |
| Native UI, tray, registry startup, PDF | Needs an interactive Windows run after these changes. |
| Published installer or GitHub release | Not yet prepared or published for this revision. |

Before tagging a release, test a clean user profile and a previous-history upgrade, verify a downloaded package hash, and record the exact Windows version. Keep a rollback copy of `%APPDATA%\ActiveRest` before upgrading. If a new build fails, close it, restore the copy and use the previous published package. A public description must match the tested features, not the workflow diagram.
