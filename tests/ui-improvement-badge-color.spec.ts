import { expect, test } from '@playwright/test'
import { contrastRatio, expectRgbClose, getColorChannels, getRootVariableChannels, gotoWithTheme } from './helpers/ui-improvement'

test('blog detail date badge uses brand navy', async ({ page }) => {
  await page.goto('/blog/seeded-blog')

  const badge = page.locator('article header .rounded-full').first()
  const background = await getColorChannels(badge, 'background-color')
  const expected = await getRootVariableChannels(page, '--brand-navy')
  expectRgbClose(background, expected)
})

test('work detail date badge uses brand navy', async ({ page }) => {
  await page.goto('/works/seeded-work')

  const badge = page.locator('article header .rounded-full').first()
  const background = await getColorChannels(badge, 'background-color')
  const expected = await getRootVariableChannels(page, '--brand-navy')
  expectRgbClose(background, expected)
})

test('dark mode date badges keep accessible white-on-navy contrast', async ({ page }) => {
  await gotoWithTheme(page, '/works/seeded-work')

  const badge = page.locator('article header .rounded-full').first()
  const background = await getColorChannels(badge, 'background-color')
  const foreground = await getColorChannels(badge, 'color')
  expect(contrastRatio(foreground, background)).toBeGreaterThanOrEqual(4.5)
})
