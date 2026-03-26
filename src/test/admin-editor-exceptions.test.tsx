import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ResumeEditor } from '@/components/admin/ResumeEditor'
import { HomePageEditor } from '@/components/admin/HomePageEditor'
import { SiteSettingsEditor } from '@/components/admin/SiteSettingsEditor'

const mocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  push: vi.fn(),
  back: vi.fn(),
  fetchWithCsrf: vi.fn(),
  toast: {
    error: vi.fn(),
    success: vi.fn(),
    loading: vi.fn(() => 'toast-id'),
  },
}))

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: mocks.refresh, push: mocks.push, back: mocks.back }),
}))

vi.mock('sonner', () => ({ toast: mocks.toast }))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

describe('Admin editor exception handling', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.fetchWithCsrf.mockReset()
    vi.stubGlobal('alert', vi.fn())
    vi.stubGlobal('confirm', vi.fn(() => true))
  })

  it('ResumeEditor rejects non-pdf uploads before calling fetch', async () => {
    render(<ResumeEditor resumeAsset={null} />)

    const fileInput = document.querySelector('#resume-upload') as HTMLInputElement
    const file = new File(['hello'], 'notes.txt', { type: 'text/plain' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(mocks.toast.error).toHaveBeenCalledWith('Please upload a PDF file.')
    })
    expect(mocks.fetchWithCsrf).not.toHaveBeenCalled()
  })

  it('HomePageEditor alerts when image upload fails', async () => {
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: false,
      json: async () => ({ error: 'bad upload' }),
    } satisfies Partial<Response>)

    render(<HomePageEditor pageId="home-id" pageTitle="Home" initialContent={{}} />)

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['img'], 'avatar.png', { type: 'image/png' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Failed to upload image: bad upload')
    })
  })

  it('HomePageEditor ignores empty upload events', async () => {
    render(<HomePageEditor pageId="home-id" pageTitle="Home" initialContent={{}} />)

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    fireEvent.change(fileInput, { target: { files: [] } })

    expect(mocks.fetchWithCsrf).not.toHaveBeenCalled()
  })

  it('HomePageEditor stores uploaded image url after a successful upload', async () => {
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      json: async () => ({ url: '/media/public-assets/avatar.png' }),
    } satisfies Partial<Response>)

    render(<HomePageEditor pageId="home-id" pageTitle="Home" initialContent={{}} />)

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['img'], 'avatar.png', { type: 'image/png' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalled()
    })
    await waitFor(() => {
      expect(screen.getByAltText('Profile')).toBeInTheDocument()
    })
  })

  it('SiteSettingsEditor alerts when save fails', async () => {
    mocks.fetchWithCsrf.mockResolvedValue({ ok: false } satisfies Partial<Response>)

    render(
      <SiteSettingsEditor
        initialSettings={{ owner_name: 'Owner', tagline: 'Tagline' }}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Failed to save settings')
    })
    expect(mocks.refresh).not.toHaveBeenCalled()
  })

  it('HomePageEditor saves successfully and lets the user remove an uploaded image', async () => {
    mocks.fetchWithCsrf
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ url: '/media/public-assets/avatar.png' }),
      } satisfies Partial<Response>)
      .mockResolvedValueOnce({ ok: true } satisfies Partial<Response>)

    render(<HomePageEditor pageId="home-id" pageTitle="Home" initialContent={{ headline: 'Hello' }} />)

    fireEvent.change(screen.getByLabelText(/Headline/i), {
      target: { value: 'Updated headline' },
    })
    fireEvent.change(screen.getByLabelText(/Intro Text/i), {
      target: { value: 'Updated intro' },
    })

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['img'], 'avatar.png', { type: 'image/png' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Remove Image/i })).toBeInTheDocument()
    })

    fireEvent.click(screen.getByRole('button', { name: /Remove Image/i }))
    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Home page saved successfully!')
    })
    expect(mocks.fetchWithCsrf).toHaveBeenLastCalledWith(
      '/api/admin/pages',
      expect.objectContaining({
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          id: 'home-id',
          title: 'Home',
          contentJson: JSON.stringify({
            headline: 'Updated headline',
            introText: 'Updated intro',
            profileImageUrl: '',
          }),
        }),
      }),
    )
    expect(mocks.refresh).toHaveBeenCalled()
  })

  it('SiteSettingsEditor saves successfully and maps the linkedIn field name for the backend payload', async () => {
    mocks.fetchWithCsrf.mockResolvedValue({ ok: true } satisfies Partial<Response>)

    render(
      <SiteSettingsEditor
        initialSettings={{
          owner_name: 'Owner',
          tagline: 'Tagline',
          facebook_url: '',
          instagram_url: '',
          twitter_url: '',
          linkedin_url: 'https://old.example.com',
          github_url: '',
        }}
      />,
    )

    fireEvent.change(screen.getByLabelText(/Site Owner Name/i), {
      target: { value: 'Updated owner' },
    })
    fireEvent.change(screen.getByLabelText(/Tagline \/ Role/i), {
      target: { value: 'Updated role' },
    })
    fireEvent.change(screen.getByLabelText(/Facebook URL/i), {
      target: { value: 'https://facebook.com/owner' },
    })
    fireEvent.change(screen.getByLabelText(/Instagram URL/i), {
      target: { value: 'https://instagram.com/owner' },
    })
    fireEvent.change(screen.getByLabelText(/Twitter URL/i), {
      target: { value: 'https://twitter.com/owner' },
    })
    fireEvent.change(screen.getByLabelText(/LinkedIn URL/i), {
      target: { value: 'https://linkedin.com/in/owner' },
    })
    fireEvent.change(screen.getByLabelText(/GitHub URL/i), {
      target: { value: 'https://github.com/owner' },
    })
    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Site settings saved successfully!')
    })
    const [, requestInit] = mocks.fetchWithCsrf.mock.calls[0]
    expect(requestInit).toMatchObject({
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
    })
    expect(JSON.parse(requestInit.body as string)).toMatchObject({
      ownerName: 'Updated owner',
      tagline: 'Updated role',
      facebookUrl: 'https://facebook.com/owner',
      instagramUrl: 'https://instagram.com/owner',
      twitterUrl: 'https://twitter.com/owner',
      linkedInUrl: 'https://linkedin.com/in/owner',
      githubUrl: 'https://github.com/owner',
    })
    expect(mocks.refresh).toHaveBeenCalled()
  })

  it('SiteSettingsEditor falls back to default owner and tagline values when initial settings are empty', () => {
    render(
      <SiteSettingsEditor
        initialSettings={{
          owner_name: '',
          tagline: '',
          facebook_url: '',
          instagram_url: '',
          twitter_url: '',
          linkedin_url: '',
          github_url: '',
        }}
      />,
    )

    expect(screen.getByLabelText(/Site Owner Name/i)).toHaveValue('John Doe')
    expect(screen.getByLabelText(/Tagline \/ Role/i)).toHaveValue('Creative Technologist')
  })

  it('SiteSettingsEditor alerts when the save request throws', async () => {
    mocks.fetchWithCsrf.mockRejectedValueOnce(new Error('network down'))

    render(
      <SiteSettingsEditor
        initialSettings={{ owner_name: 'Owner', tagline: 'Tagline' }}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Failed to save settings')
    })
  })

  it('HomePageEditor alerts when save fails or throws', async () => {
    mocks.fetchWithCsrf
      .mockResolvedValueOnce({ ok: false } satisfies Partial<Response>)
      .mockRejectedValueOnce(new Error('network down'))

    render(<HomePageEditor pageId="home-id" pageTitle="Home" initialContent={{ profileImageUrl: '/media/avatar.png' }} />)

    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))
    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Failed to save')
    })

    fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))
    await waitFor(() => {
      expect(alert).toHaveBeenCalledTimes(2)
    })
  })

  it('HomePageEditor alerts when the upload request throws', async () => {
    mocks.fetchWithCsrf.mockRejectedValueOnce('upload failed')

    render(<HomePageEditor pageId="home-id" pageTitle="Home" initialContent={{}} />)

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['img'], 'avatar.png', { type: 'image/png' })
    fireEvent.change(fileInput, { target: { files: [file] } })

    await waitFor(() => {
      expect(alert).toHaveBeenCalledWith('Failed to upload image')
    })
  })
})
