import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { PublicAdminClientGate } from '@/components/admin/PublicAdminClientGate'
import { ResumeEditor } from '@/components/admin/ResumeEditor'
import { ResumePdfViewer } from '@/components/content/ResumePdfViewer'
import { Button } from '@/components/ui/button'
import { Download } from 'lucide-react'
import { fetchResume } from '@/lib/api/site-settings'

export const revalidate = 60

interface PageProps {
    searchParams?: Promise<{ __qaEmpty?: string }>
}

export default async function ResumePage({ searchParams }: PageProps) {
    const resolvedSearchParams = await searchParams
    const qaEmptyResume = resolvedSearchParams?.__qaEmpty === '1'
    const resume = qaEmptyResume ? null : await fetchResume()
    const resumeUrl = resume?.publicUrl ?? null
    const resumeAsset = resume
        ? {
            id: resume.id,
            bucket: 'public-resume',
            path: resume.path,
        }
        : null

    return (
        <div
            data-testid="resume-shell"
            className="container mx-auto flex h-[calc(100vh-64px-72px)] flex-col px-4 py-7 md:px-6 md:py-10"
        >
            <header className="mb-5">
                <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
                    <div>
                        <h1 className="text-3xl font-heading font-bold text-foreground md:text-4xl">Resume</h1>
                    </div>
                    {resumeUrl && (
                        <div className="rounded-2xl border border-border/70 bg-muted/30 p-1">
                            <Button asChild>
                                <a href={resumeUrl} download>
                                    <Download className="mr-2 h-4 w-4" /> Download PDF
                                </a>
                            </Button>
                        </div>
                    )}
                </div>
            </header>

            <div className="flex-1 w-full overflow-hidden rounded-[2rem] border border-border/70 bg-muted/30 shadow-sm">
                {resumeUrl ? (
                    <ResumePdfViewer url={resumeUrl} />
                ) : (
                    <div className="flex h-full flex-col items-center justify-center gap-2 px-6 text-center">
                        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                            Resume unavailable
                        </p>
                        <p className="text-sm leading-relaxed text-muted-foreground">
                            No resume has been published yet. Use the contact page if you need one directly.
                        </p>
                    </div>
                )}
            </div>
            <PublicAdminClientGate>
                <InlineAdminEditorShell
                    triggerLabel="이력서 PDF 업로드"
                    title="Resume Inline Upload"
                    description="현재 페이지에서 바로 PDF를 업로드하거나 교체합니다."
                >
                    <ResumeEditor resumeAsset={resumeAsset} />
                </InlineAdminEditorShell>
            </PublicAdminClientGate>
        </div>
    )
}
