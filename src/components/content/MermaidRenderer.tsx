"use client"

import { useEffect, useId, useState } from 'react'

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
      <pre className={className ?? 'my-6 overflow-auto rounded-lg border border-border bg-card p-4'}>
        <code>{code}</code>
      </pre>
    )
  }

  if (!svg) {
    return (
      <div className={className ?? 'my-6 overflow-auto rounded-lg border border-border bg-card p-4'}>
        <span className="text-sm text-muted-foreground">Rendering Mermaid diagram…</span>
      </div>
    )
  }

  return (
    <div
      className={className ?? 'my-6 overflow-auto rounded-lg border border-border bg-card p-4'}
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  )
}
