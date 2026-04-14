"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { CircleUserRound, LogIn, Menu } from "lucide-react"
import { useEffect, useRef, useState, useSyncExternalStore } from "react"
import { Button } from "@/components/ui/button"
import { ThemeToggle } from "@/components/ui/ThemeToggle"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet"
import { logoutWithCsrf } from "@/lib/api/auth"
import { cn } from "@/lib/utils"

const navItems = [
    { name: "Home", href: "/" },
    { name: "Introduction", href: "/introduction" },
    { name: "Works", href: "/works" },
    { name: "Blog", href: "/blog" },
    { name: "Contact", href: "/contact" },
    { name: "Resume", href: "/resume" },
]

interface NavbarProps {
    ownerName?: string
    session?: {
        authenticated: boolean
        name?: string
        role?: string
    }
}

async function redirectAfterLogout() {
    const redirectUrl = await logoutWithCsrf('/')
    window.location.assign(redirectUrl)
}

function SessionActions({
    authenticated,
    isAdmin,
    avatarLabel,
}: {
    authenticated: boolean
    isAdmin: boolean
    avatarLabel: string
}) {
    if (!authenticated) {
        return (
            <Link href="/login">
                <Button variant="outline" className="h-11 rounded-full px-4 text-sm font-medium">
                    <LogIn className="mr-2 h-4 w-4" />
                    Login
                </Button>
            </Link>
        )
    }

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <button
                    type="button"
                    className="inline-flex h-11 items-center gap-2 rounded-full border border-sky-200 bg-sky-50 px-3 text-sm font-medium text-sky-700 transition-colors hover:bg-sky-100 dark:border-sky-900 dark:bg-sky-950/40 dark:text-sky-200 dark:hover:bg-sky-900/70"
                    aria-label="Open signed-in menu"
                    title={avatarLabel}
                >
                    <span className="hidden sm:inline">Signed in</span>
                    <span className="inline-flex h-8 w-8 items-center justify-center rounded-full border border-sky-300 bg-white dark:border-sky-800 dark:bg-sky-950">
                        <CircleUserRound className="h-4 w-4" />
                    </span>
                </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
                align="end"
                side="bottom"
                sideOffset={10}
                collisionPadding={16}
                className="min-w-48"
            >
                <DropdownMenuItem asChild>
                    <Link href={isAdmin ? "/admin/dashboard" : "/"}>
                        My Page
                    </Link>
                </DropdownMenuItem>
                {isAdmin && (
                    <DropdownMenuItem asChild>
                        <Link href="/admin">
                            Admin Page
                        </Link>
                    </DropdownMenuItem>
                )}
                <DropdownMenuSeparator />
                <DropdownMenuItem
                    variant="destructive"
                    onSelect={() => {
                        void redirectAfterLogout()
                    }}
                >
                    Logout
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    )
}

export function Navbar({ ownerName = 'John Doe', session }: NavbarProps) {
    const pathname = usePathname()
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

    const authenticated = session?.authenticated ?? false
    const isAdmin = session?.role === 'admin'
    const avatarLabel = session?.name || (isAdmin ? 'Admin' : 'User')
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
    }, [isMounted, ownerName, authenticated, isAdmin, avatarLabel])

    return (
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

            <div ref={headerRowRef} className="container mx-auto flex h-20 items-center gap-3 px-4 md:px-6">
                <div ref={brandRef} className="flex min-w-0 items-center gap-4">
                    <Link href="/" data-testid="navbar-brand" className="min-w-0 rounded-2xl px-1 py-1 transition-colors hover:text-primary">
                        <div className="flex flex-col">
                            <span className="text-[11px] font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                                Portfolio
                            </span>
                            <span className="max-w-[12rem] truncate text-xl font-semibold text-foreground md:max-w-[16rem] md:text-2xl xl:max-w-[14rem] 2xl:max-w-[18rem]">
                                {ownerName}
                            </span>
                        </div>
                    </Link>
                    <p className="hidden min-w-0 max-w-[15rem] truncate text-sm text-muted-foreground min-[1750px]:block">
                        Works, writing, and experiments in one balanced shell.
                    </p>
                </div>

                <div ref={actionsRef} className="ml-auto hidden min-w-0 items-center justify-end gap-2 lg:flex lg:gap-3">
                    <ThemeToggle />
                    <SessionActions authenticated={authenticated} isAdmin={isAdmin} avatarLabel={avatarLabel} />
                </div>

                {isMounted && !canUseInlineNav ? (
                    <Sheet open={isOpen} onOpenChange={setIsOpen}>
                        <SheetTrigger asChild>
                            <Button
                                variant="ghost"
                                className="h-11 rounded-full px-3"
                            >
                                <Menu className="h-5 w-5" />
                                <span className="sr-only">Toggle Menu</span>
                            </Button>
                        </SheetTrigger>
                        <SheetContent side="right" className="w-[92vw] max-w-sm p-0">
                            <div className="flex h-full flex-col">
                                <div className="border-b px-6 py-5">
                                    <Link href="/" className="flex flex-col" onClick={() => setIsOpen(false)}>
                                        <span className="text-[11px] font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                                            Portfolio
                                        </span>
                                        <span className="text-2xl font-semibold text-foreground">{ownerName}</span>
                                    </Link>
                                    <p className="mt-3 text-sm text-muted-foreground">
                                        Browse the public site or jump back into admin without awkward center-heavy navigation.
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
                                    <div className="mb-4 flex items-center justify-between rounded-2xl border border-border/80 bg-muted/30 px-4 py-3">
                                        <div>
                                            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                                                Account
                                            </p>
                                            <p className="text-sm font-medium text-foreground">
                                                {authenticated ? avatarLabel : 'Guest'}
                                            </p>
                                        </div>
                                        <CircleUserRound className="h-5 w-5 text-muted-foreground" />
                                    </div>

                                    <div className="mb-4 flex items-center justify-between rounded-2xl border border-border/80 bg-background/80 px-4 py-3">
                                        <div>
                                            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                                                Theme
                                            </p>
                                            <p className="text-sm font-medium text-foreground">
                                                Light, dark, or system
                                            </p>
                                        </div>
                                        <ThemeToggle testId={undefined} />
                                    </div>

                                    <div className="space-y-2">
                                        {authenticated ? (
                                            <>
                                                <Link href={isAdmin ? "/admin/dashboard" : "/"} onClick={() => setIsOpen(false)}>
                                                    <Button variant="outline" className="w-full justify-start rounded-2xl">
                                                        My Page
                                                    </Button>
                                                </Link>
                                                {isAdmin && (
                                                    <Link href="/admin" onClick={() => setIsOpen(false)}>
                                                        <Button variant="outline" className="w-full justify-start rounded-2xl">
                                                            Admin Page
                                                        </Button>
                                                    </Link>
                                                )}
                                                <Button
                                                    type="button"
                                                    variant="outline"
                                                    className="w-full justify-start rounded-2xl text-red-500 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/20"
                                                    onClick={() => {
                                                        closeMenu()
                                                        void redirectAfterLogout()
                                                    }}
                                                >
                                                    Logout
                                                </Button>
                                            </>
                                        ) : (
                                            <Link href="/login" onClick={() => setIsOpen(false)}>
                                                <Button className="w-full justify-start rounded-2xl">
                                                    <LogIn className="mr-2 h-4 w-4" />
                                                    Login
                                                </Button>
                                            </Link>
                                        )}
                                    </div>
                                </div>
                            </div>
                        </SheetContent>
                    </Sheet>
                ) : (
                    <div className="h-11 w-11" />
                )}
            </div>
        </header>
    )
}
