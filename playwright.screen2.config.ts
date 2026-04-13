import base from './playwright.config'
import { defineConfig } from '@playwright/test'

const screen2Args = ['--window-position=3840,0', '--window-size=1080,1920']

export default defineConfig({
  ...base,
  use: {
    ...(base.use ?? {}),
    headless: false,
    launchOptions: {
      ...((base.use as any)?.launchOptions ?? {}),
      args: [...((((base.use as any)?.launchOptions ?? {}).args ?? [])), ...screen2Args],
    },
  },
  projects: (base.projects ?? []).map((project: any) => ({
    ...project,
    use: {
      ...(project.use ?? {}),
      headless: false,
      launchOptions: {
        ...((project.use ?? {}).launchOptions ?? {}),
        args: [...((((project.use ?? {}).launchOptions ?? {}).args ?? [])), ...screen2Args],
      },
    },
  })),
})
