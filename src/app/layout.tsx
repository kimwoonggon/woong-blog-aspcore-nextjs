
import type { Metadata } from "next"
import { Archivo, Space_Grotesk } from "next/font/google"
import "./globals.css"
import { cn } from "@/lib/utils"
import { fetchPublicSiteSettings } from "@/lib/api/site-settings"

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
  }
}

import { Toaster } from "sonner"

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en">
      <body className={cn(archivo.variable, spaceGrotesk.variable, "antialiased font-sans bg-background text-foreground")}>
        {children}
        <Toaster position="top-right" richColors />
      </body>
    </html>
  )
}
