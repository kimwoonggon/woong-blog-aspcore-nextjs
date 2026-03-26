# Quality Verification Matrix

## Broad Sweep Targets

| Surface | Backend write path | DB persistence proof | Frontend/public readback proof |
|---|---|---|---|
| Pages | `/api/admin/pages` | backend tests + DB-backed page assertions | Playwright admin pages + `/introduction` / `/contact` |
| Site settings | `/api/admin/site-settings` | backend tests + singleton site settings assertions | Playwright home/admin pages + public site settings readback |
| Works | `/api/admin/works` | backend tests + work metadata/media assertions | Playwright works index/detail + public query handlers |
| Blogs | `/api/admin/blogs` | backend tests + slug/excerpt/content assertions | Playwright blog index/detail + public query handlers |
| Resume | `/api/admin/site-settings` + `/api/uploads` | backend upload/site-settings tests | Playwright resume upload/download + public resume page |
| Media | `/api/uploads` | backend upload/delete + asset persistence assertions | Playwright image upload/render paths + `/media/*` requests |

## Green Evidence Chain

1. `docker run --pull=never --rm -v "$PWD/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/Portfolio.Api.Tests/Portfolio.Api.Tests.csproj`
2. `./scripts/db-load-smoke.sh`
3. `./scripts/backend-http-smoke.sh`
4. `npm run test -- --run && npm run lint && npm run typecheck && npm run build`
5. `npm run test:e2e:stack`
6. `curl -fsS http://localhost/api/health`

## Known manual lane
- `tests/manual-auth.spec.ts` remains the manual provider-login verification lane.
