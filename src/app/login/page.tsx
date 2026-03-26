
"use client"

import { Button } from '@/components/ui/button'
import { getLocalAdminLoginUrl, getLoginUrl } from '@/lib/api/auth'
import { ShieldCheck } from 'lucide-react'

export default function LoginPage() {
    const handleLogin = () => {
        window.location.href = getLoginUrl('/admin')
    }

    const handleLocalAdminLogin = () => {
        window.location.href = getLocalAdminLoginUrl('/admin')
    }

    return (
        <div className="flex min-h-screen items-center justify-center bg-gray-50 dark:bg-gray-950">
            <div className="w-full max-w-md space-y-8 rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-gray-800 dark:bg-gray-900">
                <div className="text-center">
                    <h1 className="text-2xl font-bold tracking-tight">Admin Login</h1>
                    <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
                        Sign in to manage your portfolio content.
                    </p>
                </div>
                <div className="mt-8">
                    <Button onClick={handleLogin} className="w-full" size="lg">
                        Sign in with Google
                    </Button>
                </div>
                <div className="space-y-3 rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm dark:border-emerald-900/60 dark:bg-emerald-950/30">
                    <div className="flex items-start gap-3">
                        <ShieldCheck className="mt-0.5 h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                        <div className="space-y-2">
                            <p className="font-medium text-emerald-900 dark:text-emerald-100">
                                Local development shortcut
                            </p>
                            <p className="text-emerald-800/80 dark:text-emerald-200/80">
                                If Google login does not resolve to an admin role locally, use the seeded local admin session shortcut.
                            </p>
                            <Button type="button" variant="outline" className="w-full border-emerald-300 bg-white hover:bg-emerald-100 dark:border-emerald-800 dark:bg-emerald-950 dark:hover:bg-emerald-900/50" onClick={handleLocalAdminLogin}>
                                Continue as Local Admin
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}
