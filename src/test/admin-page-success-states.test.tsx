import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: vi.fn(),
    replace: vi.fn(),
  }),
  usePathname: () => '/admin/blog',
  useSearchParams: () => new URLSearchParams(),
}))

describe('admin page success and not-found states', () => {
  afterEach(() => {
    cleanup()
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('renders dashboard stats and collections when every dependency succeeds', async () => {
    vi.doMock('@/lib/api/admin-dashboard', () => ({
      fetchAdminDashboardSummary: vi.fn(async () => ({
        worksCount: 3,
        blogsCount: 4,
        viewsCount: 99,
      })),
    }))
    vi.doMock('@/lib/api/works', () => ({
      fetchAdminWorks: vi.fn(async () => [{ id: 'work-1', title: 'Work', slug: 'work', published: true, category: 'platform', tags: [] }]),
    }))
    vi.doMock('@/lib/api/blogs', () => ({
      fetchAdminBlogs: vi.fn(async () => [{ id: 'blog-1', title: 'Blog', slug: 'blog', published: false, excerpt: 'excerpt', tags: [] }]),
    }))
    vi.doMock('@/lib/api/admin-pages', () => ({
      fetchAdminSiteSettings: vi.fn(async () => ({
        owner_name: 'Owner',
        tagline: 'Tagline',
        facebook_url: '',
        instagram_url: '',
        twitter_url: '',
        linkedin_url: '',
        github_url: '',
        resume_asset_id: 'resume-1',
        resume_asset: {
          id: 'resume-1',
          bucket: 'public-resume',
          path: 'public-resume/resume.pdf',
          public_url: '/media/public-resume/resume.pdf',
          file_name: 'resume.pdf',
        },
      })),
    }))
    vi.doMock('@/components/admin/AdminDashboardCollections', () => ({
      AdminDashboardCollections: ({ works, blogs }: { works: unknown[]; blogs: unknown[] }) => (
        <div data-testid="dashboard-collections">{works.length}:{blogs.length}</div>
      ),
    }))

    const DashboardPage = (await import('@/app/admin/dashboard/page')).default
    render(await DashboardPage())

    expect(screen.getByText('99')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
    expect(screen.getByText('4')).toBeInTheDocument()
    expect(screen.getByText('resume.pdf')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Download Resume/i })).toHaveAttribute('href', '/media/public-resume/resume.pdf')
    expect(screen.getByRole('link', { name: /Manage Resume/i })).toHaveAttribute('href', '/admin/pages#resume-editor')
    expect(screen.getByTestId('dashboard-collections')).toHaveTextContent('1:1')
  }, 15000)

  it('renders the dashboard list error when content collections fail to load', async () => {
    vi.doMock('@/lib/api/admin-dashboard', () => ({
      fetchAdminDashboardSummary: vi.fn(async () => ({
        worksCount: 1,
        blogsCount: 1,
        viewsCount: 1,
      })),
    }))
    vi.doMock('@/lib/api/works', () => ({
      fetchAdminWorks: vi.fn(async () => {
        throw new Error('failed')
      }),
    }))
    vi.doMock('@/lib/api/blogs', () => ({
      fetchAdminBlogs: vi.fn(async () => []),
    }))
    vi.doMock('@/lib/api/admin-pages', () => ({
      fetchAdminSiteSettings: vi.fn(async () => {
        throw new Error('failed')
      }),
    }))

    const DashboardPage = (await import('@/app/admin/dashboard/page')).default
    render(await DashboardPage())

    expect(screen.getAllByText('Dashboard content lists are unavailable')[0]).toBeInTheDocument()
    expect(screen.getByText('Resume status is unavailable')).toBeInTheDocument()
  }, 15000)

  it('renders a populated admin blog table when blogs load successfully', async () => {
    vi.doMock('@/lib/api/blogs', () => ({
      fetchAdminBlogs: vi.fn(async () => [{
        id: 'blog-1',
        title: 'First blog',
        slug: 'first-blog',
        published: true,
        publishedAt: '2024-01-01T00:00:00.000Z',
        tags: ['tag-a', 'tag-b'],
        excerpt: 'excerpt',
      }]),
    }))
    vi.doMock('@/components/admin/DeleteButton', () => ({
      DeleteButton: () => <button type="button">Delete</button>,
    }))
    vi.doMock('@/app/admin/blog/actions', () => ({
      deleteBlog: vi.fn(),
    }))

    const AdminBlogPage = (await import('@/app/admin/blog/page')).default
    render(await AdminBlogPage({ searchParams: Promise.resolve({}) }))

    expect(screen.getByText('First blog')).toBeInTheDocument()
    expect(screen.getByText('Published')).toBeInTheDocument()
    expect(screen.getByText('tag-a, tag-b')).toBeInTheDocument()
    expect(screen.getByText(/batch-selection scaffolding/i)).toBeInTheDocument()
  }, 30000)

  it('renders draft blog rows without published dates or tags', async () => {
    vi.doMock('@/lib/api/blogs', () => ({
      fetchAdminBlogs: vi.fn(async () => [{
        id: 'blog-1',
        title: 'Draft blog',
        slug: 'draft-blog',
        published: false,
        publishedAt: null,
        tags: [],
        excerpt: 'excerpt',
      }]),
    }))
    vi.doMock('@/components/admin/DeleteButton', () => ({
      DeleteButton: () => <button type="button">Delete</button>,
    }))
    vi.doMock('@/app/admin/blog/actions', () => ({
      deleteBlog: vi.fn(),
    }))

    const AdminBlogPage = (await import('@/app/admin/blog/page')).default
    render(await AdminBlogPage({ searchParams: Promise.resolve({}) }))

    expect(screen.getByText('Draft blog')).toBeInTheDocument()
    expect(screen.getByText('Draft')).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
  }, 30000)

  it('renders a populated admin members table when members load successfully', async () => {
    vi.doMock('@/lib/api/admin-members', () => ({
      fetchAdminMembers: vi.fn(async () => [{
        id: 'member-1',
        displayName: 'Admin User',
        email: 'admin@example.com',
        role: 'admin',
        provider: 'google',
        createdAt: '2024-01-01T00:00:00.000Z',
        lastLoginAt: '2024-01-02T00:00:00.000Z',
        activeSessionCount: 1,
      }]),
    }))

    const AdminMembersPage = (await import('@/app/admin/members/page')).default
    render(await AdminMembersPage())

    expect(screen.getByText('Admin User')).toBeInTheDocument()
    expect(screen.getByText('admin@example.com')).toBeInTheDocument()
    expect(screen.getByText('google')).toBeInTheDocument()
    expect(screen.getByText('1')).toBeInTheDocument()
  }, 15000)

  it('renders an empty-state admin works table when no works exist', async () => {
    vi.doMock('@/lib/api/works', () => ({
      fetchAdminWorks: vi.fn(async () => []),
    }))
    vi.doMock('@/components/admin/DeleteButton', () => ({
      DeleteButton: () => <button type="button">Delete</button>,
    }))
    vi.doMock('@/app/admin/works/actions', () => ({
      deleteWork: vi.fn(),
    }))

    const AdminWorksPage = (await import('@/app/admin/works/page')).default
    render(await AdminWorksPage({ searchParams: Promise.resolve({}) }))

    expect(screen.getByText('No works found.')).toBeInTheDocument()
  })

  it('renders populated admin works rows for published and draft items', async () => {
    vi.doMock('@/lib/api/works', () => ({
      fetchAdminWorks: vi.fn(async () => [
        {
          id: 'work-1',
          title: 'Published work',
          slug: 'published-work',
          published: true,
          publishedAt: '2024-01-01T00:00:00.000Z',
          category: 'platform',
          tags: [],
        },
        {
          id: 'work-2',
          title: 'Draft work',
          slug: 'draft-work',
          published: false,
          publishedAt: null,
          category: 'experiment',
          tags: [],
        },
      ]),
    }))
    vi.doMock('@/components/admin/DeleteButton', () => ({
      DeleteButton: () => <button type="button">Delete</button>,
    }))
    vi.doMock('@/app/admin/works/actions', () => ({
      deleteWork: vi.fn(),
    }))

    const AdminWorksPage = (await import('@/app/admin/works/page')).default
    render(await AdminWorksPage({ searchParams: Promise.resolve({}) }))

    expect(screen.getByText('Published work')).toBeInTheDocument()
    expect(screen.getByText('Draft work')).toBeInTheDocument()
    expect(screen.getByText('platform')).toBeInTheDocument()
    expect(screen.getByText('experiment')).toBeInTheDocument()
    expect(screen.getByText('Published')).toBeInTheDocument()
    expect(screen.getByText('Draft')).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it('renders all admin page editors when pages and settings load', async () => {
    vi.doMock('@/lib/api/admin-pages', () => ({
      fetchAdminSiteSettings: vi.fn(async () => ({
        owner_name: 'Owner',
        tagline: 'Tagline',
        facebook_url: '',
        instagram_url: '',
        twitter_url: '',
        linkedin_url: '',
        github_url: '',
        resume_asset_id: 'resume-1',
        resume_asset: {
          id: 'resume-1',
          bucket: 'public-resume',
          path: 'public-resume/resume.pdf',
          public_url: '/media/public-resume/resume.pdf',
          file_name: 'resume.pdf',
        },
      })),
      fetchAdminPages: vi.fn(async () => [
        { id: 'page-home', title: 'Home', slug: 'home', content: { headline: 'Hi' } },
        { id: 'page-intro', title: 'Introduction', slug: 'introduction', content: { html: '<p>Intro</p>' } },
        { id: 'page-contact', title: 'Contact', slug: 'contact', content: { html: '<p>Contact</p>' } },
      ]),
    }))
    vi.doMock('@/components/admin/SiteSettingsEditor', () => ({
      SiteSettingsEditor: ({ initialSettings }: { initialSettings: { owner_name: string } }) => (
        <div>Site settings for {initialSettings.owner_name}</div>
      ),
    }))
    vi.doMock('@/components/admin/HomePageEditor', () => ({
      HomePageEditor: ({ pageTitle }: { pageTitle: string }) => <div>Home editor: {pageTitle}</div>,
    }))
    vi.doMock('@/components/admin/PageEditor', () => ({
      PageEditor: ({ page }: { page: { title: string } }) => <div>Page editor: {page.title}</div>,
    }))
    vi.doMock('@/components/admin/ResumeEditor', () => ({
      ResumeEditor: ({ resumeAsset }: { resumeAsset: { id: string; path: string } | null }) => (
        <div>Resume editor: {resumeAsset ? `${resumeAsset.id}:${resumeAsset.path}` : 'none'}</div>
      ),
    }))

    const AdminPagesPage = (await import('@/app/admin/pages/page')).default
    render(await AdminPagesPage())

    expect(screen.getByText('Site settings for Owner')).toBeInTheDocument()
    expect(screen.getByText('Home editor: Home')).toBeInTheDocument()
    expect(screen.getByText('Page editor: Introduction')).toBeInTheDocument()
    expect(screen.getByText('Page editor: Contact')).toBeInTheDocument()
    expect(screen.getByText('Resume editor: resume-1:public-resume/resume.pdf')).toBeInTheDocument()
  })

  it('renders only the available sections when optional admin pages are missing', async () => {
    vi.doMock('@/lib/api/admin-pages', () => ({
      fetchAdminSiteSettings: vi.fn(async () => ({
        owner_name: '',
        tagline: '',
        facebook_url: '',
        instagram_url: '',
        twitter_url: '',
        linkedin_url: '',
        github_url: '',
        resume_asset_id: null,
        resume_asset: null,
      })),
      fetchAdminPages: vi.fn(async () => []),
    }))
    vi.doMock('@/components/admin/SiteSettingsEditor', () => ({
      SiteSettingsEditor: ({ initialSettings }: { initialSettings: { owner_name: string; tagline: string } }) => (
        <div>Site settings fallback: {initialSettings.owner_name}/{initialSettings.tagline}</div>
      ),
    }))
    vi.doMock('@/components/admin/HomePageEditor', () => ({
      HomePageEditor: () => <div>unused home editor</div>,
    }))
    vi.doMock('@/components/admin/PageEditor', () => ({
      PageEditor: () => <div>unused page editor</div>,
    }))
    vi.doMock('@/components/admin/ResumeEditor', () => ({
      ResumeEditor: ({ resumeAsset }: { resumeAsset: { id: string; path: string } | null }) => (
        <div>Resume editor: {resumeAsset ? `${resumeAsset.id}:${resumeAsset.path}` : 'none'}</div>
      ),
    }))

    const AdminPagesPage = (await import('@/app/admin/pages/page')).default
    render(await AdminPagesPage())

    expect(screen.getByText('Site settings fallback: John Doe/Creative Technologist')).toBeInTheDocument()
    expect(screen.getByText('Resume editor: none')).toBeInTheDocument()
    expect(screen.queryByText('unused home editor')).not.toBeInTheDocument()
    expect(screen.queryByText('unused page editor')).not.toBeInTheDocument()
  })

  it('renders the blog editor when an admin blog is found', async () => {
    vi.doMock('@/lib/api/blogs', () => ({
      fetchAdminBlogById: vi.fn(async () => ({ id: 'blog-1', title: 'Blog title' })),
    }))
    vi.doMock('@/components/admin/BlogEditor', () => ({
      BlogEditor: ({ initialBlog }: { initialBlog: { title: string } }) => (
        <div>Blog editor: {initialBlog.title}</div>
      ),
    }))

    const EditBlogPage = (await import('@/app/admin/blog/[id]/page')).default
    render(await EditBlogPage({ params: Promise.resolve({ id: 'blog-1' }) }))

    expect(screen.getByText('Blog editor: Blog title')).toBeInTheDocument()
  })

  it('calls notFound when the admin blog does not exist', async () => {
    const notFound = vi.fn(() => {
      throw new Error('NEXT_NOT_FOUND')
    })

    vi.doMock('@/lib/api/blogs', () => ({
      fetchAdminBlogById: vi.fn(async () => null),
    }))
    vi.doMock('next/navigation', () => ({
      notFound,
    }))
    vi.doMock('@/components/admin/BlogEditor', () => ({
      BlogEditor: () => <div>unused</div>,
    }))

    const EditBlogPage = (await import('@/app/admin/blog/[id]/page')).default

    await expect(EditBlogPage({ params: Promise.resolve({ id: 'missing-blog' }) })).rejects.toThrow('NEXT_NOT_FOUND')
    expect(notFound).toHaveBeenCalled()
  })

  it('renders the work editor when an admin work is found', async () => {
    vi.doMock('@/lib/api/works', () => ({
      fetchAdminWorkById: vi.fn(async () => ({ id: 'work-1', title: 'Work title' })),
    }))
    vi.doMock('@/components/admin/WorkEditor', () => ({
      WorkEditor: ({ initialWork }: { initialWork: { title: string } }) => (
        <div>Work editor: {initialWork.title}</div>
      ),
    }))

    const EditWorkPage = (await import('@/app/admin/works/[id]/page')).default
    render(await EditWorkPage({ params: Promise.resolve({ id: 'work-1' }) }))

    expect(screen.getByText('Work editor: Work title')).toBeInTheDocument()
  })

  it('calls notFound when the admin work does not exist', async () => {
    const notFound = vi.fn(() => {
      throw new Error('NEXT_NOT_FOUND')
    })

    vi.doMock('@/lib/api/works', () => ({
      fetchAdminWorkById: vi.fn(async () => null),
    }))
    vi.doMock('next/navigation', () => ({
      notFound,
    }))
    vi.doMock('@/components/admin/WorkEditor', () => ({
      WorkEditor: () => <div>unused</div>,
    }))

    const EditWorkPage = (await import('@/app/admin/works/[id]/page')).default

    await expect(EditWorkPage({ params: Promise.resolve({ id: 'missing-work' }) })).rejects.toThrow('NEXT_NOT_FOUND')
    expect(notFound).toHaveBeenCalled()
  })
})
