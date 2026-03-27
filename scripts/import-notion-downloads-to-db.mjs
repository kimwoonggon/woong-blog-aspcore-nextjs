import { mkdir, readFile, writeFile } from 'node:fs/promises'
import { existsSync } from 'node:fs'
import { join, resolve } from 'node:path'
import { buildImportPayload, upsertBlogRow } from './notion-db-import-lib.mjs'

const root = resolve(process.env.NOTION_EXPORT_DIR || join(process.cwd(), 'downloads', 'notion-connected-2026-03-27T03-08-20-083Z'))
const statusDir = join(process.cwd(), 'db_status')
const startedAt = new Date().toISOString()
const concurrency = Math.max(1, Number(process.env.NOTION_IMPORT_CONCURRENCY || '4'))
const singleThread = String(process.env.NOTION_IMPORT_SINGLE_THREAD || 'false').toLowerCase() === 'true'
const workerCount = singleThread ? 1 : concurrency

async function readJson(path, fallback = null) {
  try {
    return JSON.parse(await readFile(path, 'utf8'))
  } catch {
    return fallback
  }
}

async function writeStatus(status) {
  const payload = {
    ...status,
    updatedAt: new Date().toISOString(),
  }

  await writeFile(join(statusDir, 'current.json'), JSON.stringify(payload, null, 2))
}

function extractTitle(page) {
  const properties = page.properties || {}
  for (const property of Object.values(properties)) {
    if (property?.type === 'title') {
      return (property.title || []).map((item) => item.plain_text || '').join('').trim() || 'Untitled'
    }
  }
  return 'Untitled'
}

async function main() {
  if (!existsSync(join(root, 'pages.json'))) {
    throw new Error(`pages.json not found under ${root}`)
  }

  if (!existsSync(statusDir)) {
    await mkdir(statusDir, { recursive: true })
    await writeFile(join(statusDir, '.gitkeep'), '')
  }

  const pages = await readJson(join(root, 'pages.json'), [])
  const exportManifest = await readJson(join(root, 'manifest.json'), [])
  const legacyResults = await readJson(join(root, 'db-import-results.json'), [])
  const legacyManifest = await readJson(join(root, 'db-import-manifest.json'), [])
  const resultsPath = join(root, 'db-import-direct-results.json')
  const failuresPath = join(root, 'db-import-direct-failures.json')
  const results = await readJson(resultsPath, [])
  const failures = await readJson(failuresPath, [])
  const exportManifestByPageId = new Map(exportManifest.map((item) => [item.pageId, item]))
  const done = new Set([...legacyResults, ...legacyManifest, ...results].map((item) => item.pageId))
  let nextIndex = 0
  let writeQueue = Promise.resolve()

  console.log(`Importing ${pages.length} downloaded pages from ${root} with ${workerCount} worker(s)`)
  await writeStatus({
    mode: 'running',
    root,
    startedAt,
    totalPages: pages.length,
    imported: results.length,
    failed: failures.length,
    skipped: done.size,
    currentIndex: 0,
    currentTitle: null,
    concurrency: workerCount,
  })

  async function enqueuePersist(updateStatus) {
    writeQueue = writeQueue.then(async () => {
      await writeFile(resultsPath, JSON.stringify(results, null, 2))
      await writeFile(failuresPath, JSON.stringify(failures, null, 2))
      await writeStatus({
        mode: 'running',
        root,
        startedAt,
        totalPages: pages.length,
        imported: results.length,
        failed: failures.length,
        skipped: done.size,
        concurrency: workerCount,
        ...updateStatus,
      })
    })
    await writeQueue
  }

  async function processPage(i) {
    const page = pages[i]
    const title = extractTitle(page)
    if (done.has(page.id)) {
      await enqueuePersist({
        currentIndex: i + 1,
        currentTitle: title,
        lastEvent: 'skip',
      })
      console.log(`[${i + 1}/${pages.length}] ${title} (skip: already imported)`)
      return
    }

    await enqueuePersist({
      currentIndex: i + 1,
      currentTitle: title,
      lastEvent: 'processing',
    })
    console.log(`[${i + 1}/${pages.length}] ${title}`)

    try {
      const folderPath = exportManifestByPageId.get(page.id)?.folder || resolveFolderPath(root, i, page)
      const blocks = await readJson(join(folderPath, 'blocks.json'), [])
      const assetsManifest = await readJson(join(folderPath, 'assets-manifest.json'), [])
      const payload = await buildImportPayload(page, blocks, assetsManifest)
      const imported = await upsertBlogRow({ ...payload, page })

      upsertByPageId(results, {
        pageId: page.id,
        title,
        importedBlogId: imported.blogId,
        slug: payload.slug,
        status: imported.status,
      })
      removeByPageId(failures, page.id)
      done.add(page.id)
      await enqueuePersist({
        currentIndex: i + 1,
        currentTitle: title,
        lastEvent: 'imported',
        lastImportedSlug: payload.slug,
      })
    } catch (error) {
      upsertByPageId(failures, {
        pageId: page.id,
        title,
        error: error instanceof Error ? error.message : String(error),
      })
      await enqueuePersist({
        currentIndex: i + 1,
        currentTitle: title,
        lastEvent: 'failed',
        lastError: error instanceof Error ? error.message : String(error),
      })
      console.error(`[${i + 1}/${pages.length}] FAILED ${title}: ${error instanceof Error ? error.message : String(error)}`)
    }
  }

  async function worker() {
    while (nextIndex < pages.length) {
      const current = nextIndex
      nextIndex += 1
      await processPage(current)
    }
  }

  await Promise.all(Array.from({ length: workerCount }, () => worker()))

  await writeStatus({
    mode: 'completed',
    root,
    startedAt,
    totalPages: pages.length,
    imported: results.length,
    failed: failures.length,
    skipped: done.size,
    currentIndex: pages.length,
    currentTitle: null,
    lastEvent: 'completed',
    concurrency: workerCount,
  })
  console.log(JSON.stringify({
    root,
    imported: results.length,
    failures: failures.length,
  }, null, 2))
}

function resolveFolderPath(rootDir, index, page) {
  const title = extractTitle(page)
  const slugBase = title
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^\p{L}\p{N}-]+/gu, '')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '') || `page-${page.id.slice(0, 8)}`

  return join(rootDir, `${String(index + 1).padStart(4, '0')}-${slugBase}`)
}

function upsertByPageId(rows, nextRow) {
  const index = rows.findIndex((row) => row.pageId === nextRow.pageId)
  if (index === -1) {
    rows.push(nextRow)
    return
  }

  rows[index] = nextRow
}

function removeByPageId(rows, pageId) {
  const index = rows.findIndex((row) => row.pageId === pageId)
  if (index !== -1) {
    rows.splice(index, 1)
  }
}

await main()
