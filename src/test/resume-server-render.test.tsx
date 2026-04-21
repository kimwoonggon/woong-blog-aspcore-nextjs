import { afterEach, describe, expect, it, vi } from 'vitest'

describe('Resume PDF viewer SSR isolation', () => {
  afterEach(() => {
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('does not import react-pdf when loading the ResumePdfViewer wrapper module', async () => {
    vi.doMock('react-pdf', () => {
      throw new Error('react-pdf should not be imported during resume server render')
    })

    const resumePdfViewerModule = await import('@/components/content/ResumePdfViewer')

    expect(resumePdfViewerModule.ResumePdfViewer).toBeTypeOf('function')
  })
})
