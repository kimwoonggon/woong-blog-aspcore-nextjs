import { expect, test } from '@playwright/test'

test('contact page renders heading and contact content', async ({ page }) => {
  await page.goto('/contact')
  await expect(page.getByRole('heading', { name: 'Contact' })).toBeVisible()
  await expect(page.getByText(/woong@example.com/i)).toBeVisible()
})

test('work detail page renders seeded detail content and stable related cards', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/works/seeded-work')
  await expect(page.getByRole('heading', { name: 'Portfolio Platform Rebuild' })).toBeVisible()
  await expect(page.getByText(/React \+ TypeScript frontend/i)).toBeVisible()

  const relatedWorkCards = page.getByTestId('related-work-card')
  await expect(relatedWorkCards.first()).toBeVisible()
  expect(await relatedWorkCards.count()).toBeGreaterThanOrEqual(2)

  const heights = await relatedWorkCards.evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(80)
})

test('blog detail page renders seeded blog content and stable related cards', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/blog/seeded-blog')
  await expect(page.getByRole('heading', { name: 'Designing a Seed-First Migration Strategy' })).toBeVisible()
  await expect(page.getByText(/Seed data gives frontend and backend teams/i)).toBeVisible()

  const relatedBlogCards = page.getByTestId('related-blog-card')
  await expect(relatedBlogCards.first()).toBeVisible()
  expect(await relatedBlogCards.count()).toBeGreaterThanOrEqual(2)

  const heights = await relatedBlogCards.evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(80)
})
