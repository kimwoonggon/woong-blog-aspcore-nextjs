
import { Navbar } from "@/components/layout/Navbar"
import { Footer } from "@/components/layout/Footer"
import { fetchPublicSiteSettings } from "@/lib/api/site-settings"
import { fetchServerSession } from "@/lib/api/server"

export default async function PublicLayout({
    children,
}: {
    children: React.ReactNode
}) {
    const siteSettings = await fetchPublicSiteSettings()
    const session = await fetchServerSession()
    const ownerName = siteSettings?.ownerName || 'John Doe'

    return (
        <div className="flex min-h-screen flex-col font-sans">
            <Navbar ownerName={ownerName} session={session} />
            <main className="flex-1">{children}</main>
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
