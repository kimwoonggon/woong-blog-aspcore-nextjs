"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { CircleUserRound, LogIn, Menu } from "lucide-react"
import { useState, useSyncExternalStore } from "react"
import { Button } from "@/components/ui/button"
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
            <DropdownMenuContent align="end" className="min-w-48">
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
                    onSelect={async () => {
                        const redirectUrl = await logoutWithCsrf('/')
                        window.location.assign(redirectUrl)
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
    const isMounted = useSyncExternalStore(
        () => () => {},
        () => true,
        () => false,
    )

    const authenticated = session?.authenticated ?? false
    const isAdmin = session?.role === 'admin'
    const avatarLabel = session?.name || (isAdmin ? 'Admin' : 'User')

    return (
        <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/75">
            <div className="container flex h-20 items-center gap-3 px-4 md:px-6">
                <div className="flex min-w-0 flex-1 items-center gap-4 lg:min-w-[320px] lg:flex-none">
                    <Link href="/" className="min-w-0 rounded-2xl px-1 py-1 transition-colors hover:text-primary">
                        <div className="flex flex-col">
                            <span className="text-[11px] font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                                Portfolio
                            </span>
                            <span className="truncate text-xl font-semibold text-foreground md:text-2xl">
                                {ownerName}
                            </span>
                        </div>
                    </Link>
                    <p className="hidden max-w-[220px] text-sm text-muted-foreground xl:block">
                        Works, writing, and experiments in one balanced shell.
                    </p>
                </div>

                <nav className="hidden min-w-0 flex-1 items-center justify-center gap-2 lg:flex">
                    {navItems.map((item) => (
                        <Link
                            key={item.href}
                            href={item.href}
                            className={cn(
                                "rounded-full px-4 py-2 text-sm font-medium transition-colors",
                                pathname === item.href
                                    ? "bg-foreground text-background"
                                    : "text-muted-foreground hover:bg-muted hover:text-foreground",
                            )}
                        >
                            {item.name}
                        </Link>
                    ))}
                </nav>

                <div className="ml-auto hidden items-center gap-3 lg:flex">
                    <Link
                        href="/blog"
                        className="hidden rounded-full border border-border/80 px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:border-primary/30 hover:text-foreground xl:inline-flex"
                    >
                        Latest writing
                    </Link>
                    <SessionActions authenticated={authenticated} isAdmin={isAdmin} avatarLabel={avatarLabel} />
                </div>

                {isMounted ? (
                    <Sheet open={isOpen} onOpenChange={setIsOpen}>
                        <SheetTrigger asChild>
                            <Button
                                variant="ghost"
                                className="h-11 rounded-full px-3 lg:hidden"
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
                                            onClick={() => setIsOpen(false)}
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
                                                    onClick={async () => {
                                                        setIsOpen(false)
                                                        const redirectUrl = await logoutWithCsrf('/')
                                                        window.location.assign(redirectUrl)
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
                    <div className="h-11 w-11 lg:hidden" />
                )}
            </div>
        </header>
    )
}
