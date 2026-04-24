# Main Dev Promotion With License Audit

## Summary
- Rebuilt `main` from the current `dev` runtime tree using the repository promotion workflow.
- Fixed the runtime promotion allowlist so `main` CI receives the helper/test/script files it actually references.
- Reapplied the custom Woonggon Kim proprietary license on the promoted `main` tree.

## Intentionally Not Changed
- Did not use a direct manual `dev -> main` merge.
- Did not switch to Apache, MIT, or another permissive OSS license.
- Did not clean local artifact noise in the original feature working tree.

## Goal Check
- Goal: put current `dev` runtime contents onto `main`. Met.
- Goal: keep the custom Woonggon Kim license on `main`. Met.
- Goal: restore `main` CI by fixing promotion omissions. Met.

## Validations Performed
- Inspected failed `CI Main Runtime` logs and identified missing allowlisted files.
- Regenerated runtime-only `main` worktree after updating `scripts/main-runtime-allowlist.txt`.
- Verified presence of:
  - `tests/helpers/performance-test.ts`
  - `tests/helpers/latency.ts`
  - `scripts/summarize-e2e-latency.mjs`
  - `scripts/benchmark-frontend-performance.mjs`
  - `scripts/pact-provider-verify.sh`
- Verified `LICENSE`, `README.md`, and `package.json` license metadata in the promoted worktree.

## Risks / Yellow Flags
- The custom license still needs to remain part of the promotion source or promotion overlay path for future `dev -> main` promotions.
- `main` runtime CI may still expose additional omitted files if new tests/scripts are introduced later without updating the allowlist.

## Final Recommendation
- Keep `scripts/main-runtime-allowlist.txt` synchronized with the files referenced by `CI Main Runtime`.
- Preserve the root `LICENSE` as part of the runtime promotion path from now on.
