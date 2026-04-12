import Link from 'next/link'
import { Facebook, Instagram, Twitter, Linkedin, Github } from 'lucide-react'

interface FooterProps {
    ownerName?: string
    facebookUrl?: string
    instagramUrl?: string
    twitterUrl?: string
    linkedinUrl?: string
    githubUrl?: string
}

export function Footer({
    ownerName = 'John Doe',
    facebookUrl = '',
    instagramUrl = '',
    twitterUrl = '',
    linkedinUrl = '',
    githubUrl = ''
}: FooterProps) {
    const socialLinks = [
        { url: facebookUrl, icon: Facebook, label: 'Facebook' },
        { url: instagramUrl, icon: Instagram, label: 'Instagram' },
        { url: twitterUrl, icon: Twitter, label: 'Twitter' },
        { url: linkedinUrl, icon: Linkedin, label: 'LinkedIn' },
        { url: githubUrl, icon: Github, label: 'GitHub' },
    ].filter(link => link.url) // Only show icons that have URLs

    const footerLinks = [
        { href: '/', label: 'Home' },
        { href: '/works', label: 'Works' },
        { href: '/blog', label: 'Blog' },
        { href: '/introduction', label: 'Introduction' },
        { href: '/contact', label: 'Contact' },
    ]

    return (
        <footer className="w-full border-t border-border bg-background py-8">
            <div className="container mx-auto flex flex-col gap-8 px-4">
                <div className="flex flex-col gap-8 md:flex-row md:items-start md:justify-between">
                    <div className="max-w-md space-y-2">
                        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                            Explore
                        </p>
                        <p className="text-lg font-heading font-semibold text-foreground">
                            {ownerName}
                        </p>
                        <p className="text-sm leading-relaxed text-muted-foreground">
                            Selected work, writing, and ways to get in touch.
                        </p>
                    </div>

                    <nav
                        aria-label="Footer navigation"
                        className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm sm:grid-cols-3"
                    >
                        {footerLinks.map(({ href, label }) => (
                            <Link
                                key={href}
                                href={href}
                                className="text-muted-foreground transition-colors hover:text-brand-accent"
                            >
                                {label}
                            </Link>
                        ))}
                    </nav>
                </div>

                {/* Social Icons */}
                {socialLinks.length > 0 && (
                    <div className="flex flex-col items-start gap-3 border-t border-border/70 pt-6">
                        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                            Elsewhere
                        </p>
                        <div className="flex items-center gap-6">
                            {socialLinks.map(({ url, icon: Icon, label }) => (
                                <Link
                                    key={label}
                                    href={url}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-muted-foreground transition-colors hover:text-brand-accent"
                                    aria-label={label}
                                >
                                    <Icon size={24} />
                                </Link>
                            ))}
                        </div>
                    </div>
                )}

                {/* Copyright */}
                <p className="border-t border-border/70 pt-6 text-sm text-muted-foreground">
                    &copy; {new Date().getFullYear()} {ownerName}. All rights reserved.
                </p>
            </div>
        </footer>
    )
}
