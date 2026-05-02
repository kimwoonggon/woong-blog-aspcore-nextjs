# Codex GPT-5.5 runtime env hotfix - 2026-05-03

## Changed
- Updated `.env`, `.env.example`, `.env.staging.example`, and `.env.prod.example` so `CODEX_MODEL` defaults to `gpt-5.5`.
- Updated `CODEX_ALLOWED_MODELS` in the same env files so `gpt-5.5` appears before `gpt-5.4`.
- Updated `AIFixDialog` so stale runtime or localStorage state cannot remove `gpt-5.5` from the Codex model dropdown.
- Added a persistent header action button in the shared AI Fix/AI Enrich dialog so conversion can be started without scrolling to the preview empty state.
- Restarted the Docker dev stack with `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh`.

## Intentionally not changed
- No backend AI provider execution logic was changed.
- No OpenAI/Azure provider behavior was changed.
- No remote branches were pushed for this hotfix.

## Verification performed
- Inspected runtime env and confirmed `.env` was overriding backend defaults with GPT-5.4-only Codex values.
- Docker dev stack rebuilt and restarted successfully.
- Next.js production build completed during Docker rebuild.

## Risks and follow-up
- Browser localStorage may still contain `admin-ai-codex-model=gpt-5.4`; the dropdown should still include GPT-5.5, but the selected value may remain the saved model until changed.
- No E2E suite was run for this hotfix because the user asked for immediate Docker/runtime correction.

## Recommendation
- Check the AI Fix/AI Enrich dialog in the browser. If it now looks correct, commit and push this hotfix through the normal dev-to-staging flow.
