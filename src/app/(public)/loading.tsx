export default function PublicSegmentLoading() {
  return (
    <div className="container mx-auto flex min-h-[60vh] flex-col gap-6 px-4 py-8 md:px-6 md:py-12">
      <div className="h-10 w-48 animate-pulse rounded-full bg-muted/60" />
      <div className="grid gap-6 md:grid-cols-2">
        <div className="h-40 animate-pulse rounded-3xl bg-muted/50" />
        <div className="h-40 animate-pulse rounded-3xl bg-muted/50" />
      </div>
      <div className="h-72 animate-pulse rounded-3xl bg-muted/40" />
    </div>
  )
}
