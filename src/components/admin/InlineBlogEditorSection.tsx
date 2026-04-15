"use client"

import { useState } from 'react'
import { BlogEditor } from '@/components/admin/BlogEditor'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'

interface InlineBlogEditorSectionProps {
  initialBlog: {
    id?: string
    title?: string
    excerpt?: string
    slug?: string
    tags?: string[]
    published?: boolean
    content?: { html?: string }
    publishedAt?: string | null
    updatedAt?: string
  }
  title?: string
  description?: string
  triggerLabel?: string
}

export function InlineBlogEditorSection({
  initialBlog,
  title = 'Blog Inline Editor',
  description = '현재 게시물 뷰를 유지한 채 바로 수정합니다.',
  triggerLabel = '글 수정',
}: InlineBlogEditorSectionProps) {
  const [open, setOpen] = useState(false)

  return (
    <InlineAdminEditorShell
      open={open}
      onOpenChange={setOpen}
      triggerLabel={triggerLabel}
      title={title}
      description={description}
    >
      <BlogEditor
        initialBlog={initialBlog}
        inlineMode
        onSaved={() => setOpen(false)}
      />
    </InlineAdminEditorShell>
  )
}
