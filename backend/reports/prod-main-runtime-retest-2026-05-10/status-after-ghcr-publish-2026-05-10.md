# Production Runtime Retest Status After GHCR Publish

Generated at: 2026-05-10T20:13:00+09:00

## Objective

1. Pull/deploy the latest `main` runtime images on the production server and run production preflight.
2. Rerun Real Backend Test with `pageSize=12`, no seed/fixture target, no cache workaround, and real Work/Study URLs.
3. Choose one next implementation slice from HLS fatal fix, public detail serialization/body optimization, or DB/index optimization, then verify full e2e and CI/main promotion.

## Completed Evidence

- `CI Main Runtime` run `25626902724` passed for `main` SHA `3a19dd8bfc2b5b67653acab2a4396fe26d0a66f9`.
- `Publish GHCR Main` run `25627032158` passed for `main` SHA `3a19dd8bfc2b5b67653acab2a4396fe26d0a66f9`.
- GHCR runtime backend package has tags `main`, `latest`, and `sha-3a19dd8bfc2b`.
- GHCR runtime frontend package has tags `main`, `latest`, and `sha-3a19dd8bfc2b`.
- `CI Dev` run `25626665464` passed for `dev` SHA `8f02b38cc0286c493065d056161ebc2727967256`.
- Manual `Publish GHCR Dev` run `25627146533` passed for `dev` SHA `8f02b38cc0286c493065d056161ebc2727967256`.
- GHCR runtime dev backend/frontend packages have tags `dev` and `dev-sha-8f02b38cc028`.
- Legacy alias push is now optional and skipped when `GHCR_PACKAGES_TOKEN` is absent; runtime image publish is no longer blocked by legacy package permissions.

## Current Production Probe

Production public API still appears stale before server pull/recreate:

- `https://woonglab.com/api/public/works?page=1&pageSize=12` first item still exposes `period` and `iconUrl`.
- `https://woonglab.com/api/public/works/smoke-fluid-simulation` still exposes `contentJson`, `period`, and `iconUrl`.
- The current runtime DTOs no longer expose those stale fields, so this is deployment/runtime drift, not a fresh code requirement.

### Recheck - 2026-05-10T20:19:00+09:00

- `works-list` still exposes stale fields: `period`, `iconUrl`.
- `work-detail` still exposes stale fields: `period`, `iconUrl`, `contentJson`, `originalFileName`, `fileSize`, `createdAt`.
- `blogs-list` did not expose the checked stale fields.
- Latest Actions state remains green for `CI Main Runtime`, `Publish GHCR Main`, `CI Dev`, and manual `Publish GHCR Dev`.
- GitHub API says both runtime packages are `public`. A clean temporary Docker config can inspect both `main` manifests anonymously; the earlier local `denied` came from the default Docker credential state.

## Blocker

Codex cannot complete the server-side `docker compose pull/up` step because SSH authentication to `srv1592512.hstgr.cloud` / `187.127.111.73` is not available from this environment. No deploy workflow exists in the repository that can run production host commands remotely.

SSH recheck at `2026-05-10T20:19:00+09:00`:

- No SSH authentication agent is available.
- Host keys for `srv1592512.hstgr.cloud` and `187.127.111.73` still match the previously recorded fingerprints.
- BatchMode auth failed for `kimwoonggon@srv1592512.hstgr.cloud`, `kimwoonggon@187.127.111.73`, `root@srv1592512.hstgr.cloud`, and `root@187.127.111.73`.

GitHub deployment automation recheck at `2026-05-10T20:26:27+09:00`:

- Repository secrets list contains `PROMOTION_TOKEN` only.
- Repository variables list is empty.
- No `SSH_*`, `DEPLOY_*`, or `SERVER_*` deploy secret/variable exists for production host access.
- Repository search found no existing production host deploy workflow; only the pasteable server helper artifacts reference `srv1592512.hstgr.cloud`.

Windows OpenSSH recheck at `2026-05-10T20:28:08+09:00`:

- `/mnt/c/Windows/System32/OpenSSH/ssh.exe` is available.
- BatchMode auth failed for `root@srv1592512.hstgr.cloud`, `root@187.127.111.73`, `kimwoonggon@srv1592512.hstgr.cloud`, and `kimwoonggon@187.127.111.73`.
- Windows OpenSSH therefore does not provide a usable alternate key/agent path from this environment.

Production stale recheck at `2026-05-10T20:29:42+09:00`:

- `works-list` still exposes stale fields: `period`, `iconUrl`.
- `work-detail` still exposes stale fields: `period`, `iconUrl`, `contentJson`, `originalFileName`, `fileSize`, `createdAt`.
- Decision unchanged: do not run or interpret Real Backend Test until server block 1 deploy/preflight passes.

## Required Next Action

Run the two pasteable server blocks in:

- `backend/reports/prod-main-runtime-retest-2026-05-10/pasteable-server-commands.md`

Block 1 must pass before any load result is trusted. It updates only non-secret runtime image/base-url keys, pulls `main` runtime images, recreates containers, and runs `prod-runtime-preflight.sh`.

The block now checks GHCR manifest access before `docker compose pull` using a temporary Docker config, so stale host credentials should not break public runtime image pulls. If GHCR still denies access on the server, rerun it with:

```bash
GHCR_USER=kimwoonggon GHCR_TOKEN=<read-packages-token> bash /tmp/server-main-runtime-redeploy.sh
```

Block 2 should run only after block 1 passes. It reruns Real Backend Test using `pageSize=12`, no seed/fixture target, no cache workaround, and real public Work/Study targets.

## Not Complete

The active objective is not complete because production has not yet been redeployed from the latest runtime image, production preflight has not passed on the server, Real Backend Test has not been rerun under the required conditions, and no next performance/HLS slice has been selected from the new result.
