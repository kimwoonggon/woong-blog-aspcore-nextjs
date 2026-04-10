# Work Inline Video Embed Plan

## Summary
- Keep `WorkVideo[]` as the source of truth for uploaded and YouTube videos.
- Store inline placement only as body references using `<work-video-embed data-video-id="..."></work-video-embed>`.
- Support inline placement on existing works and on newly created works after the staged-video create flow redirects to edit.
- Preserve legacy behavior: works without inline embeds still render the saved video stack above the body.

## Implementation Changes
- Add a `workVideoEmbed` Tiptap block node that stores only `videoId` and previews the resolved video inside the editor.
- Switch `WorkEditor` content editing from the temporary raw textarea back to `TiptapEditor`.
- Add saved-video actions for `Insert Into Body` and `Remove From Body`, plus placed/unplaced state and orphan reference warnings.
- Redirect `Create And Add Videos` success to `/admin/works/{id}?videoInline=1` so inline placement can be completed immediately.
- Extend work content rendering so inline embeds resolve against `work.videos` and legacy top-stack rendering is skipped when inline embeds are present.

## Test Plan
- Unit
  - `work-video-embeds` helpers extract, split, and remove embed references correctly.
  - `InteractiveRenderer` renders inline embeds through `WorkVideoPlayer` and skips missing references.
  - `WorkEditor` inserts saved videos into the body, blocks deleting referenced videos, and redirects staged-video creates to edit.
- Playwright
  - Existing work flows still cover publish/edit/image upload/pagination.
  - Mixed inline video flow covers YouTube + uploaded MP4 placement inside the body.
  - Legacy public video flow still renders the fallback top-stack when no inline embed exists.

## Assumptions
- A saved video may be placed in the body at most once in v1.
- Missing inline references are ignored publicly and shown as warnings in the editor.
- No backend API changes are required; inline placement is a frontend content-format and rendering change.
