
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
            <Navbar ownerName={ownerName} />
            <main id="main-content" tabIndex={-1} className="flex-1">{children}</main>
            <Footer
                ownerName={ownerName}
                facebookUrl={siteSettings?.facebookUrl || ''}
                instagramUrl={siteSettings?.instagramUrl || ''}
                twitterUrl={siteSettings?.twitterUrl || ''}
                linkedinUrl={siteSettings?.linkedInUrl || ''}
                githubUrl={siteSettings?.gitHubUrl || ''}
            />
        </div>
    )
}
