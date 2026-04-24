import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import type { AnchorHTMLAttributes, ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Navbar } from '@/components/layout/Navbar'

const mocks = vi.hoisted(() => ({
  pathname: '/',
  push: vi.fn(),
}))

vi.mock('next/navigation', () => ({
  usePathname: () => mocks.pathname,
  useRouter: () => ({ push: mocks.push }),
}))

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}))

vi.mock('@/components/ui/ThemeToggle', () => ({
  ThemeToggle: ({ testId = 'theme-toggle' }: { testId?: string }) => <button type="button" data-testid={testId}>Theme</button>,
}))

vi.mock('@/components/ui/sheet', () => ({
  Sheet: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  SheetTrigger: ({ children }: { children: ReactNode }) => <>{children}</>,
  SheetContent: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}))

describe('Navbar mobile controls', () => {
  beforeEach(() => {
    mocks.pathname = '/'
    mocks.push.mockReset()
    document.body.innerHTML = ''
  })

  afterEach(() => {
    cleanup()
  })

  it('renders six mobile bottom tabs and sets aria-current on the active page', () => {
    mocks.pathname = '/works'
    render(<Navbar ownerName="Woong" />)

    const bottomNav = screen.getByTestId('mobile-bottom-nav')
    const navQueries = within(bottomNav)

    expect(bottomNav).toBeInTheDocument()
    expect(navQueries.getByRole('link', { name: 'Home' })).toHaveAttribute('href', '/')
    expect(navQueries.getByRole('link', { name: 'Intro' })).toHaveAttribute('href', '/introduction')
    expect(navQueries.getByRole('link', { name: 'Works' })).toHaveAttribute('href', '/works')
    expect(navQueries.getByRole('link', { name: 'Study' })).toHaveAttribute('href', '/blog')
    expect(navQueries.getByRole('link', { name: 'Contact' })).toHaveAttribute('href', '/contact')
    expect(navQueries.getByRole('link', { name: 'Resume' })).toHaveAttribute('href', '/resume')
    expect(navQueries.getByRole('link', { name: 'Works' })).toHaveAttribute('aria-current', 'page')
  })

  it('focuses the study search input when the mobile search button is used on /blog', () => {
    mocks.pathname = '/blog'
    const searchInput = document.createElement('input')
    searchInput.id = 'study-search'
    document.body.appendChild(searchInput)

    render(<Navbar ownerName="Woong" />)

    fireEvent.click(screen.getByRole('button', { name: 'Open search' }))

    expect(document.activeElement).toBe(searchInput)
    expect(mocks.push).not.toHaveBeenCalled()
  })

  it('focuses the works search input when the mobile search button is used on /works', () => {
    mocks.pathname = '/works'
    const searchInput = document.createElement('input')
    searchInput.id = 'work-search'
    document.body.appendChild(searchInput)

    render(<Navbar ownerName="Woong" />)

    fireEvent.click(screen.getByRole('button', { name: 'Open search' }))

    expect(document.activeElement).toBe(searchInput)
    expect(mocks.push).not.toHaveBeenCalled()
  })

  it('routes to /blog with focusSearch when mobile search is used outside blog/works', () => {
    mocks.pathname = '/contact'
    render(<Navbar ownerName="Woong" />)

    fireEvent.click(screen.getByRole('button', { name: 'Open search' }))

    expect(mocks.push).toHaveBeenCalledWith('/blog?focusSearch=1')
  })
})
