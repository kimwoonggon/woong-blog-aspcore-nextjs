"use client"

import { useEffect, useState } from 'react'

interface BrowserSession {
  authenticated?: boolean
  role?: string
}

interface PublicAdminClientGateProps {
  children: React.ReactNode
}

function canShowAdminAffordance(session: BrowserSession | null) {
  return session?.authenticated === true && session.role === 'admin'
}

export function PublicAdminClientGate({ children }: PublicAdminClientGateProps) {
  const [canShow, setCanShow] = useState(false)

  useEffect(() => {
    let cancelled = false

    async function loadSession() {
      try {
        const response = await fetch('/api/auth/session', {
          credentials: 'include',
          cache: 'no-store',
        })

        if (!response.ok) {
          if (!cancelled) setCanShow(false)
          return
        }

        const session = await response.json() as BrowserSession
        if (!cancelled) {
          setCanShow(canShowAdminAffordance(session))
        }
      } catch {
        if (!cancelled) setCanShow(false)
      }
    }

    void loadSession()

    return () => {
      cancelled = true
    }
  }, [])

  return canShow ? <>{children}</> : null
}
