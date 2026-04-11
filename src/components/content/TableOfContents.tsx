"use client"

import { useEffect, useMemo, useState } from 'react'

interface TableOfContentsProps {
  contentRootId: string
}

interface HeadingItem {
  id: string
  text: string
  level: 2 | 3
}

function normalizeSlugPart(value: string) {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9가-힣\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')
}

export function slugifyHeading(text: string, usedSlugs?: Map<string, number>) {
  const baseSlug = normalizeSlugPart(text) || 'section'
  if (!usedSlugs) {
    return baseSlug
  }

  const seenCount = usedSlugs.get(baseSlug) ?? 0
  usedSlugs.set(baseSlug, seenCount + 1)
  return seenCount === 0 ? baseSlug : `${baseSlug}-${seenCount + 1}`
}

function collectHeadings(root: HTMLElement) {
  const usedSlugs = new Map<string, number>()
  const headings = Array.from(root.querySelectorAll<HTMLElement>('h2, h3'))

  return headings
    .map((heading) => {
      const text = heading.textContent?.trim() ?? ''
      if (!text) {
        return null
      }

      const level = heading.tagName === 'H3' ? 3 : 2
      const id = heading.id || slugifyHeading(text, usedSlugs)
      heading.id = id

      return { id, text, level } satisfies HeadingItem
    })
    .filter((item): item is HeadingItem => item !== null)
}

export function TableOfContents({ contentRootId }: TableOfContentsProps) {
  const [items, setItems] = useState<HeadingItem[]>([])
  const [activeId, setActiveId] = useState<string>('')

  useEffect(() => {
    const root = document.getElementById(contentRootId)
    if (!root) {
      return
    }
    let observer: IntersectionObserver | null = null
    const frame = window.requestAnimationFrame(() => {
      const nextItems = collectHeadings(root)
      setItems(nextItems)
      setActiveId(nextItems[0]?.id ?? '')

      if (nextItems.length === 0) {
        return
      }

      observer = new IntersectionObserver(
        (entries) => {
          const visibleHeading = entries
            .filter((entry) => entry.isIntersecting)
            .sort((left, right) => left.boundingClientRect.top - right.boundingClientRect.top)[0]

          if (visibleHeading?.target instanceof HTMLElement) {
            setActiveId(visibleHeading.target.id)
          }
        },
        {
          rootMargin: '0px 0px -70% 0px',
          threshold: [0, 1],
        },
      )

      nextItems.forEach((item) => {
        const heading = document.getElementById(item.id)
        if (heading) {
          observer?.observe(heading)
        }
      })
    })

    return () => {
      window.cancelAnimationFrame(frame)
      observer?.disconnect()
    }
  }, [contentRootId])

  const renderedItems = useMemo(() => items, [items])

  if (renderedItems.length === 0) {
    return null
  }

  return (
    <nav
      aria-label="Table of contents"
      data-testid="blog-toc"
      className="sticky top-24 rounded-2xl border border-border/80 bg-background/95 p-4 shadow-sm"
    >
      <p className="mb-3 text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
        On This Page
      </p>
      <ol className="space-y-2 text-sm">
        {renderedItems.map((item) => (
          <li key={item.id}>
            <a
              href={`#${item.id}`}
              className={[
                'block rounded-xl px-3 py-2 transition-colors',
                item.level === 3 ? 'ml-3 text-muted-foreground' : 'font-medium',
                activeId === item.id
                  ? 'bg-muted text-foreground'
                  : 'text-muted-foreground hover:bg-muted/60 hover:text-foreground',
              ].join(' ')}
            >
              {item.text}
            </a>
          </li>
        ))}
      </ol>
    </nav>
  )
}
