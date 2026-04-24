import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AdminLogoutButton } from '@/app/admin/AdminLogoutButton'

const mocks = vi.hoisted(() => ({
  logoutWithCsrf: vi.fn(),
}))

vi.mock('@/lib/api/auth', () => ({
  logoutWithCsrf: mocks.logoutWithCsrf,
}))

describe('AdminLogoutButton', () => {
  beforeEach(() => {
    mocks.logoutWithCsrf.mockReset()
  })

  it('calls logoutWithCsrf with root redirect target', async () => {
    mocks.logoutWithCsrf.mockRejectedValueOnce(new Error('test'))

    render(<AdminLogoutButton />)
    fireEvent.click(screen.getByTestId('admin-logout-button'))

    await waitFor(() => {
      expect(mocks.logoutWithCsrf).toHaveBeenCalledWith('/')
    })
  })

  it('prevents duplicate submissions while sign out is pending', async () => {
    let rejectPromise: ((reason?: unknown) => void) | undefined
    const pending = new Promise<string>((_, reject) => {
      rejectPromise = reject
    })
    mocks.logoutWithCsrf.mockReturnValueOnce(pending)

    render(<AdminLogoutButton />)
    const button = screen.getByTestId('admin-logout-button')
    fireEvent.click(button)
    fireEvent.click(button)

    expect(mocks.logoutWithCsrf).toHaveBeenCalledTimes(1)
    expect(button).toBeDisabled()

    rejectPromise?.(new Error('test'))
    await waitFor(() => expect(button).not.toBeDisabled())
  })
})
