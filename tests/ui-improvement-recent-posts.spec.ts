import { expect, test } from '@playwright/test'
import { getStyle } from './helpers/ui-improvement'

test('Recent posts section uses the default page background', async ({ page }) => {
  await page.goto('/')

  const section = page.getByRole('heading', { name: 'Recent posts' }).locator('xpath=ancestor::section[1]')
  const sectionBackground = await getStyle(section, 'background-color')
  const bodyBackground = await getStyle(page.locator('body'), 'background-color')

  expect(sectionBackground).toBe(bodyBackground)
})

test('Recent posts heading uses bold weight', async ({ page }) => {
  await page.goto('/')

  const heading = page.getByRole('heading', { name: 'Recent posts' })
  const fontWeight = await getStyle(heading, 'font-weight')
  expect(Number.parseInt(fontWeight, 10)).toBeGreaterThanOrEqual(700)
})
