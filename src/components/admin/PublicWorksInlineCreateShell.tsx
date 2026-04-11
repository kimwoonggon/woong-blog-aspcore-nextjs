'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { WorkEditor } from '@/components/admin/WorkEditor'

export function PublicWorksInlineCreateShell() {
  const router = useRouter()
  const [open, setOpen] = useState(false)
  const [formInstanceKey, setFormInstanceKey] = useState(0)

  return (
    <InlineAdminEditorShell
      open={open}
      onOpenChange={setOpen}
      triggerLabel="새 작업 쓰기"
      title="Works Inline Create"
      description="navbar를 유지한 채 현재 페이지 아래에서 새 작업을 작성합니다."
    >
      <WorkEditor
        key={formInstanceKey}
        inlineMode
        onSaved={() => {
          setOpen(false)
          setFormInstanceKey((current) => current + 1)
          router.refresh()
        }}
      />
    </InlineAdminEditorShell>
  )
}
