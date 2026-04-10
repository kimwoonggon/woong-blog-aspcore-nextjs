import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can add a youtube video to an existing work', async ({ page }) => {
  await page.goto('/admin/works')
  await page.getByTestId('admin-work-row').first().getByRole('link').first().click()

  await page.getByLabel('YouTube URL or ID').fill('https://youtu.be/dQw4w9WgXcQ')
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/videos/youtube') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Add YouTube Video' }).click(),
  ])

  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]').first()).toBeVisible()
})
