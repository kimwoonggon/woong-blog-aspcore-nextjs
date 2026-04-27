"use client"

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { Briefcase, FileText, LayoutDashboard, Settings, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const navItems = [
  { href: '/admin/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/admin/works', label: 'Works', icon: Briefcase },
  { href: '/admin/blog', label: 'Blog', icon: FileText },
  { href: '/admin/blog/notion', label: 'Blog Notion View', icon: FileText },
  { href: '/admin/pages', label: 'Pages & Settings', icon: Settings },
  { href: '/admin/members', label: 'Members', icon: Users },
]

export function AdminSidebarNav() {
  const pathname = usePathname()

  return (
    <nav aria-label="Admin navigation" className="flex flex-col gap-1">
      {navItems.map(({ href, label, icon: Icon }) => {
        const isActive = pathname === href || (href !== '/admin/dashboard' && pathname.startsWith(href))

        return (
          <Button
            key={href}
            asChild
            variant={isActive ? 'secondary' : 'ghost'}
            className={cn(
              'w-full justify-start gap-2 rounded-xl px-3 py-5',
              isActive && 'bg-accent font-semibold text-accent-foreground',
            )}
          >
            <Link aria-current={isActive ? 'page' : undefined} href={href}>
              <Icon aria-hidden="true" size={18} />
              {label}
            </Link>
          </Button>
        )
      })}
    </nav>
  )
}
