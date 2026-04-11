import { expect, test } from '@playwright/test'

test('keyboard-focused public links keep a visible focus outline', async ({ page }) => {
  await page.goto('/')

  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')

  const focused = page.getByRole('link', { name: 'Home', exact: true }).first()
  await expect(focused).toBeFocused()

  const outlineWidth = await focused.evaluate((element) => getComputedStyle(element).outlineWidth)
  expect(Number.parseFloat(outlineWidth)).toBeGreaterThan(0)
})
