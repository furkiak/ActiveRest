# Data and recovery

ActiveRest stores `stats.json`, `stats.json.bak` and `settings.json` in `%APPDATA%\ActiveRest`. Each daily record includes a local date, first-run timestamp, three duration totals and per-process duration totals. The program samples input idle time and output audio peak to choose a category, but does not persist the raw idle or audio sample. No retention period is enforced; the user controls export and deletion.

Writes use a temporary file in the same directory, flush it, then replace the main file while retaining the previous version as `.bak`. On a damaged main file, the backup is validated before recovery. The malformed file is renamed `.corrupt-YYYYMMDD-HHMMSSfff`. A backup can be up to one save interval older than the main file. If both copies fail validation, no new history is written and the app shows a recovery notice. Copy both files before manual repair.

The in-app **Delete history** action deletes `stats.json` and `stats.json.bak`, then resets the live counters. It intentionally leaves any prior `.corrupt-*` recovery files and user-created exports for manual review. Those files may also contain process names. Filesystem deletion is not guaranteed secure erasure.
