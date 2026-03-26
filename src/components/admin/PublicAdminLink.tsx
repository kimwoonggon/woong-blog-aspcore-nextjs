import Link from 'next/link'
import { Pencil, LayoutDashboard } from 'lucide-react'
import { fetchServerSession } from '@/lib/api/server'

interface PublicAdminLinkProps {
  href: string
  label: string
  variant?: 'edit' | 'manage'
}

export async function PublicAdminLink({ href, label, variant = 'edit' }: PublicAdminLinkProps) {
  const session = await fetchServerSession()

  if (!session.authenticated || session.role !== 'admin') {
    return null
  }

  const Icon = variant === 'manage' ? LayoutDashboard : Pencil

  return (
    <Link
      href={href}
      className="inline-flex items-center gap-2 rounded-full border border-sky-200 bg-sky-50 px-3 py-1.5 text-sm font-medium text-sky-700 transition-colors hover:bg-sky-100 dark:border-sky-900 dark:bg-sky-950/40 dark:text-sky-200 dark:hover:bg-sky-900/60"
    >
      <Icon className="h-4 w-4" />
      {label}
    </Link>
  )
}
