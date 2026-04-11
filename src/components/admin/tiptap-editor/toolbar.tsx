import { useEffect, useState } from 'react'
import type { Editor } from '@tiptap/react'
import {
  Bold,
  Box,
  Code,
  Heading1,
  Heading2,
  Heading3,
  Highlighter,
  ImageIcon,
  Italic,
  Link as LinkIcon,
  List,
  ListOrdered,
  Quote,
  Redo,
  Strikethrough,
  Undo,
} from 'lucide-react'

export function EditorToolbar({
  editor,
  editable,
  addImage,
  setLink,
}: {
  editor: Editor
  editable: boolean
  addImage: () => void
  setLink: () => void
}) {
  if (!editable) {
    return null
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-1 border-b px-3 py-2 bg-gray-50 dark:bg-gray-900 border-gray-200 dark:border-gray-800">
        <ToolbarButton
          onClick={() => editor.chain().focus().undo().run()}
          disabled={!editor.can().undo()}
          title="Undo"
        >
          <Undo size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().redo().run()}
          disabled={!editor.can().redo()}
          title="Redo"
        >
          <Redo size={18} />
        </ToolbarButton>

        <div className="w-px h-6 bg-gray-300 dark:bg-gray-700 mx-1" />

        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
          active={editor.isActive('heading', { level: 1 })}
          title="Heading 1"
        >
          <Heading1 size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          active={editor.isActive('heading', { level: 2 })}
          title="Heading 2"
        >
          <Heading2 size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
          active={editor.isActive('heading', { level: 3 })}
          title="Heading 3"
        >
          <Heading3 size={18} />
        </ToolbarButton>

        <div className="w-px h-6 bg-gray-300 dark:bg-gray-700 mx-1" />

        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBold().run()}
          active={editor.isActive('bold')}
          title="Bold"
        >
          <Bold size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleItalic().run()}
          active={editor.isActive('italic')}
          title="Italic"
        >
          <Italic size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleStrike().run()}
          active={editor.isActive('strike')}
          title="Strikethrough"
        >
          <Strikethrough size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHighlight({ color: '#fef08a' }).run()}
          active={editor.isActive('highlight')}
          title="Highlight"
        >
          <Highlighter size={18} />
        </ToolbarButton>

        <div className="w-px h-6 bg-gray-300 dark:bg-gray-700 mx-1" />

        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBulletList().run()}
          active={editor.isActive('bulletList')}
          title="Bullet List"
        >
          <List size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
          active={editor.isActive('orderedList')}
          title="Numbered List"
        >
          <ListOrdered size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
          active={editor.isActive('blockquote')}
          title="Blockquote"
        >
          <Quote size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleCodeBlock().run()}
          active={editor.isActive('codeBlock')}
          title="Code Block"
        >
          <Code size={18} />
        </ToolbarButton>

        <div className="w-px h-6 bg-gray-300 dark:bg-gray-700 mx-1" />

        <ToolbarButton onClick={addImage} title="Insert Image">
          <ImageIcon size={18} />
        </ToolbarButton>
        <ToolbarButton
          onClick={setLink}
          active={editor.isActive('link')}
          title="Add Link"
        >
          <LinkIcon size={18} />
        </ToolbarButton>

        <div className="w-px h-6 bg-gray-300 dark:bg-gray-700 mx-1" />

        <ToolbarButton
          onClick={() => editor.chain().focus().insertContent({ type: 'threeJsBlock' }).run()}
          title="Insert 3D Model"
        >
          <Box size={18} />
        </ToolbarButton>

        <ToolbarButton
          onClick={() => editor.chain().focus().insertContent({ type: 'htmlBlock' }).run()}
          title="Insert HTML Widget"
        >
          <Code size={18} />
        </ToolbarButton>
      </div>
      <div
        data-testid="tiptap-capability-hint"
        className="border-b border-dashed border-sky-200 bg-sky-50/70 px-4 py-2 text-xs text-sky-900 dark:border-sky-900 dark:bg-sky-950/20 dark:text-sky-100"
      >
        Type <span className="font-semibold">/</span> for commands, use <span className="font-semibold">Code Block</span> for snippets, and drag/drop or paste images directly into the editor. HTML widgets and 3D blocks stay available from the toolbar.
      </div>
    </>
  )
}

export function EditorFormattingBubble({
  editor,
  editable,
  setLink,
}: {
  editor: Editor
  editable: boolean
  setLink: () => void
}) {
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    if (!editable) {
      return
    }

    const syncVisibility = () => {
      const selection = window.getSelection()
      if (!selection || selection.isCollapsed) {
        setIsVisible(false)
        return
      }

      const anchorNode = selection.anchorNode
      const focusNode = selection.focusNode
      const editorElement = editor.view.dom
      const containsSelection = Boolean(anchorNode && focusNode)
        && editorElement.contains(anchorNode)
        && editorElement.contains(focusNode)

      setIsVisible(containsSelection)
    }
    const handleBlur = () => setIsVisible(false)
    const editorElement = editor.view.dom

    editor.on('selectionUpdate', syncVisibility)
    editor.on('transaction', syncVisibility)
    editor.on('blur', handleBlur)
    document.addEventListener('selectionchange', syncVisibility)
    editorElement.addEventListener('mouseup', syncVisibility)
    editorElement.addEventListener('keyup', syncVisibility)

    return () => {
      editor.off('selectionUpdate', syncVisibility)
      editor.off('transaction', syncVisibility)
      editor.off('blur', handleBlur)
      document.removeEventListener('selectionchange', syncVisibility)
      editorElement.removeEventListener('mouseup', syncVisibility)
      editorElement.removeEventListener('keyup', syncVisibility)
    }
  }, [editable, editor])

  if (!editable) {
    return null
  }

  if (!isVisible) {
    return null
  }

  return (
    <div
      data-testid="editor-formatting-bubble"
      className="sticky top-2 z-20 mx-auto mb-2 flex w-fit items-center gap-1 rounded-lg border border-gray-700 bg-gray-900 px-2 py-1 text-white shadow-xl dark:border-gray-700 dark:bg-gray-950"
    >
      <BubbleButton
        onClick={() => editor.chain().focus().toggleBold().run()}
        active={editor.isActive('bold')}
      >
        <Bold size={16} />
      </BubbleButton>
      <BubbleButton
        onClick={() => editor.chain().focus().toggleItalic().run()}
        active={editor.isActive('italic')}
      >
        <Italic size={16} />
      </BubbleButton>
      <BubbleButton
        onClick={() => editor.chain().focus().toggleStrike().run()}
        active={editor.isActive('strike')}
      >
        <Strikethrough size={16} />
      </BubbleButton>
      <BubbleButton
        onClick={() => editor.chain().focus().toggleHighlight({ color: '#fef08a' }).run()}
        active={editor.isActive('highlight')}
      >
        <Highlighter size={16} />
      </BubbleButton>
      <div className="w-px h-4 bg-gray-600 mx-1" />
      <BubbleButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
        active={editor.isActive('heading', { level: 1 })}
      >
        <Heading1 size={16} />
      </BubbleButton>
      <BubbleButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        active={editor.isActive('heading', { level: 2 })}
      >
        <Heading2 size={16} />
      </BubbleButton>
      <BubbleButton onClick={setLink} active={editor.isActive('link')}>
        <LinkIcon size={16} />
      </BubbleButton>
    </div>
  )
}

function ToolbarButton({
  children,
  onClick,
  active,
  disabled,
  title,
}: {
  children: React.ReactNode
  onClick: () => void
  active?: boolean
  disabled?: boolean
  title?: string
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={title}
      className={`p-1.5 rounded transition-colors ${active
        ? 'bg-gray-200 text-gray-900 dark:bg-gray-700 dark:text-white'
        : 'text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-800'
      } ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
    >
      {children}
    </button>
  )
}

function BubbleButton({
  children,
  onClick,
  active,
}: {
  children: React.ReactNode
  onClick: () => void
  active?: boolean
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`p-1 rounded transition-colors ${active
        ? 'bg-blue-600 text-white'
        : 'text-gray-300 hover:bg-gray-700 hover:text-white'
      }`}
    >
      {children}
    </button>
  )
}
