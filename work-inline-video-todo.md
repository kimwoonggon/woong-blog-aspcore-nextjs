# Work Inline Video Embed TODO

## Editor and Content
- [x] Add shared work video embed parsing helpers
- [x] Add `workVideoEmbed` Tiptap node and preview component
- [x] Reconnect `WorkEditor` to `TiptapEditor`
- [x] Add saved-video inline placement actions and placed/unplaced status
- [x] Add orphan reference warnings in the editor

## Create and Render Flow
- [x] Redirect staged-video create flow to work edit for inline placement
- [x] Render inline work videos inside public work content
- [x] Keep legacy top-stack fallback when no inline embeds exist

## Tests
- [x] Add unit coverage for embed helpers
- [x] Add unit coverage for inline renderer behavior
- [x] Extend `work-editor` unit coverage for insert/delete-guard/create redirect
- [x] Update work e2e specs to use Tiptap editor input
- [x] Add mixed inline YouTube + MP4 e2e coverage
- [x] Re-run grouped work/public/video regression suites

## Docs
- [x] Update quality verification matrix with inline video evidence
