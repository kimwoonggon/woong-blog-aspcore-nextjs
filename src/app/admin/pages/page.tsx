import Link from 'next/link'
import { AdminErrorPanel } from '@/components/admin/AdminErrorPanel'
import { HomePageEditor } from '@/components/admin/HomePageEditor'
import { PageEditor } from '@/components/admin/PageEditor'
import { ResumeEditor } from '@/components/admin/ResumeEditor'
import { SiteSettingsEditor } from '@/components/admin/SiteSettingsEditor'
import { Button } from '@/components/ui/button'
import {
    fetchAdminPages,
    fetchAdminSiteSettings,
    type AdminPageRecord,
    type AdminSiteSettings,
} from '@/lib/api/admin-pages'
import { fetchResume } from '@/lib/api/site-settings'
import { isHtmlPageContent, toHomeContent } from '@/lib/content/page-content'

export const revalidate = 0

export default async function AdminPagesPage() {
    let siteSettings: AdminSiteSettings | null = null
    let pages: AdminPageRecord[] = []
    let resume = null
    let loadFailed = false

    try {
        siteSettings = await fetchAdminSiteSettings()
        pages = await fetchAdminPages(['home', 'introduction', 'contact'])
        resume = await fetchResume()
    } catch {
        loadFailed = true
    }

    const resumeAsset = resume
        ? {
            id: resume.id,
            bucket: 'public-resume',
            path: resume.path,
        }
        : null

    const homePage = pages.find((p) => p.slug === 'home')
    const introPage = pages.find((p) => p.slug === 'introduction')
    const contactPage = pages.find((p) => p.slug === 'contact')
    const sectionLinks = [
        { href: '#site-settings-editor', label: 'Edit site settings' },
        { href: '#home-page-editor', label: 'Edit home page' },
        { href: '#introduction-page-editor', label: 'Edit introduction page' },
        { href: '#contact-page-editor', label: 'Edit contact page' },
        { href: '#resume-editor', label: 'Edit resume upload' },
    ]

    return (
        <div className="space-y-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">Pages &amp; Settings</h1>
            {loadFailed || !siteSettings ? (
                <AdminErrorPanel
                    title="Pages and settings are unavailable"
                    message="Admin pages/settings data could not be loaded. Please refresh after verifying the backend and database are healthy."
                />
            ) : (
                <>
                    <p className="text-gray-600 dark:text-gray-400">
                        Edit your site&apos;s settings and page content here.
                    </p>

                    <div className="rounded-2xl border border-border/70 bg-background/80 p-4 shadow-sm">
                        <div className="flex flex-wrap gap-2">
                            {sectionLinks.map((link) => (
                                <Button key={link.href} asChild variant="outline" size="sm" className="rounded-full">
                                    <Link href={link.href}>{link.label}</Link>
                                </Button>
                            ))}
                        </div>
                    </div>

                    <div className="space-y-12">
                        <section id="site-settings-editor">
                            <div className="mb-4">
                                <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-50">Site Settings</h2>
                                <p className="mt-1 text-sm text-muted-foreground">Owner name, tagline, and social links for the public shell.</p>
                            </div>
                            <SiteSettingsEditor
                                initialSettings={{
                                    owner_name: siteSettings.owner_name || 'John Doe',
                                    tagline: siteSettings.tagline || 'Creative Technologist',
                                    facebook_url: siteSettings.facebook_url || '',
                                    instagram_url: siteSettings.instagram_url || '',
                                    twitter_url: siteSettings.twitter_url || '',
                                    linkedin_url: siteSettings.linkedin_url || '',
                                    github_url: siteSettings.github_url || '',
                                }}
                            />
                        </section>

                        {homePage && (
                            <section id="home-page-editor">
                                <div className="mb-4">
                                    <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-50">Home</h2>
                                    <p className="mt-1 text-sm text-muted-foreground">Update the hero copy and profile image for the landing page.</p>
                                </div>
                                <HomePageEditor
                                    pageId={homePage.id}
                                    pageTitle={homePage.title}
                                    initialContent={toHomeContent(homePage.content)}
                                />
                            </section>
                        )}

                        {introPage && (
                            <section id="introduction-page-editor">
                                <div className="mb-4">
                                    <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-50">Introduction</h2>
                                    <p className="mt-1 text-sm text-muted-foreground">Edit the long-form introduction page content.</p>
                                </div>
                                <PageEditor
                                    page={{
                                        id: introPage.id,
                                        title: introPage.title,
                                        slug: introPage.slug,
                                        content: isHtmlPageContent(introPage.content) ? introPage.content : { html: '' },
                                    }}
                                />
                            </section>
                        )}

                        {contactPage && (
                            <section id="contact-page-editor">
                                <div className="mb-4">
                                    <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-50">Contact</h2>
                                    <p className="mt-1 text-sm text-muted-foreground">Maintain the contact page body without hunting through URLs.</p>
                                </div>
                                <PageEditor
                                    page={{
                                        id: contactPage.id,
                                        title: contactPage.title,
                                        slug: contactPage.slug,
                                        content: isHtmlPageContent(contactPage.content) ? contactPage.content : { html: '' },
                                    }}
                                />
                            </section>
                        )}

                        <section id="resume-editor">
                            <div className="mb-4">
                                <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-50">Resume</h2>
                                <p className="mt-1 text-sm text-muted-foreground">Upload the latest PDF that powers the public resume download.</p>
                            </div>
                            <ResumeEditor resumeAsset={resumeAsset} />
                        </section>
                    </div>
                </>
            )}
        </div>
    )
}
