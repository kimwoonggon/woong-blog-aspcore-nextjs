"use client"

import { useEditor, EditorContent } from '@tiptap/react'
import { BubbleMenu } from '@tiptap/react/menus'
import StarterKit from '@tiptap/starter-kit'
import Image from '@tiptap/extension-image'
import Placeholder from '@tiptap/extension-placeholder'
import Highlight from '@tiptap/extension-highlight'
import { TextStyle } from '@tiptap/extension-text-style'
import Color from '@tiptap/extension-color'
import Link from '@tiptap/extension-link'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
import { common, createLowlight } from 'lowlight'
import { ThreeJsBlock } from './tiptap/ThreeJsBlock'
import { HtmlBlock } from './tiptap/HtmlBlock'
import { SlashCommand } from './tiptap/SlashCommand'
import { suggestion } from './tiptap/Commands'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import {
    Bold,
    Italic,
    Strikethrough,
    Heading1,
    Heading2,
    Heading3,
    List,
    ListOrdered,
    Quote,
    Code,
    ImageIcon,
    Link as LinkIcon,
    Highlighter,
    Undo,
    Redo,
    Box
} from 'lucide-react'
import { useCallback, useEffect } from 'react'

interface TiptapEditorProps {
    content: string
    onChange: (html: string) => void
    placeholder?: string
    editable?: boolean
}

// Initialize lowlight for syntax highlighting with common languages only
const lowlight = createLowlight(common)

export function TiptapEditor({ content, onChange, placeholder = "Type '/' for commands, or just start writing...", editable = true }: TiptapEditorProps) {
    const editor = useEditor({
        extensions: [
            StarterKit.configure({
                heading: {
                    levels: [1, 2, 3],
                },
                codeBlock: false, // Disable default code block
            }),
            CodeBlockLowlight.configure({
                lowlight,
                HTMLAttributes: {
                    class: 'rounded-md bg-gray-900 text-gray-100 p-4 font-mono my-4',
                },
            }),
            Image.configure({
                inline: true,
                allowBase64: true,
                HTMLAttributes: {
                    class: 'max-w-full h-auto rounded-lg my-4',
                },
            }),
            Placeholder.configure({
                placeholder,
            }),
            Highlight.configure({
                multicolor: true,
            }),
            TextStyle,
            Color,
            Link.configure({
                openOnClick: false,
                HTMLAttributes: {
                    class: 'text-blue-600 underline cursor-pointer hover:text-blue-800',
                },
            }),
            ThreeJsBlock,
            HtmlBlock,
            SlashCommand.configure({
                suggestion,
            }),
        ],
        content,
        editorProps: {
            attributes: {
                class: 'prose prose-lg dark:prose-invert max-w-none min-h-[500px] focus:outline-none px-4 py-8 bg-white dark:bg-gray-950 rounded-b-lg border',
            },
            handleDrop: (view, event, slice, moved) => {
                if (!moved && event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files[0]) {
                    const file = event.dataTransfer.files[0]
                    if (file.type.startsWith('image/')) {
                        handleImageUpload(file)
                        return true
                    }
                }
                return false
            },
            handlePaste: (view, event) => {
                if (event.clipboardData && event.clipboardData.files && event.clipboardData.files[0]) {
                    const file = event.clipboardData.files[0]
                    if (file.type.startsWith('image/')) {
                        handleImageUpload(file)
                        return true
                    }
                }
                return false
            },
        },
        onUpdate: ({ editor }) => {
            onChange(editor.getHTML())
        },
        editable,
        immediatelyRender: false,
    })

    // Handle image upload
    const handleImageUpload = useCallback(async (file: File) => {
        if (!editor) return

        const formData = new FormData()
        formData.append('file', file)

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/uploads`, {
                method: 'POST',
                body: formData,
            })

            if (response.ok) {
                const data = await response.json()
                editor.chain().focus().setImage({ src: data.url }).run()
            }
        } catch (error) {
            console.error('Error uploading image:', error)
        }
    }, [editor])

    // Trigger file input for image upload
    const addImage = useCallback(() => {
        const input = document.createElement('input')
        input.type = 'file'
        input.accept = 'image/*'
        input.onchange = async (e) => {
            const file = (e.target as HTMLInputElement).files?.[0]
            if (file) {
                await handleImageUpload(file)
            }
        }
        input.click()
    }, [handleImageUpload])

    // Set link
    const setLink = useCallback(() => {
        if (!editor) return
        const previousUrl = editor.getAttributes('link').href
        const url = window.prompt('URL', previousUrl)

        if (url === null) return
        if (url === '') {
            editor.chain().focus().extendMarkRange('link').unsetLink().run()
            return
        }

        editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
    }, [editor])

    // Sync content changes from parent
    useEffect(() => {
        if (editor && content !== editor.getHTML()) {
            editor.commands.setContent(content)
        }
    }, [content, editor])

    if (!editor) return null

    return (
        <div className="rounded-lg bg-white dark:bg-gray-950 border dark:border-gray-800 shadow-sm overflow-hidden">
            {/* Toolbar */}
            {editable && (
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
            )}

            {/* Bubble Menu */}
            {editable && (
                <BubbleMenu
                    editor={editor}
                    className="flex items-center gap-1 bg-gray-900 dark:bg-gray-800 text-white rounded-lg shadow-xl px-2 py-1 border border-gray-700"
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
                </BubbleMenu>
            )}

            {/* Editor Content */}
            <div className="bg-white dark:bg-gray-950">
                <EditorContent editor={editor} />
            </div>
        </div>
    )
}

// Toolbar Button Component
function ToolbarButton({
    children,
    onClick,
    active,
    disabled,
    title
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

// Bubble Menu Button Component
function BubbleButton({
    children,
    onClick,
    active
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
