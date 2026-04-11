import { expect, test } from '@playwright/test'
import { expectRgbClose, getColorChannels, getRootVariableChannels } from './helpers/ui-improvement'

test('Featured works appears before Recent posts on the home page', async ({ page }) => {
  await page.goto('/')

  const featuredHeading = page.getByRole('heading', { name: 'Featured works' })
  const recentHeading = page.getByRole('heading', { name: 'Recent posts' })

  const featuredBox = await featuredHeading.boundingBox()
  const recentBox = await recentHeading.boundingBox()

  expect(featuredBox).toBeTruthy()
  expect(recentBox).toBeTruthy()
  expect(featuredBox!.y).toBeLessThan(recentBox!.y)
})

test('Featured works section uses the brand section background', async ({ page }) => {
  await page.goto('/')

  const section = page.getByRole('heading', { name: 'Featured works' }).locator('xpath=ancestor::section[1]')
  const background = await getColorChannels(section, 'background-color')
  const expected = await getRootVariableChannels(page, '--brand-section-bg')
  expectRgbClose(background, expected)
})
