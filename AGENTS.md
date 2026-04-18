# Repository Guidelines

## Project Structure & Module Organization

Frontend source is a Next.js App Router app under `src/app`, with reusable UI in `src/components`, shared helpers in `src/lib`, hooks in `src/hooks`, and static assets in `public`.

The ASP.NET Core backend lives in `backend/src/WoongBlog.Api`, organized by `Modules`, `Application`, `Infrastructure`, `Domain`, and `Common`. Backend tests are in `backend/tests/WoongBlog.Api.Tests`. Frontend unit tests are in `src/test`; Playwright specs are in `tests`. Docs are under `docs`, and automation is in `scripts`.

## Build, Test, and Development Commands

- `docker compose -f docker-compose.dev.yml up -d --build`: start the supported full-stack dev environment.
- `./scripts/dev-up.sh`: start the dev stack with the local admin shortcut enabled.
- `npm run dev`: run the frontend only for isolated UI work.
- `npm run build`: build the Next.js app.
- `npm run lint`: run ESLint.
- `npm run typecheck`: run TypeScript checks after clearing Next build folders.
- `npm test`: run Vitest unit tests.
- `npm run test:e2e`: run the full Playwright suite serially.
- `dotnet test backend/WoongBlog.sln`: run backend xUnit tests.

Use compose for auth, uploads, admin mutations, and release checks.

For dev-related branches, including `dev`, `dev/*`, and branches intended to merge into `dev`, run the Docker-based development environment bound to `http://127.0.0.1` and validate frontend behavior with Playwright tests.

## Coding Style & Naming Conventions

Use TypeScript, React Server Components, and existing App Router patterns. Keep component files in PascalCase, hooks as `useX`, and unit tests as `*.test.ts` or `*.test.tsx`. Playwright specs use `*.spec.ts`.

C# uses nullable reference types and implicit usings. Follow the module layout: endpoints in `Modules/*/Api`, commands and queries in `Application`, persistence and external services in `Infrastructure`. Name backend tests `*Tests.cs`.

## Testing Guidelines

Add or update the smallest relevant suite. Prefer Vitest for frontend logic and components, Playwright for browser flows and visual or routing regressions, and xUnit for backend endpoints, validation, persistence contracts, and services. For full-stack behavior, run the relevant `npm run test:e2e:*` subset against compose.

Use the `tdd` skill actively for development work. Work in small red-green-refactor slices: add or update a behavior-focused test first, implement the minimum change, then refactor with tests green.

Every developed feature must be represented in tests. Add coverage for new or changed behavior, remove obsolete tests for removed behavior, and ensure every newly written function or method is exercised by an appropriate test. Use Playwright for frontend validation whenever UI behavior, routing, visual behavior, or browser integration is affected.

## Agent Workflow Requirements

When a work plan is set, create or update a dated TODO file named `todolist-YYYY-MM-DD.md` before implementation. Record all TODOs carefully, include the user's instructions, and map each instruction to the TODO items that satisfy it.

Before starting TODO execution and after completing TODO progress, confirm that the actual work still matches the planned TODO list. After each TODO is completed, run an appropriate verification step and record the result in the TODO file or final summary.

Prepare a backup before modifying code or configuration. Keep edits scoped to the requested work and do not revert user changes unless explicitly instructed.

Subagents are user-triggered only. Do not spawn or delegate to subagents unless the user explicitly asks for subagents, delegation, or parallel agent work.

For specialist knowledge, use the `find-skills` skill before implementation. Search for relevant skills, prefer suitable popular or high-star options when available, install or register the selected skill when feasible, and use it during development. If no exact needed skill is already available, still search for a related skill and proceed with the best available option or document why none was usable.

## Codex Panic Prevention

Codex/plugin panic is a serious blocker. A known recurring cause is the Codex TUI plugin store failing with `plugin cache root should be absolute: No such file or directory`. This is caused by Codex trying to canonicalize a plugin cache root that is missing or is being resolved as a non-absolute path. It is separate from application code failures and can appear after skill/plugin installation, subagent/plugin warm-up, or long-running commands that trigger plugin refresh.

Before installing skills/plugins, spawning subagents, or starting long-running Docker/build/test commands, ensure these plugin cache directories exist as absolute paths:

```bash
mkdir -p /home/kimwoonggon/.codex/plugins/cache /home/kimwoonggon/.codex/.tmp/plugins
readlink -f /home/kimwoonggon/.codex/plugins/cache
readlink -f /home/kimwoonggon/.codex/.tmp/plugins
```

Never set `CODEX_HOME`, `CODEX_HOME_DIR`, plugin cache paths, or plugin-related environment values to relative paths. Use absolute paths such as `/home/kimwoonggon/.codex`.

If a Codex panic happens, stop assuming any prior background command or subagent state is reliable. First inspect `~/.codex/log/codex-tui.log` for the panic line and verify the plugin cache directories above. Then re-check external process state explicitly with commands such as `docker compose ps -a`, targeted logs, and health probes before continuing.

Avoid installing new skills/plugins or spawning additional subagents after a plugin-cache panic unless the cache paths have been verified and the work genuinely requires it.

## Commit & Pull Request Guidelines

Recent commits use concise imperative subjects, usually sentence case, such as `Stabilize batch AI provider test timing`. Keep subjects specific and avoid bundling unrelated work.

Open feature PRs into `dev` unless preparing a production promotion. Include a short description, linked issue or context, verification commands, and screenshots or recordings for UI changes. Call out configuration, database, Docker, or security-sensitive changes explicitly.

## Security & Configuration Tips

Start from `.env.example`, `.env.staging.example`, or `.env.prod.example`; do not commit secrets. Check production-like behavior with the compose files and scripts in `README.md`, especially when changing auth, media, nginx routing, or runtime flags.
