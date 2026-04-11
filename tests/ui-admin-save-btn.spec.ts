import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('Blog and Work primary save buttons no longer use the legacy hover-scale styling', async ({ page }) => {
  await page.goto('/admin/blog/new')
  const blogSaveButton = page.getByRole('button', { name: 'Create Post' })
  await expect(blogSaveButton).toBeVisible()
  await expect(blogSaveButton).toHaveAttribute('data-variant', 'default')
  await expect(blogSaveButton).not.toHaveClass(/hover:scale/)
  await expect(blogSaveButton).not.toHaveClass(/bg-brand-navy/)

  await page.goto('/admin/works/new')
  const workCreateWithVideosButton = page.getByRole('button', { name: 'Create And Add Videos' })
  await expect(workCreateWithVideosButton).toBeVisible()
  await expect(workCreateWithVideosButton).toHaveAttribute('data-variant', 'default')
  await expect(workCreateWithVideosButton).not.toHaveClass(/hover:scale/)
  await expect(workCreateWithVideosButton).not.toHaveClass(/bg-brand-navy/)
})
