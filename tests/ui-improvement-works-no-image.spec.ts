import { expect, test } from './helpers/performance-test'

test('works cards use the richer no-image placeholder treatment', async ({ page }) => {
  await page.goto('/works?__qaNoImage=1')

  const placeholder = page.getByTestId('work-card-no-image-placeholder').first()
  await expect(placeholder).toBeVisible()
  await expect(placeholder).toHaveClass(/bg-gradient-to-br/)
})
