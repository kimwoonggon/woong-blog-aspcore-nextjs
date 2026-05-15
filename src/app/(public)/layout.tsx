
import { Suspense } from "react"
import { Navbar } from "@/components/layout/Navbar"
import { Footer } from "@/components/layout/Footer"
import { SkipToMainLink } from "@/components/layout/SkipToMainLink"
import { fetchPublicSiteSettings } from "@/lib/api/site-settings"

export default async function PublicLayout({
    children,
}: {
    children: React.ReactNode
}) {
    const siteSettings = await fetchPublicSiteSettings()
    const ownerName = siteSettings?.ownerName || 'John Doe'

    return (
        <div className="flex min-h-screen flex-col font-sans">
            <SkipToMainLink />
            <Suspense fallback={<div className="h-16 border-b bg-background/95 lg:h-20" />}>
                <Navbar ownerName={ownerName} />
            </Suspense>
            <main
                id="main-content"
                tabIndex={-1}
                className="safe-area-main-bottom flex-1"
            >
                {children}
            </main>
            <Footer
                ownerName={ownerName}
                facebookUrl={siteSettings?.facebookUrl || ''}
                instagramUrl={siteSettings?.instagramUrl || ''}
                twitterUrl={siteSettings?.twitterUrl || ''}
                linkedinUrl={siteSettings?.linkedInUrl || ''}
                githubUrl={siteSettings?.gitHubUrl || ''}
                className="safe-area-footer-bottom"
            />
        </div>
    )
}
