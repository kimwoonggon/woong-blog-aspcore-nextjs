# Next Plan After CI - Real Backend Target DB Attribution

## Trigger
Use this plan after the `dev` CI, GHCR dev publish, main runtime promotion, and GHCR main publish are green for the target DB attribution slice.

## Plan
1. Confirm the deployed backend image contains `X-Db-Command-Elapsed-Ms` and `X-Db-Command-Count` headers on public API responses.
2. Run production preflight before load testing, including nginx timing, app timing, DB diagnostics, cgroup CPU/memory, and public Work contract checks.
3. Run Real Backend Test with real public Work/Study read URLs, `pageSize=12`, no seed target override, no cache optimization.
4. Interpret target rows using `P95`, `DB P95`, `DB Cmds`, `Payload P95`, and `Receive P95`.
5. Choose the next implementation slice from evidence:
   - If `DB P95` is low but target `P95` is high, prioritize public detail payload/serialization/app CPU reduction.
   - If `DB P95` is high, prioritize EF projection, indexes, and roundtrip reduction.
   - If `DB Cmds` is greater than 2 on detail requests, prioritize detail query consolidation.
   - If `Payload P95` or `Receive P95` is high, prioritize public body/DTO slimming.
6. Next likely slice if metrics confirm app/payload pressure: public detail projection and stored thumbnail equivalence guard, proving stored public thumbnail/cover fields match legacy resolver output while avoiding request-time body JSON scans.

## Required Record After CI
Append the CI run IDs, publish run IDs, production preflight command/result, and Real Backend Test run ID to this file or to the audit report before starting the next code slice.
