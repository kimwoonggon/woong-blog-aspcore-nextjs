
import { BlogEditor } from '@/components/admin/BlogEditor'

export default function NewBlogPage() {
    return (
        <div className="space-y-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50">New Post</h1>
            <BlogEditor />
        </div>
    )
}
