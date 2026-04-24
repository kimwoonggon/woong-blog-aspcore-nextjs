"use client"

import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import {
    BookOpen,
    BriefcaseBusiness,
    FileText,
    Home,
    Mail,
    Menu,
    Search,
    UserRound,
} from "lucide-react"
import { useEffect, useRef, useState, useSyncExternalStore } from "react"
import { Button } from "@/components/ui/button"
import { ThemeToggle } from "@/components/ui/ThemeToggle"
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet"
import { cn } from "@/lib/utils"

const navItems = [
    { name: "Home", href: "/" },
    { name: "Introduction", href: "/introduction" },
    { name: "Works", href: "/works" },
    { name: "Study", href: "/blog" },
    { name: "Contact", href: "/contact" },
    { name: "Resume", href: "/resume" },
]

const mobileTabItems = [
    { name: "Home", href: "/", icon: Home },
    { name: "Intro", href: "/introduction", icon: UserRound },
    { name: "Works", href: "/works", icon: BriefcaseBusiness },
    { name: "Study", href: "/blog", icon: BookOpen },
    { name: "Contact", href: "/contact", icon: Mail },
    { name: "Resume", href: "/resume", icon: FileText },
]

interface NavbarProps {
    ownerName?: string
}

function isActivePath(pathname: string, href: string) {
    if (href === "/") {
        return pathname === "/"
    }

    return pathname === href || pathname.startsWith(`${href}/`)
}

function focusSearchInputById(id: string) {
    const target = document.getElementById(id) as HTMLInputElement | null
    if (!target) {
        return false
    }

    target.focus({ preventScroll: false })
    target.select()
    return true
}

export function Navbar({ ownerName = 'John Doe' }: NavbarProps) {
    const pathname = usePathname()
    const router = useRouter()
    const [isOpen, setIsOpen] = useState(false)
    const [canUseInlineNav, setCanUseInlineNav] = useState(false)
    const isMounted = useSyncExternalStore(
        () => () => {},
        () => true,
        () => false,
    )
    const headerRowRef = useRef<HTMLDivElement | null>(null)
    const brandRef = useRef<HTMLDivElement | null>(null)
    const actionsRef = useRef<HTMLDivElement | null>(null)
    const closeMenu = () => setIsOpen(false)

    function measureInlineNavWidth() {
        if (typeof window === 'undefined') {
            return 0
        }

        const canvas = document.createElement('canvas')
        const context = canvas.getContext('2d')
        if (!context) {
            return 0
        }

        context.font = '500 14px system-ui'
        return navItems.reduce((total, item) => total + context.measureText(item.name).width + 32, 0) + ((navItems.length - 1) * 8)
    }

    useEffect(() => {
        if (!isMounted || typeof window === 'undefined') {
            return
        }

        const recompute = () => {
            const headerWidth = headerRowRef.current?.getBoundingClientRect().width ?? 0
            const brandWidth = brandRef.current?.getBoundingClientRect().width ?? 0
            const actionsWidth = actionsRef.current?.getBoundingClientRect().width ?? 0
            const navWidth = measureInlineNavWidth()

            const viewportWideEnough = window.innerWidth >= 1280
            const requiredWidth = brandWidth + actionsWidth + navWidth + 96
            setCanUseInlineNav(viewportWideEnough && headerWidth >= requiredWidth)
        }

        const observer = new ResizeObserver(recompute)
        if (headerRowRef.current) observer.observe(headerRowRef.current)
        if (brandRef.current) observer.observe(brandRef.current)
        if (actionsRef.current) observer.observe(actionsRef.current)

        recompute()
        window.addEventListener('resize', recompute)

        return () => {
            observer.disconnect()
            window.removeEventListener('resize', recompute)
        }
    }, [isMounted, ownerName])

    function handleMobileSearch() {
        if (pathname === '/blog') {
            if (!focusSearchInputById('study-search')) {
                router.push('/blog?focusSearch=1')
            }
            return
        }

        if (pathname === '/works') {
            if (!focusSearchInputById('work-search')) {
                router.push('/works?focusSearch=1')
            }
            return
        }

        router.push('/blog?focusSearch=1')
    }

    return (
        <>
            <header className="sticky top-0 z-[50] border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/75">
                {canUseInlineNav ? (
                    <nav className="pointer-events-none absolute left-1/2 top-1/2 z-[65] hidden -translate-x-1/2 -translate-y-1/2 items-center justify-center gap-2 whitespace-nowrap xl:flex">
                        <div className="pointer-events-auto flex items-center gap-2">
                            {navItems.map((item) => (
                                <Link
                                    key={item.href}
                                    href={item.href}
                                    className={cn(
                                        "whitespace-nowrap rounded-full px-4 py-2 text-sm font-medium transition-colors",
                                        "focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ring",
                                        pathname === item.href
                                            ? "bg-foreground text-background"
                                            : "text-muted-foreground hover:bg-muted hover:text-foreground",
                                    )}
                                >
                                    {item.name}
                                </Link>
                            ))}
                        </div>
                    </nav>
                ) : null}

                <Sheet open={isOpen} onOpenChange={setIsOpen}>
                    <div ref={headerRowRef} className="container mx-auto flex h-16 items-center gap-2 px-3 md:px-6 lg:h-20 lg:gap-3">
                        <div className="flex shrink-0 items-center lg:hidden">
                            <SheetTrigger asChild>
                                <Button
                                    type="button"
                                    variant="ghost"
                                    className="h-11 min-w-11 rounded-full px-3"
                                    aria-label="Toggle Menu"
                                >
                                    <Menu className="h-5 w-5" />
                                </Button>
                            </SheetTrigger>
                        </div>

                        <div ref={brandRef} className="flex min-w-0 flex-1 items-center justify-center lg:flex-none lg:justify-start">
                            <Link href="/" data-testid="navbar-brand" className="min-w-0 rounded-2xl px-1 py-1 transition-colors hover:text-primary">
                                <span className="max-w-[10.5rem] truncate text-lg font-semibold text-foreground sm:max-w-[13.5rem] sm:text-xl md:max-w-[16rem] md:text-2xl xl:max-w-[14rem] 2xl:max-w-[18rem]">
                                    {ownerName}
                                </span>
                            </Link>
                        </div>

                        <div className="ml-auto flex shrink-0 items-center gap-1 lg:hidden">
                            <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                aria-label="Open search"
                                data-testid="mobile-header-search"
                                className="h-11 w-11 rounded-full"
                                onClick={handleMobileSearch}
                            >
                                <Search className="h-5 w-5" aria-hidden="true" />
                            </Button>
                            <ThemeToggle testId="mobile-header-theme-toggle" />
                        </div>

                        <div ref={actionsRef} className="ml-auto hidden min-w-0 items-center justify-end gap-2 lg:flex lg:gap-3">
                            <ThemeToggle />
                        </div>

                        {isMounted && !canUseInlineNav ? (
                            <div className="hidden shrink-0 items-center justify-end lg:flex">
                                <SheetTrigger asChild>
                                    <Button
                                        type="button"
                                        variant="ghost"
                                        className="h-11 min-w-11 rounded-full px-3"
                                        aria-label="Toggle Menu"
                                    >
                                        <Menu className="h-5 w-5" />
                                    </Button>
                                </SheetTrigger>
                            </div>
                        ) : null}
                    </div>

                    <SheetContent side="right" className="w-[92vw] max-w-sm p-0">
                        <div className="flex h-full flex-col">
                            <div className="border-b px-6 py-5">
                                <Link href="/" className="flex flex-col" onClick={() => setIsOpen(false)}>
                                    <span className="text-2xl font-semibold text-foreground">{ownerName}</span>
                                </Link>
                                <p className="mt-3 text-sm text-muted-foreground">
                                    Browse the public site with simple, touch-friendly navigation.
                                </p>
                            </div>

                            <div className="flex flex-1 flex-col gap-2 px-6 py-6">
                                {navItems.map((item) => (
                                    <Link
                                        key={item.href}
                                        href={item.href}
                                        onClick={closeMenu}
                                        className={cn(
                                            "rounded-2xl px-4 py-3 text-base font-medium transition-colors",
                                            pathname === item.href
                                                ? "bg-foreground text-background"
                                                : "text-foreground hover:bg-muted",
                                        )}
                                    >
                                        {item.name}
                                    </Link>
                                ))}
                            </div>

                            <div className="border-t px-6 py-5">
                                <ThemeToggle showLabel testId="mobile-theme-toggle" />
                            </div>
                        </div>
                    </SheetContent>
                </Sheet>
            </header>

            <nav
                aria-label="Mobile tab navigation"
                data-testid="mobile-bottom-nav"
                className="fixed inset-x-0 bottom-0 z-[60] border-t border-border/80 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/80 lg:hidden"
            >
                <ul className="grid grid-cols-6 gap-1 px-2 pb-[calc(env(safe-area-inset-bottom)+0.35rem)] pt-1.5">
                    {mobileTabItems.map(({ name, href, icon: Icon }) => {
                        const active = isActivePath(pathname, href)
                        return (
                            <li key={href}>
                                <Link
                                    href={href}
                                    aria-current={active ? 'page' : undefined}
                                    data-testid={`mobile-bottom-nav-link-${name.toLowerCase()}`}
                                    className={cn(
                                        "flex min-h-12 flex-col items-center justify-center gap-1 rounded-xl px-1 text-[11px] transition-colors",
                                        active
                                            ? "bg-foreground text-background"
                                            : "text-muted-foreground hover:bg-muted hover:text-foreground",
                                    )}
                                >
                                    <Icon className="h-4 w-4" aria-hidden="true" />
                                    <span className="font-medium leading-none">{name}</span>
                                </Link>
                            </li>
                        )
                    })}
                </ul>
            </nav>
        </>
    )
}
