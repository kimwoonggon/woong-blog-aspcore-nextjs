import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TiptapEditor } from '@/components/admin/TiptapEditor'
import { buildWorkVideoEmbedMarkup } from '@/lib/content/work-video-embeds'

const mocks = vi.hoisted(() => {
  type DropHandler = (view: unknown, event: DragEvent, slice: unknown, moved: boolean) => boolean | Promise<boolean>
  type PasteHandler = (view: unknown, event: ClipboardEvent) => boolean | Promise<boolean>
  type EditorConfig = {
    content?: string
    editorProps?: {
      handleDrop?: DropHandler
      handlePaste?: PasteHandler
    }
    extensions?: unknown[]
  }

  const state: {
    config: EditorConfig | null
    currentHtml: string
    setContent: (value: string) => void
    insertContent: (value: unknown) => void
    setImage: (value: string) => void
  } = {
    config: null,
    currentHtml: '',
    setContent: vi.fn<(value: string) => void>(),
    insertContent: vi.fn<(value: unknown) => void>(),
    setImage: vi.fn<(value: string) => void>(),
  }

  return {
    state,
    fetchWithCsrf: vi.fn(),
  }
})

vi.mock('@/lib/api/auth', () => ({
  fetchWithCsrf: mocks.fetchWithCsrf,
}))

vi.mock('@/lib/api/browser', () => ({
  getBrowserApiBaseUrl: () => '/api',
}))

vi.mock('@tiptap/react', () => {
  type ChainLike = {
    focus: () => ChainLike
    undo: () => ChainLike
    redo: () => ChainLike
    toggleHeading: (_args?: unknown) => ChainLike
    toggleBold: () => ChainLike
    toggleItalic: () => ChainLike
    toggleStrike: () => ChainLike
    toggleHighlight: (_args?: unknown) => ChainLike
    toggleBulletList: () => ChainLike
    toggleOrderedList: () => ChainLike
    toggleBlockquote: () => ChainLike
    toggleCodeBlock: () => ChainLike
    extendMarkRange: (_name?: string) => ChainLike
    unsetLink: () => ChainLike
    setLink: (_value?: unknown) => ChainLike
    insertContent: (value: unknown) => ChainLike
    setImage: (value: { src: string }) => ChainLike
    run: () => boolean
  }

  const createChain = () => {
    const chain: ChainLike = {
      focus: () => chain,
      undo: () => chain,
      redo: () => chain,
      toggleHeading: () => chain,
      toggleBold: () => chain,
      toggleItalic: () => chain,
      toggleStrike: () => chain,
      toggleHighlight: () => chain,
      toggleBulletList: () => chain,
      toggleOrderedList: () => chain,
      toggleBlockquote: () => chain,
      toggleCodeBlock: () => chain,
      extendMarkRange: () => chain,
      unsetLink: () => chain,
      setLink: () => chain,
      insertContent: (value: unknown) => {
        mocks.state.insertContent(value)
        if (typeof value === 'string') {
          mocks.state.currentHtml = `${mocks.state.currentHtml}${value}`
        }
        return chain
      },
      setImage: ({ src }: { src: string }) => {
        mocks.state.setImage(src)
        mocks.state.currentHtml = `${mocks.state.currentHtml}<img src="${src}" />`
        return chain
      },
      run: () => true,
    }

    return chain
  }

  const editor = {
    getHTML: () => mocks.state.currentHtml,
    view: {
      dom: document.createElement('div'),
    },
    commands: {
      setContent: (value: string) => {
        mocks.state.currentHtml = value
        mocks.state.setContent(value)
      },
    },
    on: vi.fn(),
    off: vi.fn(),
    chain: () => createChain(),
    can: () => ({
      undo: () => true,
      redo: () => true,
    }),
    isActive: () => false,
  }

  return {
    useEditor: (config: unknown) => {
      mocks.state.config = config as typeof mocks.state.config
      return editor
    },
    EditorContent: () => <div data-testid="mock-editor-content" />,
    BubbleMenu: ({ children }: { children: React.ReactNode }) => <div data-testid="mock-bubble-menu">{children}</div>,
  }
})

vi.mock('@tiptap/react/menus', () => ({
  BubbleMenu: ({ children }: { children: React.ReactNode }) => <div data-testid="mock-bubble-menu">{children}</div>,
}))

vi.mock('@tiptap/starter-kit', () => ({ default: { configure: () => ({ name: 'starter-kit' }) } }))
vi.mock('@tiptap/extension-image', () => ({ default: { configure: () => ({ name: 'image' }) } }))
vi.mock('@tiptap/extension-placeholder', () => ({ default: { configure: () => ({ name: 'placeholder' }) } }))
vi.mock('@tiptap/extension-highlight', () => ({ default: { configure: () => ({ name: 'highlight' }) } }))
vi.mock('@tiptap/extension-color', () => ({ default: { configure: () => ({ name: 'color' }) } }))
vi.mock('@tiptap/extension-link', () => ({ default: { configure: () => ({ name: 'link' }) } }))
vi.mock('@tiptap/extension-code-block-lowlight', () => ({ default: { configure: () => ({ name: 'code-block-lowlight' }) } }))
vi.mock('@tiptap/extension-text-style', () => ({ TextStyle: { name: 'text-style' } }))
vi.mock('lowlight', () => ({
  common: {},
  createLowlight: () => ({}),
}))
vi.mock('@/components/admin/tiptap/ThreeJsBlock', () => ({
  ThreeJsBlock: { name: 'three-js-block' },
}))
vi.mock('@/components/admin/tiptap/HtmlBlock', () => ({
  HtmlBlock: { name: 'html-block' },
}))
vi.mock('@/components/admin/tiptap/MermaidBlock', () => ({
  MermaidBlock: { name: 'mermaid-block' },
}))
vi.mock('@/components/admin/tiptap/ResizableImageBlock', () => ({
  ResizableImage: { configure: () => ({ name: 'resizable-image' }) },
}))
vi.mock('@/components/admin/tiptap/SlashCommand', () => ({
  SlashCommand: { configure: () => ({ name: 'slash-command' }) },
}))
vi.mock('@/components/admin/tiptap/Commands', () => ({
  suggestion: {},
}))
vi.mock('@/components/admin/tiptap/WorkVideoEmbedBlock', () => ({
  WorkVideoEmbedBlock: { configure: () => ({ name: 'work-video-embed-block' }) },
}))

describe('TiptapEditor', () => {
  const clearMock = (fn: unknown) => {
    ;(fn as { mockClear: () => void }).mockClear()
  }

  beforeEach(() => {
    mocks.state.config = null
    mocks.state.currentHtml = ''
    clearMock(mocks.state.setContent)
    clearMock(mocks.state.insertContent)
    clearMock(mocks.state.setImage)
    mocks.fetchWithCsrf.mockReset()
  })

  it('syncs incoming content changes into the editor instance', async () => {
    mocks.state.currentHtml = '<p>Initial</p>'

    const { rerender } = render(
      <TiptapEditor content="<p>Initial</p>" onChange={vi.fn()} />,
    )

    rerender(<TiptapEditor content="<p>Updated</p>" onChange={vi.fn()} />)

    await waitFor(() => {
      expect(mocks.state.setContent).toHaveBeenCalledWith('<p>Updated</p>')
    })
  })

  it('uploads dropped images and inserts the returned asset url', async () => {
    mocks.state.currentHtml = '<p>Initial</p>'
    mocks.fetchWithCsrf.mockResolvedValue({
      ok: true,
      json: async () => ({ url: '/uploads/dropped-image.png' }),
    })

    render(<TiptapEditor content="<p>Initial</p>" onChange={vi.fn()} />)

    const file = new File(['image'], 'dropped-image.png', { type: 'image/png' })

    await act(async () => {
      const handled = await mocks.state.config?.editorProps?.handleDrop?.(
        {},
        { dataTransfer: { files: [file] } } as unknown as DragEvent,
        null,
        false,
      )
      expect(handled).toBe(true)
    })

    await waitFor(() => {
      expect(mocks.fetchWithCsrf).toHaveBeenCalledWith(
        '/api/uploads',
        expect.objectContaining({
          method: 'POST',
          body: expect.any(FormData),
        }),
      )
    })

    expect(mocks.state.setImage).toHaveBeenCalledWith('/uploads/dropped-image.png')
  })

  it('keeps pasted mermaid fences out of the independent mermaid block path', async () => {
    render(<TiptapEditor content="<p>Initial</p>" onChange={vi.fn()} />)

    const clipboardData = {
      files: [],
      getData: (type: string) => type === 'text/plain'
        ? '```mermaid\nsequenceDiagram\n  User->>Frontend: Login\n```'
        : '',
    }

    await act(async () => {
      const handled = await mocks.state.config?.editorProps?.handlePaste?.(
        {},
        { clipboardData } as unknown as ClipboardEvent,
      )
      expect(handled).toBe(false)
    })

    expect(mocks.state.insertContent).not.toHaveBeenCalledWith(expect.objectContaining({ type: 'mermaidBlock' }))
  })

  it('inserts a work video embed and reports success when a requested video exists', async () => {
    mocks.state.currentHtml = '<p>Body</p>'
    const onVideoInsertHandled = vi.fn()

    render(
      <TiptapEditor
        content="<p>Body</p>"
        onChange={vi.fn()}
        workVideos={[{ id: 'video-1', sourceType: 'youtube', sourceKey: 'abc123', sortOrder: 0 }]}
        insertVideoEmbedRequest={{ videoId: 'video-1', nonce: 1 }}
        onVideoInsertHandled={onVideoInsertHandled}
      />,
    )

    await waitFor(() => {
      expect(mocks.state.insertContent).toHaveBeenCalledWith(buildWorkVideoEmbedMarkup('video-1'))
    })

    expect(onVideoInsertHandled).toHaveBeenCalledWith({ inserted: true })
  })

  it('reports duplicate insert attempts without mutating the editor html', async () => {
    mocks.state.currentHtml = `<p>Body</p>${buildWorkVideoEmbedMarkup('video-1')}`
    const onVideoInsertHandled = vi.fn()

    render(
      <TiptapEditor
        content={mocks.state.currentHtml}
        onChange={vi.fn()}
        workVideos={[{ id: 'video-1', sourceType: 'youtube', sourceKey: 'abc123', sortOrder: 0 }]}
        insertVideoEmbedRequest={{ videoId: 'video-1', nonce: 2 }}
        onVideoInsertHandled={onVideoInsertHandled}
      />,
    )

    await waitFor(() => {
      expect(onVideoInsertHandled).toHaveBeenCalledWith({ inserted: false, reason: 'duplicate' })
    })

    expect(mocks.state.insertContent).not.toHaveBeenCalled()
  })

  it('renders the authoring capability hint when editable', () => {
    render(<TiptapEditor content="<p>Body</p>" onChange={vi.fn()} />)

    expect(screen.getByTestId('tiptap-toolbar-hint')).toBeInTheDocument()
  })

  it('exposes a mermaid insert action in the toolbar and registers the mermaid block extension', () => {
    render(<TiptapEditor content="<p>Body</p>" onChange={vi.fn()} />)

    expect(mocks.state.config?.extensions).toEqual(expect.arrayContaining([
      expect.objectContaining({ name: 'mermaid-block' }),
    ]))

    fireEvent.click(screen.getByTitle('Insert Mermaid Diagram'))

    expect(mocks.state.insertContent).toHaveBeenCalledWith({ type: 'mermaidBlock' })
  })
})
