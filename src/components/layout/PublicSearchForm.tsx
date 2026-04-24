'use client'

import Link from 'next/link'
import { useEffect, useRef } from 'react'
import { Search, X } from 'lucide-react'

interface PublicSearchFormProps {
  action: '/blog' | '/works'
  inputId: string
  inputName: string
  query: string
  placeholder: string
  inputAriaLabel: string
  shouldFocusSearch: boolean
  clearHref: '/blog' | '/works'
  clearLabel: string
  wrapperClassName?: string
}

export function PublicSearchForm({
  action,
  inputId,
  inputName,
  query,
  placeholder,
  inputAriaLabel,
  shouldFocusSearch,
  clearHref,
  clearLabel,
  wrapperClassName,
}: PublicSearchFormProps) {
  const inputRef = useRef<HTMLInputElement | null>(null)

  const focusInput = () => {
    inputRef.current?.focus({ preventScroll: true })
    inputRef.current?.select()
  }

  useEffect(() => {
    if (!shouldFocusSearch) {
      return
    }

    window.requestAnimationFrame(focusInput)
  }, [shouldFocusSearch])

  return (
    <form action={action} method="get" role="search" className={wrapperClassName ?? 'hidden items-center gap-2 lg:flex'}>
      <label htmlFor={inputId} className="sr-only">{inputAriaLabel}</label>
      <div className="flex min-h-11 items-center gap-2 rounded-full border border-border bg-background px-3 transition-colors focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/20">
        <Search className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
        <input
          ref={inputRef}
          id={inputId}
          name={inputName}
          defaultValue={query}
          placeholder={placeholder}
          className="w-full min-w-0 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground lg:w-56"
        />
      </div>

      <button
        type="submit"
        aria-label={inputAriaLabel}
        title={inputAriaLabel}
        className="inline-flex min-h-11 min-w-11 items-center justify-center rounded-full bg-foreground p-2 text-sm font-semibold text-background transition-colors hover:bg-foreground/90"
      >
        <Search className="h-4 w-4" aria-hidden="true" />
      </button>
      {query ? (
        <Link
          href={clearHref}
          aria-label={clearLabel}
          title={clearLabel}
          className="inline-flex min-h-11 items-center justify-center gap-2 rounded-full border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
        >
          <X className="h-4 w-4" aria-hidden="true" />
          Clear
        </Link>
      ) : null}
    </form>
  )
}
