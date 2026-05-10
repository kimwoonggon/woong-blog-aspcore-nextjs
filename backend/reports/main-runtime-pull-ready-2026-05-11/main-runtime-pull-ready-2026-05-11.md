# Main Runtime Pull-Ready Audit - 2026-05-11

## Goal

User corrected the production scope: do not treat SSH access or automated remote redeploy as the blocker. The practical requirement is that the server can pull the `main` runtime Docker images, fill `.env` with secrets/server values, start compose, and reach the site/health endpoint.

## Changes

- Updated `.env.prod.example` to use the actual published GHCR `main` runtime images:
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
- Updated `docs/walkthroughs/main-server-setup.md` with the minimal server command flow:
  - copy `.env.prod.example` to `.env.prod`
  - fill `.env.prod`
  - pull images
  - start compose
  - probe `/api/health`
- Updated `todolist-2026-05-11.md` with the corrected scope, validations, backups, and report paths.

## Intentionally Not Changed

- No production SSH access was attempted.
- No remote server command was executed.
- No repository secret or production secret workflow was modified.
- No backend performance slice was changed in this correction.
- No Docker runtime image was rebuilt by this local change.

## Evidence

- Latest `origin/main`: `44e93243da153254526f0753e9324d5e9962ab19`.
- `CI Main Runtime` run `25634824569`: success for `44e93243da153254526f0753e9324d5e9962ab19`.
- `Publish GHCR Main` run `25634944032`: success for `44e93243da153254526f0753e9324d5e9962ab19`.
- GHCR manifest checks passed for:
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`

## Validation

- RED check before the change found placeholder image owner values in `.env.prod.example`.
- GREEN check after the change: `rg -n "ghcr.io/your-owner" .env.prod.example docs/walkthroughs/main-server-setup.md` returned no matches.
- Compose config check passed:

```bash
APP_ENV_FILE=.env.prod.example docker compose --env-file .env.prod.example -f docker-compose.prod.yml config
```

- Resolved compose config contains:
  - backend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
  - frontend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
  - `LoadTesting__BaseUrl: https://woonglab.com`
  - `NEXT_PUBLIC_SITE_URL: https://woonglab.com`
- Diff whitespace check passed:

```bash
git diff --check -- .env.prod.example docs/walkthroughs/main-server-setup.md todolist-2026-05-11.md
```

## Remaining Goal Items

- Server-side pull/up and public health probe are still user/server-side actions, not local repo actions.
- Production Real Backend Test with `pageSize=12`, no seed, no cache remains dependent on the server running the current `main` images.
- The next backend performance slice should be selected from a fresh real-load result, or from dev-only evidence if production is intentionally excluded.

## Recommendation

Use the documented server flow in `docs/walkthroughs/main-server-setup.md`. Once the server is running the `main` images, run the production preflight and Real Backend Test through the public URL. Until that external result exists, do not mark the broader active goal complete.
