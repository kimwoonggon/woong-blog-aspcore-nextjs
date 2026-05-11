# Main Runtime Setup Doc Allowlist Audit - 2026-05-11

## Goal

Ensure the server setup document that explains the corrected `main` image pull workflow is included when the runtime tree is promoted from `dev` to `main`.

## Problem

`docs/walkthroughs/main-server-setup.md` existed on `origin/dev` but not on `origin/main`. The main runtime promotion script only copies paths listed in `scripts/main-runtime-allowlist.txt`, and the setup document path was missing from that allowlist.

## Changes

- Added `docs/walkthroughs/main-server-setup.md` to `scripts/main-runtime-allowlist.txt`.
- Updated `todolist-2026-05-11.md` with the doc-promotion correction, checks, backups, and report paths.

## Intentionally Not Changed

- No production SSH access or remote command execution.
- No Docker image build or runtime code change.
- No secret or `.env` value changes.

## Validation

- Confirmed `origin/main` did not contain `docs/walkthroughs/main-server-setup.md` before this fix.
- Confirmed `origin/dev` did contain `docs/walkthroughs/main-server-setup.md` before this fix.
- Confirmed `scripts/main-runtime-allowlist.txt` now includes `docs/walkthroughs/main-server-setup.md`.
- Ran `git diff --check` for the changed files.
- Parsed this JSON audit companion file successfully.

## Risks

- This is a promotion-scope correction. It becomes complete only after dev CI passes, the promotion PR updates, main checks pass, and `origin/main` contains the setup document.

## Recommendation

Merge this into `dev`, let the automatic runtime promotion carry the setup document to `main`, then verify `git show origin/main:docs/walkthroughs/main-server-setup.md` succeeds.
