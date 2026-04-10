"use client"

import { EditorContent, useEditor } from '@tiptap/react'
import { useCallback, useEffect, useRef } from 'react'
import type { WorkVideo } from '@/lib/api/works'
import { createTiptapExtensions } from '@/components/admin/tiptap-editor/extensions'
import { handleVideoInsertRequest } from '@/components/admin/tiptap-editor/insert-controller'
import { EditorFormattingBubble, EditorToolbar } from '@/components/admin/tiptap-editor/toolbar'
import { uploadEditorImage } from '@/components/admin/tiptap-editor/upload'

interface TiptapEditorProps {
    content: string
    onChange: (html: string) => void
    placeholder?: string
    editable?: boolean
    workVideos?: WorkVideo[]
    insertVideoEmbedRequest?: { videoId: string; nonce: number } | null
    onVideoInsertHandled?: (result: { inserted: boolean; reason?: 'duplicate' | 'missing' }) => void
}

export function TiptapEditor({
    content,
    onChange,
    placeholder = "Type '/' for commands, or just start writing...",
    editable = true,
    workVideos = [],
    insertVideoEmbedRequest = null,
    onVideoInsertHandled,
}: TiptapEditorProps) {
    const workVideosRef = useRef(workVideos)
    const lastHandledInsertNonce = useRef<number | null>(null)

    useEffect(() => {
        workVideosRef.current = workVideos
    }, [workVideos])

    const resolveVideo = useCallback((videoId: string) => {
        return workVideosRef.current.find((video) => video.id === videoId) ?? null
    }, [])

    const editor = useEditor({
        // eslint-disable-next-line react-hooks/refs -- TipTap keeps this resolver for later callbacks, so it must read the latest video list after initialization.
        extensions: createTiptapExtensions({
            placeholder,
            resolveVideo,
        }),
        content,
        editorProps: {
            attributes: {
                class: 'prose prose-lg dark:prose-invert max-w-none min-h-[500px] focus:outline-none px-4 py-8 bg-white dark:bg-gray-950 rounded-b-lg border',
            },
            handleDrop: (_view, event, _slice, moved) => {
                if (!moved && event.dataTransfer?.files?.[0]?.type.startsWith('image/')) {
                    void handleImageUpload(event.dataTransfer.files[0])
                    return true
                }
                return false
            },
            handlePaste: (_view, event) => {
                if (event.clipboardData?.files?.[0]?.type.startsWith('image/')) {
                    void handleImageUpload(event.clipboardData.files[0])
                    return true
                }
                return false
            },
        },
        onUpdate: ({ editor: activeEditor }) => {
            onChange(activeEditor.getHTML())
        },
        editable,
        immediatelyRender: false,
    })

    const handleImageUpload = useCallback(async (file: File) => {
        if (!editor) return

        try {
            const imageUrl = await uploadEditorImage(file)
            editor.chain().focus().setImage({ src: imageUrl }).run()
        } catch (error) {
            console.error('Error uploading image:', error)
        }
    }, [editor])

    const addImage = useCallback(() => {
        const input = document.createElement('input')
        input.type = 'file'
        input.accept = 'image/*'
        input.onchange = async (event) => {
            const file = (event.target as HTMLInputElement).files?.[0]
            if (file) {
                await handleImageUpload(file)
            }
        }
        input.click()
    }, [handleImageUpload])

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

    useEffect(() => {
        if (editor && content !== editor.getHTML()) {
            editor.commands.setContent(content)
        }
    }, [content, editor])

    useEffect(() => {
        if (!editor || typeof window === 'undefined') {
            return
        }

        if (!/^(localhost|127\.0\.0\.1)$/i.test(window.location.hostname)) {
            return
        }

        const target = window as typeof window & { __WOONG_TIPTAP_EDITOR__?: typeof editor }
        target.__WOONG_TIPTAP_EDITOR__ = editor

        return () => {
            if (target.__WOONG_TIPTAP_EDITOR__ === editor) {
                delete target.__WOONG_TIPTAP_EDITOR__
            }
        }
    }, [editor])

    useEffect(() => {
        handleVideoInsertRequest({
            editor,
            insertVideoEmbedRequest,
            lastHandledInsertNonce,
            workVideosRef,
            onVideoInsertHandled,
        })
    }, [editor, insertVideoEmbedRequest, onVideoInsertHandled])

    if (!editor) return null

    return (
        <div
            className="rounded-lg bg-white dark:bg-gray-950 border dark:border-gray-800 shadow-sm overflow-hidden"
            onDragOver={(event) => {
                if (event.dataTransfer?.files?.[0]?.type.startsWith('image/')) {
                    event.preventDefault()
                }
            }}
            onDrop={(event) => {
                const file = event.dataTransfer?.files?.[0]
                if (file?.type.startsWith('image/')) {
                    event.preventDefault()
                    void handleImageUpload(file)
                }
            }}
        >
            <EditorToolbar
                editor={editor}
                editable={editable}
                addImage={addImage}
                setLink={setLink}
            />
            <EditorFormattingBubble
                editor={editor}
                editable={editable}
                setLink={setLink}
            />
            <div className="bg-white dark:bg-gray-950">
                <EditorContent editor={editor} />
            </div>
        </div>
    )
}
