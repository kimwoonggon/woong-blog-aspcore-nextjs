import { expect, test } from '@playwright/test'

test('switching documents in notion view does not create a new navigation entry', async ({ page }) => {
  await page.goto('/admin/blog/notion')

  await page.getByTestId('notion-library-trigger').click()
  const listItems = page.getByTestId('notion-blog-list-item')
  await expect(listItems.first()).toBeVisible()
  const itemCount = await listItems.count()
  test.skip(itemCount < 2, 'Need at least two blog documents for client-side switching coverage')

  await listItems.nth(0).click()
  await expect(page.locator('.tiptap.ProseMirror').first()).toBeVisible()

  const initialNavigationCount = await page.evaluate(() => performance.getEntriesByType('navigation').length)
  const initialUrl = page.url()

  await page.getByTestId('notion-library-trigger').click()
  await listItems.nth(1).click()

  await expect(page).not.toHaveURL(initialUrl)
  const nextNavigationCount = await page.evaluate(() => performance.getEntriesByType('navigation').length)
  expect(nextNavigationCount).toBe(initialNavigationCount)
})

test('selected notion document persists after reload via url state', async ({ page }) => {
  await page.goto('/admin/blog/notion')

  await page.getByTestId('notion-library-trigger').click()
  const listItems = page.getByTestId('notion-blog-list-item')
  await expect(listItems.first()).toBeVisible()

  const itemCount = await listItems.count()
  test.skip(itemCount < 2, 'Need at least two blog documents for reload persistence coverage')

  await listItems.nth(1).click()

  await expect(page.locator('.tiptap.ProseMirror').first()).toBeVisible()
  const selectedUrl = page.url()
  const selectedTitle = await page.getByLabel('Title').inputValue()

  await page.reload({ waitUntil: 'networkidle' })
  await expect(page.locator('.tiptap.ProseMirror').first()).toBeVisible()
  await expect(page).toHaveURL(selectedUrl)
  await expect(page.getByLabel('Title')).toHaveValue(selectedTitle)
})
