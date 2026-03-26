"use client"

import { useState } from 'react'
import { ChevronDown, ChevronUp, PencilLine } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface InlineAdminEditorShellProps {
  triggerLabel: string
  title: string
  description?: string
  backLabel?: string
  children: React.ReactNode
}

export function InlineAdminEditorShell({
  triggerLabel,
  title,
  description,
  backLabel = '뒤로가기',
  children,
}: InlineAdminEditorShellProps) {
  const [open, setOpen] = useState(false)

  return (
    <section className="mt-6 space-y-4 rounded-3xl border border-border/80 bg-card/90 p-5 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="space-y-1">
          <h2 className="text-lg font-semibold text-foreground">{title}</h2>
          {description && (
            <p className="text-sm text-muted-foreground">{description}</p>
          )}
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            className="gap-2 rounded-full"
            onClick={() => setOpen((value) => !value)}
          >
            <PencilLine className="h-4 w-4" />
            {triggerLabel}
            {open ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </Button>
          {open && (
            <Button
              type="button"
              variant="ghost"
              className="gap-2 rounded-full text-muted-foreground"
              onClick={() => setOpen(false)}
            >
              {backLabel}
            </Button>
          )}
        </div>
      </div>

      {open && (
        <div className="rounded-2xl border border-border/80 bg-background p-4 shadow-sm">
          {children}
        </div>
      )}
    </section>
  )
}
