import { expect, test } from '@playwright/test'

test('VA-130 works archive keeps four cards aligned on the first desktop row', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1400 })
  await page.goto('/works')

  const cards = page.getByTestId('work-card')
  expect(await cards.count()).toBeGreaterThanOrEqual(4)

  const boxes = await Promise.all([0, 1, 2, 3].map(async (index) => {
    const box = await cards.nth(index).boundingBox()
    expect(box).toBeTruthy()
    return box!
  }))

  const tops = boxes.map((box) => box.y)
  expect(Math.max(...tops) - Math.min(...tops)).toBeLessThan(4)

  const xs = boxes.map((box) => box.x)
  expect(xs[0]).toBeLessThan(xs[1])
  expect(xs[1]).toBeLessThan(xs[2])
  expect(xs[2]).toBeLessThan(xs[3])
})
