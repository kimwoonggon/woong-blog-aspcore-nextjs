# CI Verification Report — 2026-05-04

## Scope
Verify current GitHub Actions promotion chain evidence on branch `dev` for repository `kimwoonggon/woong-blog-aspcore-nextjs`.

## Requested Items
- Check current CI/promotion chain evidence.
- Find latest relevant runs:
  - CI Dev on `dev`
  - Promote Main Runtime run(s) triggered by CI Dev
  - `release/main-promote -> main` PR state
  - CI Main Runtime success
- Provide exact `gh` commands and observed outputs.
- Determine whether the chain is currently succeeding end-to-end.

## What was changed
- No production, test, or workflow code was changed.
- Added audit-only artifacts:
  - `backend/reports/ci-verification-2026-05-04/ci-verification-2026-05-04.md`
  - `backend/reports/ci-verification-2026-05-04/ci-verification-2026-05-04.html`
  - `backend/reports/ci-verification-2026-05-04/ci-verification-2026-05-04.json`

## What was intentionally not changed
- No workflow files.
- No application source/test files.
- No branch/push operations.

## Exact `gh` commands and observed evidence

### 1) CI Dev on dev
```bash
gh run list --workflow "CI Dev" --branch dev --limit 3 --json number,status,conclusion,url,createdAt,updatedAt,headSha,databaseId
```
Result (latest):
- Run ID `25322888983` (`number:109`)
- Branch `dev`
- Head SHA `1fd143f35bb5a6a4a67f46b7ad94f9ecafa4befe`
- Status `completed`
- Conclusion `success`
- URL `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25322888983`
- Created `2026-05-04T13:50:55Z`

### 2) Promote Main Runtime runs triggered by CI Dev
```bash
gh run list --workflow "Promote Main Runtime" --limit 6 --json number,status,conclusion,event,url,headBranch,headSha,databaseId
```
Result (latest `workflow_run`):
- Run ID `25323185860` (`number:12`)
- `event: workflow_run`
- Branch `main`
- Status `completed`
- Conclusion `success`
- URL `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25323185860`
- Created `2026-05-04T13:56:48Z`

### 3) PR creation evidence from promotion run
```bash
gh run view 25323185860 --log | rg -n "SOURCE_BRANCH|SOURCE_REF|Created promotion PR #"
```
Result excerpts:
- `SOURCE_BRANCH: dev`
- `SOURCE_REF: 1fd143f35bb5a6a4a67f46b7ad94f9ecafa4befe`
- `Created promotion PR #35`
- `Enabled auto-merge for promotion PR #35`

### 4) `release/main-promote -> main` PR state
```bash
gh pr list --base main --head release/main-promote --state open --json number,title,state,url,mergeable,createdAt,author

gh pr list --base main --head release/main-promote --limit 5 --state all --json number,title,state,url,mergedAt,closedAt,mergedBy,createdAt,headRefName
```
Result:
- Open PRs: none (`[]`).
- Latest PR on that path: `#35`.
  - URL: `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/pull/35`
  - State: `MERGED`
  - Closed/Merged: `2026-05-04T14:03:11Z`

### 5) CI Main Runtime success
```bash
gh run list --workflow "CI Main Runtime" --limit 8 --json number,event,status,conclusion,url,headBranch,headSha,createdAt,databaseId,displayTitle
```
Result (latest relevant chain):
- PR-check run: `25323196924` (`event: pull_request`, head `release/main-promote`, status `completed`, conclusion `success`, URL `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25323196924`)
- Merge push run: `25323508573` (`event: push`, head `main`, status `completed`, conclusion `success`, URL `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25323508573`)

## Validation against requested chain
- `dev` CI: **success** (`25322888983`).
- `Promote Main Runtime` triggered via `workflow_run`: **exists and success** (`25323185860`).
- Promotion PR `release/main-promote -> main` created/auto-merged: **created (#35), merged (14:03:11Z)**, no open PR now.
- PR-level `CI Main Runtime` check: **success** (`25323196924`).
- Post-merge `CI Main Runtime` on `main`: **success** (`25323508573`).

## Final assessment
The current chain is fully succeeding: `CI Dev` success on `dev` (25322888983) propagated to `Promote Main Runtime` via `workflow_run` (25323185860), which created/auto-merged PR #35, followed by successful PR checks and successful push checks on `main`.

## Risks / yellow flags / follow-up
- Time correlation in workflow telemetry should be interpreted in UTC.
- This report captures the latest successful chain only; it does not re-run jobs.

## Recommendation / next step
Continue with current automation: monitor for future deviations by checking for a new `CI Dev` run and ensure each has a corresponding `Promote Main Runtime` `event: workflow_run` plus success on both PR and merge `CI Main Runtime` checks.
