# Active Goal Completion Audit - 2026-05-12

## Completion Decision

Do **not** mark the active goal complete.

The latest main runtime CI and GHCR publish are green, but the required server-side deploy/preflight evidence and valid Real Backend Test evidence are still missing. The public origin probe also still shows stale Works list DTO fields, so it does not prove the current main runtime is deployed.

## Objective Restated As Success Criteria

1. Server pulls and deploys the current `main` runtime images.
2. Production preflight passes after that deployment.
3. Real Backend Test is rerun after that deployment with:
   - `pageSize=12`
   - no seed/fixture read targets
   - no cache workaround
   - real Work and Study read URLs
4. The next implementation slice is selected from the valid Real Backend Test result:
   - HLS fatal fix, or
   - public detail serialization/body optimization, or
   - DB/index optimization
5. After the selected slice, full E2E is green.
6. CI is green for the branch/PR and the main promotion path.

## Prompt-To-Artifact Checklist

| Requirement | Evidence inspected | Result |
| --- | --- | --- |
| Current `main` CI green | GitHub Actions run `25717479751`, `CI Main Runtime`, SHA `389a117ee8cda43e84536c85164bf13afd8e38bf` | Present |
| Current `main` GHCR publish green | GitHub Actions run `25717736048`, `Publish GHCR Main`, SHA `389a117ee8cda43e84536c85164bf13afd8e38bf` | Present |
| Main runtime images are readable | `docker manifest inspect` for backend/frontend `:main` | Present |
| Backend runtime digest | `sha256:ae0679f8166380e0edef07c3d546cc31c9c291380e6f7eccc691c225c44afdf2` | Present |
| Frontend runtime digest | `sha256:8cd7cdea54ad6388bcb34f3b8b694cf87e9721a35273cafd35d0f5fea0bfb5f3` | Present |
| GitHub production SSH deploy path usable | Repository secrets list and environments list | Missing: only `PROMOTION_TOKEN`; no environments |
| Production Runtime Redeploy success | `gh run list --workflow "Production Runtime Redeploy"` | Missing: latest runs are May 10 failures on old SHA |
| Server actually pulled/deployed current images | Required evidence would be server-side compose/image/preflight output | Missing |
| Production preflight after deploy | Required evidence would be `scripts/prod-runtime-preflight.sh` output from the deployed server | Missing |
| Public origin proves current main runtime | `GET https://woonglab.com/api/public/works?page=1&pageSize=12` | Not proven: response still includes stale `iconUrl` and `period` fields |
| Real Backend Test rerun uses `pageSize=12` | Required evidence would be real-load summary with list page size | Missing |
| Real Backend Test avoids seed/fixture | Required evidence would be read target URLs and guard output | Missing |
| Real Backend Test avoids cache workaround | Required evidence would be runner config and generated k6 target URLs | Missing |
| Real Work/Study URLs used | Required evidence would be Work/Study read URLs from current public data | Missing |
| Result-based next slice selected | Requires valid Real Backend Test result | Missing |
| Full E2E after selected slice | Requires selected slice implementation and test result | Missing |
| CI green after selected slice | Requires PR/main CI result after selected slice | Missing |

## Evidence Details

### GitHub Actions

Recent relevant successful runs:

- `CI Main Runtime`
  - run: `25717479751`
  - branch: `main`
  - SHA: `389a117ee8cda43e84536c85164bf13afd8e38bf`
  - conclusion: `success`
- `Publish GHCR Main`
  - run: `25717736048`
  - branch: `main`
  - SHA: `389a117ee8cda43e84536c85164bf13afd8e38bf`
  - conclusion: `success`
- PR #187 `Document production redeploy handoff state`
  - branch: `docs/production-redeploy-handoff-20260512`
  - base: `dev`
  - merge state: `CLEAN`
  - CI Dev run `25721009999`: `success`
  - status: still open

Latest `Production Runtime Redeploy` runs:

- run `25628772593`: `failure`, created `2026-05-10T12:30:18Z`, SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- run `25628614468`: `failure`, created `2026-05-10T12:22:28Z`, SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- run `25628508822`: `failure`, created `2026-05-10T12:17:16Z`, SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`

### Repository Secrets And Environments

`gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs --json name,updatedAt` returned only:

- `PROMOTION_TOKEN`

`gh api repos/kimwoonggon/woong-blog-aspcore-nextjs/environments --jq '.environments[]?.name'` returned no environments.

That means the GitHub-hosted production SSH deploy path still lacks the expected `PROD_SSH_*` secret evidence.

### Public Origin Probe

Probe time: `2026-05-12T08:26Z`.

- `GET https://woonglab.com/api/health`
  - HTTP: `200`
  - total: `1.119451s`
  - body: `{"status":"ok","service":"portfolio-api",...}`
- `GET https://woonglab.com/api/public/works?page=1&pageSize=12`
  - HTTP: `200`
  - total: `1.099498s`
  - downloaded bytes: `3138`
  - parsed item count: `7`
  - first item keys: `category, excerpt, iconUrl, id, period, publishedAt, slug, tags, thumbnailUrl, title`
- `GET https://woonglab.com/api/public/blogs?page=1&pageSize=12`
  - HTTP: `200`
  - total: `1.077573s`
  - downloaded bytes: `4867`
  - parsed item count: `12`
  - first item keys: `coverUrl, excerpt, id, publishedAt, slug, tags, title`

Current local `WorkCardDto` list fields are:

- `Id`
- `Slug`
- `Title`
- `Excerpt`
- `Category`
- `Tags`
- `ThumbnailUrl`
- `PublishedAt`

Therefore the public Works list still exposing `period` and `iconUrl` is stale against current code and cannot be used as proof that the current main runtime has been deployed.

## Current Blocker

The next required evidence is server-side, not local:

- proof that the production host pulled the current `main` runtime images
- proof that compose restarted with those images
- production preflight result from that runtime
- real-load summary from that runtime

Because production SSH/remote server access is explicitly out of scope in this thread, this workspace cannot directly produce that evidence.

## Existing Handoff Artifact

The operator handoff remains the correct next executable artifact:

- `backend/reports/current-main-server-evidence-handoff-2026-05-12/current-main-server-evidence-handoff-2026-05-12.md`

It contains the server-side command/script path and expected artifacts. It still does not replace the required server-side output; it only defines how that evidence should be produced.

Post-audit action performed in this workspace: the handoff Markdown, JSON, HTML, and shell script were refreshed to SHA `389a117ee8cda43e84536c85164bf13afd8e38bf`, short tag `sha-389a117ee8cd`, backend digest `sha256:ae0679f8166380e0edef07c3d546cc31c9c291380e6f7eccc691c225c44afdf2`, and frontend digest `sha256:8cd7cdea54ad6388bcb34f3b8b694cf87e9721a35273cafd35d0f5fea0bfb5f3`.

## Recommendation

Do not run Real Backend Test against the current public origin yet. First produce server-side deploy/preflight evidence for latest `main`; otherwise the load result is ambiguous and cannot drive the HLS/detail/DB slice selection.
