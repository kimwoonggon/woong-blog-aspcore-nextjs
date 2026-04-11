
import { InlineAdminEditorShell } from '@/components/admin/InlineAdminEditorShell'
import { ResumeEditor } from '@/components/admin/ResumeEditor'
import { Button } from '@/components/ui/button'
import { fetchServerSession } from '@/lib/api/server'
import { Download } from 'lucide-react'
import { fetchResume } from '@/lib/api/site-settings'

export const revalidate = 60
export const dynamic = 'force-dynamic'

export default async function ResumePage() {
    const resume = await fetchResume()
    const session = await fetchServerSession()
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
            className="container mx-auto flex h-[calc(100vh-64px-72px)] flex-col px-4 py-8 md:px-6 md:py-12"
        >
            <div className="flex items-center justify-between mb-6">
                <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">Resume</h1>
                {resumeUrl && (
                    <Button asChild>
                        <a href={resumeUrl} download>
                            <Download className="mr-2 h-4 w-4" /> Download
                        </a>
                    </Button>
                )}
            </div>

            <div className="flex-1 w-full bg-gray-100 rounded-lg overflow-hidden border border-gray-200 dark:bg-gray-800 dark:border-gray-700">
                {resumeUrl ? (
                    <iframe
                        src={`${resumeUrl}#toolbar=0`}
                        className="h-full w-full"
                        title="Resume PDF"
                    />
                ) : (
                    <div className="flex h-full items-center justify-center text-gray-500">
                        No resume uploaded yet.
                    </div>
                )}
            </div>
            {session.authenticated && session.role === 'admin' && (
                <InlineAdminEditorShell
                    triggerLabel="이력서 PDF 업로드"
                    title="Resume Inline Upload"
                    description="현재 페이지에서 바로 PDF를 업로드하거나 교체합니다."
                >
                    <ResumeEditor resumeAsset={resumeAsset} />
                </InlineAdminEditorShell>
            )}
        </div>
    )
}
