# Release Transition Todo

- [x] `A1` lock `dev` as the default development branch strategy in docs
- [x] `A2` add runtime-only promotion script and allowlist for `main`
- [x] `A3` add a `main`-only GHCR publish workflow
- [x] `A4` replace the old mixed CI/CD workflow with dedicated CI + promotion/publish workflows
- [x] `A5` add `dev` and `main` local docker launcher scripts
- [x] `A6` hide `Continue as Local Admin` in `main`-like runtime
- [x] `A7` block non-admin login and show admin-only guidance on `/login`
- [x] `A8` verify `main`-like browser output: login hidden + detail center + dropdown visible
- [x] `A9` verify `dev`-like browser output: login shortcut visible + signed-in dropdown visible
- [x] `A10` create and push remote `dev` branch
- [x] `A11` replace isolated image CI with compose-first full-stack verification
- [x] `A12` add frontend/backend/db smoke assertions to compose CI
- [x] `A13` require `main` GHCR publish workflow to rerun `main` compose smoke
- [ ] `A14` run first `release/main-promote -> main` promotion
