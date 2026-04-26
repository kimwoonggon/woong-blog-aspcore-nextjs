"use client"

import { useEffect, useId, useState } from 'react'
import { cn } from '@/lib/utils'

interface MermaidModule {
  initialize: (config: {
    startOnLoad: boolean
    securityLevel?: string
    theme?: string
    fontFamily?: string
  }) => void
  render: (id: string, definition: string) => Promise<{ svg: string }>
}

interface MermaidRendererProps {
  code: string
  className?: string
}

const centeredMermaidClassName = 'my-6 overflow-x-auto rounded-lg border border-border bg-card p-4 text-center [&_svg]:mx-auto [&_svg]:block [&_svg]:h-auto [&_svg]:max-w-full'

function getTheme() {
  if (typeof document === 'undefined') {
    return 'default'
  }

  return document.documentElement.classList.contains('dark') ? 'dark' : 'default'
}

export function MermaidRenderer({ code, className }: MermaidRendererProps) {
  const [svg, setSvg] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const reactId = useId().replace(/:/g, '-')

  useEffect(() => {
    const diagram = code.trim()

    if (!diagram) {
      setSvg(null)
      setError(null)
      return
    }

    let cancelled = false
    setSvg(null)
    setError(null)

    const renderDiagram = async () => {
      try {
        const mod = await import('mermaid')
        const mermaid = (mod.default ?? mod) as MermaidModule

        mermaid.initialize({
          startOnLoad: false,
          securityLevel: 'strict',
          theme: getTheme(),
          fontFamily: 'inherit',
        })

        const result = await mermaid.render(`mermaid-${reactId}`, diagram)

        if (!cancelled) {
          setSvg(result.svg)
          setError(null)
        }
      } catch (renderError) {
        if (!cancelled) {
          setSvg(null)
          setError(renderError instanceof Error ? renderError.message : 'Mermaid rendering failed.')
        }
      }
    }

    void renderDiagram()

    return () => {
      cancelled = true
    }
  }, [code, reactId])

  if (error) {
    return (
      <pre data-testid="mermaid-renderer" className={cn(centeredMermaidClassName, className)}>
        <code>{code}</code>
      </pre>
    )
  }

  if (!svg) {
    return (
      <div data-testid="mermaid-renderer" className={cn(centeredMermaidClassName, className)}>
        <span className="text-sm text-muted-foreground">Rendering Mermaid diagram…</span>
      </div>
    )
  }

  return (
    <div
      data-testid="mermaid-renderer"
      className={cn(centeredMermaidClassName, className)}
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  )
}
