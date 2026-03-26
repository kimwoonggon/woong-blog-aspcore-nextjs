
import Link from 'next/link'
import Image from 'next/image'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { fetchPublicHome } from '@/lib/api/home'
import { parsePageContentJson, toHomeContent } from '@/lib/content/page-content'

export const dynamic = 'force-dynamic'

export default async function HomePage() {
  const payload = await fetchPublicHome()
  const homeContent = toHomeContent(parsePageContentJson(payload?.homePage?.contentJson))

  const headline = homeContent.headline || 'Hi, I am John, Creative Technologist'
  const introText = homeContent.introText || 'Amet minim mollit non deserunt ullamco est sit aliqua dolor do amet sint. Velit officia consequat duis enim velit mollit. Exercitation veniam consequat sunt nostrud amet.'
  const profileImageUrl = homeContent.profileImageUrl || ''
  const recentPosts = payload?.recentPosts || []
  const featuredWorks = payload?.featuredWorks || []

  return (
    <div className="container mx-auto flex flex-col gap-16 px-4 py-8 md:px-6 md:py-12">
      <section className="flex flex-col-reverse items-center justify-between gap-8 md:flex-row md:items-start md:gap-12">
        <div className="flex flex-1 flex-col items-center text-center md:items-start md:text-left">
          <h1
            className="mb-4 text-4xl font-heading font-bold tracking-tight text-gray-900 md:text-5xl lg:text-6xl dark:text-gray-50 opacity-0 animate-fade-in-up"
            style={{ animationDelay: '100ms' }}
          >
            {headline}
          </h1>
          <p
            className="mb-8 max-w-[600px] text-lg text-gray-600 dark:text-gray-400 opacity-0 animate-fade-in-up"
            style={{ animationDelay: '200ms' }}
          >
            {introText}
          </p>
        </div>
        <div className="flex-shrink-0 opacity-0 animate-fade-in-up" style={{ animationDelay: '0ms' }}>
          <div className="relative h-60 w-60 overflow-hidden rounded-full bg-gray-200 shadow-xl dark:bg-gray-800">
            {profileImageUrl ? (
              <Image
                src={profileImageUrl}
                alt="Profile"
                fill
                className="object-cover"
                unoptimized
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-gray-400">
                Avatar
              </div>
            )}
          </div>
        </div>
      </section>

      <section className="bg-[#EDF7FA] -mx-4 px-4 py-8 md:-mx-6 md:px-6 dark:bg-gray-900/50">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-medium text-gray-900 md:text-2xl dark:text-gray-50">
            Recent posts
          </h2>
          <Link
            href="/blog"
            className="text-sm font-medium text-[#00A8CC] hover:underline dark:text-[#00A8CC]"
          >
            View all
          </Link>
        </div>
        <div className="grid gap-6 md:grid-cols-2">
          {recentPosts.length > 0 ? (
            recentPosts.map((post) => {
              const publishDate = post.publishedAt
                ? new Date(post.publishedAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })
                : 'Unknown Date'

              return (
                <Card key={post.id} className="border-none shadow-sm">
                  <CardHeader>
                    <CardTitle className="text-xl font-bold">
                      <Link href={`/blog/${post.slug}`} className="hover:text-[#00A8CC] transition-colors">
                        {post.title}
                      </Link>
                    </CardTitle>
                    <div className="flex gap-4 text-base text-gray-600 dark:text-gray-400">
                      <span>{publishDate}</span>
                      {post.tags?.[0] && (
                        <span className="border-l border-gray-400 pl-4">{post.tags[0]}</span>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="line-clamp-3 text-gray-600 dark:text-gray-300">
                      {post.excerpt}
                    </p>
                  </CardContent>
                </Card>
              )
            })
          ) : (
            <div className="col-span-2 py-8 text-center text-gray-500">
              No recent posts found.
            </div>
          )}
        </div>
      </section>

      <section>
        <div className="mb-6">
          <h2 className="text-xl font-bold text-gray-900 md:text-2xl dark:text-gray-50">
            Featured works
          </h2>
        </div>
        <div className="flex flex-col gap-6">
          {featuredWorks.length > 0 ? (
            featuredWorks.map((work) => {
              const thumbnailUrl = work.thumbnailUrl || null
              const publishDate = work.publishedAt
                ? new Date(work.publishedAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })
                : 'Unknown Date'

              return (
                <div key={work.id} className="flex flex-col gap-6 border-b border-gray-200 pb-6 md:flex-row dark:border-gray-800">
                  <Link
                    href={`/works/${work.slug}`}
                    className="h-48 w-full flex-shrink-0 overflow-hidden rounded-md bg-gray-200 md:w-64 dark:bg-gray-800 relative block group border"
                  >
                    {thumbnailUrl ? (
                      <Image
                        src={thumbnailUrl}
                        alt={work.title}
                        fill
                        className="object-cover transition-transform duration-300 group-hover:scale-105"
                        unoptimized
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center text-gray-400 font-medium">
                        No Image
                      </div>
                    )}
                  </Link>
                  <div className="flex flex-1 flex-col justify-start">
                    <Link href={`/works/${work.slug}`} className="mb-4 text-2xl font-bold text-gray-900 hover:text-[#F3434F] dark:text-gray-50 transition-colors">
                      {work.title}
                    </Link>
                    <div className="mb-4 flex flex-wrap items-center gap-4">
                      <span className="rounded-full bg-[#142850] px-3 py-1 text-sm font-bold text-white">
                        {publishDate}
                      </span>
                      <span className="text-gray-500 dark:text-gray-400 font-medium">
                        {work.category}
                      </span>
                      {work.period && (
                        <span className="text-sm border-l pl-4 text-gray-400 dark:text-gray-500 font-mono">
                          {work.period}
                        </span>
                      )}
                    </div>
                    <p className="text-gray-600 dark:text-gray-300 line-clamp-2 leading-relaxed">
                      {work.excerpt || 'Click to view details'}
                    </p>
                  </div>
                </div>
              )
            })
          ) : (
            <div className="py-8 text-center text-gray-500">
              No featured works found.
            </div>
          )}
        </div>
      </section>
    </div>
  )
}
