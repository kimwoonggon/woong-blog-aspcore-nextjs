import Link from 'next/link'

interface EdgePaginationNavProps {
  pathname: string
  currentPage: number
  totalPages: number
  pageSize: number
}

function navButtonClass(side: 'left' | 'right', disabled: boolean) {
  return `fixed top-1/2 z-40 hidden h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full border border-border/80 bg-background/90 text-lg font-semibold shadow-sm backdrop-blur transition-colors md:inline-flex ${
    side === 'left' ? 'left-4 lg:left-6' : 'right-4 lg:right-6'
  } ${disabled ? 'cursor-not-allowed opacity-40' : 'hover:bg-accent'}`
}

export function EdgePaginationNav({
  pathname,
  currentPage,
  totalPages,
  pageSize,
}: EdgePaginationNavProps) {
  const previousHref = currentPage > 1 ? `${pathname}?page=${currentPage - 1}&pageSize=${pageSize}` : null
  const nextHref = currentPage < totalPages ? `${pathname}?page=${currentPage + 1}&pageSize=${pageSize}` : null

  return (
    <>
      {previousHref ? (
        <Link href={previousHref} aria-label="이전 페이지로 가기" className={navButtonClass('left', false)}>
          {'<-'}
        </Link>
      ) : (
        <span aria-label="이전 페이지 없음" className={navButtonClass('left', true)}>
          {'<-'}
        </span>
      )}
      {nextHref ? (
        <Link href={nextHref} aria-label="다음 페이지로 가기" className={navButtonClass('right', false)}>
          {'->'}
        </Link>
      ) : (
        <span aria-label="다음 페이지 없음" className={navButtonClass('right', true)}>
          {'->'}
        </span>
      )}
    </>
  )
}
