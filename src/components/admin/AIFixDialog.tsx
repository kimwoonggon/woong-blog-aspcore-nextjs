'use client'

import { useState, useEffect } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { TiptapEditor } from './TiptapEditor'
import { Loader2, Wand2, Check } from 'lucide-react'
import { toast } from 'sonner'
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from "@/components/ui/resizable"
import { Rnd } from 'react-rnd'
import { getErrorMessage } from '@/lib/error-message'
import { fetchWithCsrf } from '@/lib/api/auth'
import { Label } from '@/components/ui/label'
import { fetchAdminAiRuntimeConfigBrowser, getAdminAiErrorMessage, type AdminAiRuntimeConfig } from '@/lib/api/admin-ai'

interface AIFixDialogProps {
    content: string
    onApply: (fixedContent: string) => void
    apiEndpoint?: string
    title?: string
    extraBodyParams?: Record<string, unknown>
}

export function AIFixDialog({
    content,
    onApply,
    apiEndpoint = '/api/admin/ai/blog-fix',
    title = 'AI Content Fixer',
    extraBodyParams = {}
}: AIFixDialogProps) {
    const [open, setOpen] = useState(false)
    const [loading, setLoading] = useState(false)
    const [fixedContent, setFixedContent] = useState<string | null>(null)
    const [runtimeConfig, setRuntimeConfig] = useState<AdminAiRuntimeConfig | null>(null)
    const [codexModel, setCodexModel] = useState('gpt-5.4')
    const [codexReasoningEffort, setCodexReasoningEffort] = useState('medium')

    // Window state for Rnd
    const [windowState, setWindowState] = useState<{
        width: string | number
        height: string | number
        x: number
        y: number
    }>({
        width: '95vw',
        height: '95vh',
        x: 0,
        y: 0
    })

    // Setup initial centered position on mount/open
    useEffect(() => {
        if (open && typeof window !== 'undefined') {
            const width = window.innerWidth * 0.95
            const height = window.innerHeight * 0.95
            setWindowState({
                width,
                height,
                x: (window.innerWidth - width) / 2,
                y: (window.innerHeight - height) / 2
            })
        }
    }, [open])

    const [isMounted, setIsMounted] = useState(false)

    useEffect(() => {
        setIsMounted(true)
    }, [])

    useEffect(() => {
        if (!open) {
            return
        }

        let cancelled = false
        const savedModel = typeof window !== 'undefined' ? window.localStorage.getItem('admin-ai-codex-model') : null
        const savedReasoning = typeof window !== 'undefined' ? window.localStorage.getItem('admin-ai-codex-reasoning') : null

        void fetchAdminAiRuntimeConfigBrowser()
            .then((config: AdminAiRuntimeConfig) => {
                if (cancelled) {
                    return
                }

                setRuntimeConfig(config)
                setCodexModel(savedModel || config.codexModel || 'gpt-5.4')
                setCodexReasoningEffort(savedReasoning || config.codexReasoningEffort || 'medium')
            })
            .catch((error: unknown) => {
                if (!cancelled) {
                    toast.error(getErrorMessage(error, 'Failed to load AI runtime config'))
                }
            })

        return () => {
            cancelled = true
        }
    }, [open])

    const handleFix = async () => {
        setLoading(true)
        setFixedContent(null)
        try {
            const response = await fetchWithCsrf(apiEndpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    html: content,
                    codexModel,
                    codexReasoningEffort,
                    ...extraBodyParams
                }),
            })

            const contentType = response.headers.get('content-type') ?? ''
            const rawBody = await response.text()
            const data = contentType.includes('application/json')
                ? JSON.parse(rawBody) as { error?: string; fixedHtml?: string }
                : null

            if (!response.ok) {
                if (response.status === 504) {
                    throw new Error('AI fix timed out while waiting for the backend response. Please retry.')
                }

                throw new Error(getAdminAiErrorMessage(data, rawBody || 'Failed to fix content'))
            }

            if (!data?.fixedHtml) {
                throw new Error(getAdminAiErrorMessage(data, 'AI fix response did not include fixed HTML.'))
            }

            setFixedContent(data.fixedHtml)
        } catch (error: unknown) {
            toast.error(getErrorMessage(error, 'Failed to fix content'))
        } finally {
            setLoading(false)
        }
    }

    const handleApply = () => {
        if (fixedContent) {
            onApply(fixedContent)
            setOpen(false)
            setFixedContent(null)
            toast.success('AI changes applied successfully')
        }
    }

    if (!isMounted) {
        return (
            <Button variant="outline" size="sm" className="gap-2" type="button">
                <Wand2 size={16} />
                {title}
            </Button>
        )
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button variant="outline" size="sm" className="gap-2" type="button">
                    <Wand2 size={16} />
                    {title}
                </Button>
            </DialogTrigger>
            {/* 
                DialogContent acts as a full-screen transparent overlay.
                We force reset the default Radix centering/transforms to ensure reliable X/Y positioning for Rnd.
            */}
            <DialogContent
                className="!max-w-none !w-screen !h-screen !bg-transparent !border-none !shadow-none !p-0 !m-0 !translate-x-0 !translate-y-0 !top-0 !left-0 fixed z-50 flex items-start justify-start pointer-events-none"
                onInteractOutside={(e) => e.preventDefault()}
                onPointerDownOutside={(e) => e.preventDefault()}
                showCloseButton={false}
            >
                <Rnd
                    size={{ width: windowState.width, height: windowState.height }}
                    position={{ x: windowState.x, y: windowState.y }}
                    onDragStop={(e, d) => {
                        setWindowState(prev => ({ ...prev, x: d.x, y: d.y }))
                    }}
                    onResizeStop={(e, direction, ref, delta, position) => {
                        setWindowState({
                            width: ref.style.width,
                            height: ref.style.height,
                            ...position,
                        })
                    }}
                    dragHandleClassName="dialog-header-drag"
                    bounds="window"
                    minWidth={400}
                    minHeight={300}
                    className={`pointer-events-auto bg-background border rounded-lg shadow-lg flex flex-col overflow-hidden transition-opacity duration-200 ${
                        // Hide until centered position is calculated to avoid top-left jump
                        windowState.x === 0 && windowState.y === 0 ? 'opacity-0' : 'opacity-100'
                        }`}
                >
                    {/* Width 100% handle for dragging */}
                    <div className="dialog-header-drag cursor-move w-full">
                        <DialogHeader className="p-4 border-b shrink-0 pointer-events-none flex flex-row items-center justify-between space-y-0">
                            <DialogTitle className="flex items-center gap-2">
                                <Wand2 className="w-5 h-5" />
                                {title}
                            </DialogTitle>
                            <div className="flex items-center gap-2 pointer-events-auto">
                                <Button variant="ghost" size="sm" onClick={() => setOpen(false)}>
                                    Cancel
                                </Button>
                                {fixedContent && (
                                    <Button size="sm" onClick={handleApply} className="gap-2">
                                        <Check size={16} />
                                        Apply Changes
                                    </Button>
                                )}
                            </div>
                        </DialogHeader>
                    </div>

                    {/* Content Area - absolutely positioned */}
                    <div className="absolute top-[65px] left-0 right-0 bottom-0">
                        <div className="absolute left-4 top-3 z-10 flex flex-wrap items-center gap-2 rounded-full border border-border/80 bg-background/95 px-3 py-1.5 shadow-sm">
                            <span className="text-xs text-muted-foreground">Provider</span>
                            <span className="text-xs font-medium uppercase">{runtimeConfig?.provider ?? 'loading'}</span>
                            {runtimeConfig?.provider === 'codex' ? (
                                <>
                                    <Label htmlFor="codex-model" className="text-xs text-muted-foreground">Model</Label>
                                    <select
                                        id="codex-model"
                                        value={codexModel}
                                        onChange={(event) => {
                                            setCodexModel(event.target.value)
                                            if (typeof window !== 'undefined') {
                                                window.localStorage.setItem('admin-ai-codex-model', event.target.value)
                                            }
                                        }}
                                        className="bg-transparent text-xs outline-none"
                                    >
                                        {(runtimeConfig.allowedCodexModels || []).map((model: string) => (
                                            <option key={model} value={model}>{model}</option>
                                        ))}
                                    </select>
                                    <Label htmlFor="codex-reasoning" className="text-xs text-muted-foreground">Reasoning</Label>
                                    <select
                                        id="codex-reasoning"
                                        value={codexReasoningEffort}
                                        onChange={(event) => {
                                            setCodexReasoningEffort(event.target.value)
                                            if (typeof window !== 'undefined') {
                                                window.localStorage.setItem('admin-ai-codex-reasoning', event.target.value)
                                            }
                                        }}
                                        className="bg-transparent text-xs outline-none"
                                    >
                                        {(runtimeConfig.allowedCodexReasoningEfforts || []).map((effort: string) => (
                                            <option key={effort} value={effort}>{effort}</option>
                                        ))}
                                    </select>
                                </>
                            ) : null}
                        </div>
                        <ResizablePanelGroup orientation="horizontal" className="h-full w-full">
                            {/* Original Content */}
                            <ResizablePanel defaultSize={50} minSize={20} className="overflow-hidden">
                                <div className="flex flex-col h-full bg-gray-50/50 dark:bg-gray-900/50 overflow-hidden">
                                    <div className="p-3 text-sm font-medium text-gray-500 border-b bg-white dark:bg-gray-950 shrink-0">
                                        Original
                                    </div>
                                    <div className="flex-1 min-h-0 overflow-y-auto p-4">
                                        <div className="prose prose-sm dark:prose-invert max-w-none pointer-events-none opacity-80">
                                            <div dangerouslySetInnerHTML={{ __html: content }} />
                                        </div>
                                    </div>
                                </div>
                            </ResizablePanel>

                            <ResizableHandle withHandle />

                            {/* Fixed Content / Loading / Empty State */}
                            <ResizablePanel defaultSize={50} minSize={20} className="overflow-hidden">
                                <div className="flex flex-col h-full bg-white dark:bg-gray-950 relative overflow-hidden">
                                    <div className="p-3 text-sm font-medium text-blue-600 dark:text-blue-400 border-b bg-white dark:bg-gray-950 flex justify-between items-center shrink-0">
                                        <span>AI Fixed Version</span>
                                        {loading && <span className="text-xs animate-pulse">Processing...</span>}
                                    </div>

                                    <div className="flex-1 min-h-0 overflow-y-auto p-4">
                                        <div className="min-h-full">
                                            {loading ? (
                                                <div className="h-full flex flex-col items-center justify-center text-gray-400 gap-4">
                                                    <Loader2 className="w-8 h-8 animate-spin" />
                                                    <p className="text-sm">Analyzing and fixing content...</p>
                                                </div>
                                            ) : fixedContent ? (
                                                <TiptapEditor
                                                    content={fixedContent}
                                                    onChange={setFixedContent}
                                                    editable={false}
                                                />
                                            ) : (
                                                <div className="h-full flex flex-col items-center justify-center text-gray-400 gap-4">
                                                    <Wand2 className="w-12 h-12 opacity-20" />
                                                    <div className="text-center space-y-2">
                                                        <p>Ready to fix formatting?</p>
                                                        <Button onClick={handleFix}>
                                                            Start AI Fix
                                                        </Button>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </ResizablePanel>
                        </ResizablePanelGroup>
                    </div>
                </Rnd>
            </DialogContent>
        </Dialog>
    )
}
