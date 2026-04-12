import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can create and publish a work that appears on public works page', async ({ page }) => {
  const title = `Playwright Work ${Date.now()}`

  await page.goto('/admin/works')
  await page.getByRole('link', { name: /Add Work/i }).click()
  await expect(page).toHaveURL(/\/admin\/works\/new/)
  await expect(page.getByLabel('Category')).toHaveValue('Uncategorized')
  await expect(page.getByLabel('Title')).toBeVisible()
  await page.evaluate((titleValue) => {
    const setInputValue = (selector: string, value: string) => {
      const element = document.querySelector(selector) as HTMLInputElement | null
      if (!element) throw new Error(`Missing input: ${selector}`)
      const prototype = Object.getPrototypeOf(element)
      const descriptor = Object.getOwnPropertyDescriptor(prototype, 'value')
      descriptor?.set?.call(element, value)
      element.dispatchEvent(new Event('input', { bubbles: true }))
      element.dispatchEvent(new Event('change', { bubbles: true }))
    }

    setInputValue('input[name="title"]', titleValue)
    setInputValue('input[name="period"]', '2026.03 - 2026.03')
    setInputValue('input[name="tags"]', 'playwright, works')
  }, title)

  await expect(page.locator('input[name="title"]')).toHaveValue(title)
  await expect(page.locator('input[name="category"]')).toHaveValue('Uncategorized')
  await expect(page.locator('input[name="period"]')).toHaveValue('2026.03 - 2026.03')
  await expect(page.locator('input[name="tags"]')).toHaveValue('playwright, works')
  await expect(page.getByText('New works go live immediately. Staged videos attach automatically after creation.')).toBeVisible()

  await page.locator('.tiptap.ProseMirror').first().fill(`This is a browser-driven published work for ${title}.`)

  await expect(page.getByRole('button', { name: /Create Work/i })).toBeEnabled()
  const [saveResponse] = await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/works') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: /Create Work/i }).click(),
  ])

  const payload = await saveResponse.json()
  await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/, { timeout: 20000 })
  await expect(page.getByRole('link', { name: title }).first()).toBeVisible()

  await page.goto(`/works/${payload.slug}`)
  await expect(page.getByRole('heading', { name: title })).toBeVisible()
  await expect(page.getByText('Uncategorized').first()).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/admin-work-publish.png', fullPage: true })
})
