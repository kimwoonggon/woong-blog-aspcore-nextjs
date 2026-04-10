import { useEffect } from 'react'

export function useBatchJobPolling({
  isOpen,
  activeJobId,
  activeJobStatus,
  loadRecentJobs,
  loadJobDetail,
}: {
  isOpen: boolean
  activeJobId: string | null
  activeJobStatus?: string | null
  loadRecentJobs: (nextActiveJobId?: string | null) => Promise<void>
  loadJobDetail: (jobId: string) => Promise<void>
}) {
  useEffect(() => {
    if (!isOpen || !activeJobId || !activeJobStatus || !['queued', 'running'].includes(activeJobStatus)) {
      return
    }

    const timer = window.setInterval(() => {
      void loadRecentJobs(activeJobId)
      void loadJobDetail(activeJobId)
    }, 2000)

    return () => {
      window.clearInterval(timer)
    }
  }, [activeJobId, activeJobStatus, isOpen, loadJobDetail, loadRecentJobs])
}
