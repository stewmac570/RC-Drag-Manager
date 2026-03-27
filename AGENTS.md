\# AGENTS.md — RC Drag Manager



\## Project context

Personal C# .NET WinForms application for managing RC drag racing.

Core domains: run logging, timing data, telemetry, vehicle/tune management.

Solo developer. Reviews are for catching real problems, not style policing.



\## Review priorities (in order)



\### 1. Logic \& correctness

\- Flag any calculation errors in timing, speed, or performance derived values

\- Check boundary conditions — lap/run counters, elapsed time, index bounds

\- Flag any off-by-one errors in run sequences or data arrays

\- Verify state transitions are correct (e.g. run in progress → complete → saved)



\### 2. Data integrity

\- Flag any path where run/log data could be silently lost or overwritten

\- Check that file writes are atomic or at minimum fail loudly — no quiet corruption

\- Flag missing null checks before writing telemetry or log entries

\- Ensure timestamps and run identifiers are applied before data is persisted



\### 3. Performance

\- Flag any UI thread blocking — data loads, file I/O, or calculations should not run on the UI thread

\- Flag unnecessary re-reads of data that could be cached

\- Watch for tight loops over large telemetry datasets without early exits or batching



\### 4. Code structure \& naming

\- Flag ambiguous variable names in calculation or timing logic — clarity matters here

\- Flag methods doing more than one job — especially in data processing paths

\- Suggest extraction when a method exceeds \~50 lines and has mixed concerns



\### 5. Security

\- Flag any file paths constructed from user input without sanitisation

\- Flag hardcoded paths that assume a specific machine (e.g. C:\\Users\\Stew\\...)

\- Flag any external data loaded without validation



\## What NOT to flag

\- Minor formatting or whitespace

\- Preference-based naming (e.g. abbreviations the author clearly uses consistently)

\- Missing XML doc comments on private methods

\- Warnings on third-party or generated code



\## Review style

\- Be direct and specific — point to the line and explain the risk

\- If a fix is obvious, suggest it inline

\- Group related issues rather than repeating the same comment multiple times

\- Do not summarise what the code does — only flag what's wrong or risky

