"use client"

import { Moon, Sun } from "lucide-react"
import { useTheme } from "next-themes"
import { useSyncExternalStore } from "react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { cn } from "@/lib/utils"

type ThemeMode = "light" | "dark"

const themeOptions: Array<{
  label: string
  value: ThemeMode
  Icon: typeof Sun
}> = [
  { label: "Light", value: "light", Icon: Sun },
  { label: "Dark", value: "dark", Icon: Moon },
]

function CurrentThemeIcon({ theme }: { theme: ThemeMode }) {
  if (theme === "dark") {
    return <Moon className="h-5 w-5" />
  }

  if (theme === "light") {
    return <Sun className="h-5 w-5" />
  }

  return <Sun className="h-5 w-5" />
}

export function ThemeToggle({ className, testId = "theme-toggle" }: { className?: string; testId?: string }) {
  const { setTheme, theme } = useTheme()
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  )

  if (!mounted) {
    return <div aria-hidden className={cn("h-11 w-11 rounded-full border border-border/70 bg-background/70", className)} />
  }

  const currentTheme = (theme === "dark" ? "dark" : "light") as ThemeMode

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className={cn("h-11 w-11 rounded-full bg-transparent hover:bg-accent", className)}
          aria-label="테마 변경"
          data-testid={testId}
        >
          <CurrentThemeIcon theme={currentTheme} />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        side="bottom"
        sideOffset={10}
        collisionPadding={16}
        className="min-w-40"
      >
        <DropdownMenuLabel>Theme</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuRadioGroup value={currentTheme} onValueChange={(value) => setTheme(value)}>
          {themeOptions.map(({ label, value, Icon }) => (
            <DropdownMenuRadioItem key={value} value={value} className="gap-2">
              <Icon className="h-4 w-4" />
              {label}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
