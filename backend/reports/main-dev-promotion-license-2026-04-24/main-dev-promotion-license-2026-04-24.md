# Main Dev Promotion With License Audit

## Summary
- Rebuilt the `main` branch contents from the current `dev` branch using the repository's runtime-only promotion workflow instead of a direct merge.
- Added a custom proprietary `LICENSE` for Woonggon Kim to the promoted `main` tree.
- Updated `README.md` and `package.json` to reference the repository license.

## Intentionally Not Changed
- Did not use Apache, MIT, or another permissive OSS license.
- Did not perform a manual line-by-line `dev -> main` merge; this repository's runtime-only promotion path was used instead.
- Did not modify local artifact noise in the original working tree.

## Goal Check
- Goal: put the current `dev` runtime tree onto `main`. Met.
- Goal: include a Woonggon Kim license on `main`. Met.
- Goal: avoid Apache-style licensing. Met.

## Validations Performed
- Verified the direct merge path was structurally wrong due to the runtime-only `main` branch shape.
- Rebuilt `main` from `origin/dev` using `scripts/promote-main-runtime.sh`.
- Verified root `LICENSE`, `README.md`, and `package.json` in the promotion worktree before commit.

## Risks / Yellow Flags
- This remains a custom proprietary license and not an OSI-approved open-source license.
- Future runtime promotions from `dev` will need to keep carrying this license change; if the promotion workflow should always preserve it, the allowlist and source branch policy should be kept consistent.

## Final Recommendation
- Treat the runtime-only promotion workflow as the only supported `dev -> main` path.
- Keep the root `LICENSE` file as the source of truth for `main` branch usage rights.
