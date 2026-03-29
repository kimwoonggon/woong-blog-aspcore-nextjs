import Link from 'next/link'

interface PublicPaginationProps {
  pathname: string
  currentPage: number
  totalPages: number
  pageSize: number
  ariaLabel: string
}

function getPageWindow(currentPage: number, totalPages: number, windowSize = 5) {
  const half = Math.floor(windowSize / 2)
  const start = Math.max(1, Math.min(currentPage - half, totalPages - windowSize + 1))
  const end = Math.min(totalPages, start + windowSize - 1)

  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
}

export function PublicPagination({
  pathname,
  currentPage,
  totalPages,
  pageSize,
  ariaLabel,
}: PublicPaginationProps) {
  const pageWindow = getPageWindow(currentPage, totalPages)

  return (
    <nav aria-label={ariaLabel} className="space-y-3">
      <div className="flex flex-wrap items-center justify-center gap-2">
        {pageWindow.map((pageNumber) => (
          <Link
            key={pageNumber}
            href={`${pathname}?page=${pageNumber}&pageSize=${pageSize}`}
            className={`rounded-full border px-3 py-1.5 text-sm font-medium transition-colors ${
              pageNumber === currentPage
                ? 'border-sky-400 bg-sky-500 text-white'
                : 'hover:bg-accent'
            }`}
          >
            {pageNumber}
          </Link>
        ))}
      </div>
      <div className="flex items-center justify-center gap-3">
        {currentPage > 1 ? (
          <Link
            href={`${pathname}?page=1&pageSize=${pageSize}`}
            className="rounded-full border px-4 py-2 text-sm font-medium hover:bg-accent"
          >
            처음
          </Link>
        ) : (
          <span className="rounded-full border px-4 py-2 text-sm text-muted-foreground">처음</span>
        )}
        {currentPage > 1 ? (
          <Link
            href={`${pathname}?page=${currentPage - 1}&pageSize=${pageSize}`}
            className="rounded-full border px-4 py-2 text-sm font-medium hover:bg-accent"
          >
            이전
          </Link>
        ) : (
          <span className="rounded-full border px-4 py-2 text-sm text-muted-foreground">이전</span>
        )}
        <span className="text-sm text-muted-foreground">
          {currentPage} / {totalPages}
        </span>
        {currentPage < totalPages ? (
          <Link
            href={`${pathname}?page=${currentPage + 1}&pageSize=${pageSize}`}
            className="rounded-full border px-4 py-2 text-sm font-medium hover:bg-accent"
          >
            다음
          </Link>
        ) : (
          <span className="rounded-full border px-4 py-2 text-sm text-muted-foreground">다음</span>
        )}
        {currentPage < totalPages ? (
          <Link
            href={`${pathname}?page=${totalPages}&pageSize=${pageSize}`}
            className="rounded-full border px-4 py-2 text-sm font-medium hover:bg-accent"
          >
            마지막
          </Link>
        ) : (
          <span className="rounded-full border px-4 py-2 text-sm text-muted-foreground">마지막</span>
        )}
      </div>
    </nav>
  )
}
