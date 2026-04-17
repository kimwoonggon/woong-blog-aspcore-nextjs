
import type { Metadata } from "next"
import { Archivo, Space_Grotesk } from "next/font/google"
import "./globals.css"
import { cn } from "@/lib/utils"
import { fetchPublicSiteSettings } from "@/lib/api/site-settings"
import { ThemeProvider } from "@/components/providers/ThemeProvider"

const archivo = Archivo({
  subsets: ["latin"],
  variable: "--font-archivo",
  display: "swap",
})

const spaceGrotesk = Space_Grotesk({
  subsets: ["latin"],
  variable: "--font-space-grotesk",
  display: "swap",
})

export async function generateMetadata(): Promise<Metadata> {
  const siteSettings = await fetchPublicSiteSettings()
  const ownerName = siteSettings?.ownerName || 'John Doe'
  const tagline = siteSettings?.tagline || 'Creative Technologist'

  return {
    title: `${ownerName} | ${tagline}`,
    description: `Personal portfolio of ${ownerName}, showcasing works and thoughts.`,
    icons: {
      icon: '/favicon.svg',
      shortcut: '/favicon.svg',
      apple: '/favicon.svg',
    },
  }
}

import { Toaster } from "sonner"

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={cn(archivo.variable, spaceGrotesk.variable, "antialiased font-sans bg-background text-foreground")}>
        <ThemeProvider>
          {children}
          <Toaster position="top-right" richColors />
        </ThemeProvider>
      </body>
    </html>
  )
}
