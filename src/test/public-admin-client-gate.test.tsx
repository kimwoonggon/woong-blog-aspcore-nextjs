import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { PublicAdminClientGate, resetPublicAdminClientSessionForTests } from '@/components/admin/PublicAdminClientGate'

describe('PublicAdminClientGate', () => {
  afterEach(() => {
    resetPublicAdminClientSessionForTests()
    vi.unstubAllGlobals()
  })

  it('shows admin affordances after the browser session confirms admin role', async () => {
    vi.stubGlobal('fetch', vi.fn(async () =>
      new Response(JSON.stringify({ authenticated: true, role: 'admin' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    ) as typeof fetch)

    render(
      <PublicAdminClientGate>
        <button type="button">Admin edit</button>
      </PublicAdminClientGate>,
    )

    expect(await screen.findByRole('button', { name: 'Admin edit' })).toBeInTheDocument()
  })

  it('keeps admin affordances hidden for anonymous visitors', async () => {
    vi.stubGlobal('fetch', vi.fn(async () =>
      new Response(JSON.stringify({ authenticated: false }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    ) as typeof fetch)

    render(
      <PublicAdminClientGate>
        <button type="button">Admin edit</button>
      </PublicAdminClientGate>,
    )

    await waitFor(() => expect(fetch).toHaveBeenCalledWith('/api/auth/session', {
      credentials: 'include',
      cache: 'no-store',
    }))
    expect(screen.queryByRole('button', { name: 'Admin edit' })).not.toBeInTheDocument()
  })

  it('deduplicates browser session checks when a public page has multiple admin gates', async () => {
    const fetchMock = vi.fn(async () =>
      new Response(JSON.stringify({ authenticated: true, role: 'admin' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock as typeof fetch)

    render(
      <>
        <PublicAdminClientGate>
          <button type="button">Manage studies</button>
        </PublicAdminClientGate>
        <PublicAdminClientGate>
          <button type="button">Create study</button>
        </PublicAdminClientGate>
      </>,
    )

    expect(await screen.findByRole('button', { name: 'Manage studies' })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Create study' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
