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

    return (
        <footer className="w-full bg-white py-8 dark:bg-gray-950">
            <div className="container mx-auto flex flex-col items-center gap-6 px-4">
                {/* Social Icons */}
                {socialLinks.length > 0 && (
                    <div className="flex items-center gap-6">
                        {socialLinks.map(({ url, icon: Icon, label }) => (
                            <Link
                                key={label}
                                href={url}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="text-gray-600 transition-colors hover:text-[#F3434F] dark:text-gray-400 dark:hover:text-[#F3434F]"
                                aria-label={label}
                            >
                                <Icon size={24} />
                            </Link>
                        ))}
                    </div>
                )}

                {/* Copyright */}
                <p className="text-center text-sm text-gray-500 dark:text-gray-400">
                    &copy; {new Date().getFullYear()} {ownerName}. All rights reserved.
                </p>
            </div>
        </footer>
    )
}
