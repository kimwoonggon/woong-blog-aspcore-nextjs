"use client"

import Image from 'next/image'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Upload } from 'lucide-react'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'

interface HomeContent {
    headline?: string
    introText?: string
    profileImageUrl?: string
}

interface HomePageEditorProps {
    pageId: string
    pageTitle: string
    initialContent: HomeContent
}

export function HomePageEditor({ pageId, pageTitle, initialContent }: HomePageEditorProps) {
    const router = useRouter()
    const [headline, setHeadline] = useState(initialContent.headline || 'Hi, I am John, Creative Technologist')
    const [introText, setIntroText] = useState(initialContent.introText || 'Amet minim mollit non deserunt ullamco est sit aliqua dolor do amet sint.')
    const [profileImageUrl, setProfileImageUrl] = useState(initialContent.profileImageUrl || '')
    const [isSaving, setIsSaving] = useState(false)
    const [isUploading, setIsUploading] = useState(false)

    async function handleImageUpload(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0]
        if (!file) return

        setIsUploading(true)

        const formData = new FormData()
        formData.append('file', file)
        formData.append('bucket', 'public-assets')

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/uploads`, {
                method: 'POST',
                body: formData,
            })

            if (response.ok) {
                const data = await response.json()
                console.log('Upload response:', data)
                setProfileImageUrl(data.url)
            } else {
                const errorData = await response.json()
                console.error('Upload failed:', errorData)
                alert('Failed to upload image: ' + (errorData.error || 'Unknown error'))
            }
        } catch (error) {
            console.error('Upload error:', error)
            alert('Failed to upload image')
        } finally {
            setIsUploading(false)
        }
    }

    async function handleSave() {
        setIsSaving(true)

        const content = {
            headline,
            introText,
            profileImageUrl,
        }

        try {
            const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/pages`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: pageId, title: pageTitle, contentJson: JSON.stringify(content) }),
            })

            if (response.ok) {
                router.refresh()
                alert('Home page saved successfully!')
            } else {
                alert('Failed to save')
            }
        } catch (error) {
            console.error('Save error:', error)
            alert('Failed to save')
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <div className="space-y-6 rounded-lg border bg-white p-6 dark:border-gray-800 dark:bg-gray-950">
            <div className="flex items-center justify-between">
                <h2 className="text-xl font-bold">Home Page - Hero Section</h2>
                <Button onClick={handleSave} disabled={isSaving}>
                    {isSaving ? 'Saving...' : 'Save Changes'}
                </Button>
            </div>

            {/* Profile Image */}
            <div className="space-y-2">
                <Label>Profile Image</Label>
                <div className="flex items-center gap-6">
                    <div className="relative h-32 w-32 overflow-hidden rounded-full bg-gray-200 dark:bg-gray-800">
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
                                No Image
                            </div>
                        )}
                    </div>
                    <div>
                        <label className="cursor-pointer">
                            <div className="flex items-center gap-2 rounded-md border px-4 py-2 hover:bg-gray-50 dark:hover:bg-gray-900">
                                <Upload size={16} />
                                {isUploading ? 'Uploading...' : 'Upload Image'}
                            </div>
                            <input
                                type="file"
                                accept="image/*"
                                className="hidden"
                                onChange={handleImageUpload}
                                disabled={isUploading}
                            />
                        </label>
                        {profileImageUrl && (
                            <button
                                type="button"
                                className="mt-2 text-sm text-red-500 hover:underline"
                                onClick={() => setProfileImageUrl('')}
                            >
                                Remove Image
                            </button>
                        )}
                    </div>
                </div>
            </div>

            {/* Headline */}
            <div className="space-y-2">
                <Label htmlFor="headline">Headline</Label>
                <Input
                    id="headline"
                    value={headline}
                    onChange={(e) => setHeadline(e.target.value)}
                    placeholder="Hi, I am John, Creative Technologist"
                />
                <p className="text-sm text-gray-500">This is the main title shown in the Hero section</p>
            </div>

            {/* Intro Text */}
            <div className="space-y-2">
                <Label htmlFor="introText">Intro Text</Label>
                <Textarea
                    id="introText"
                    value={introText}
                    onChange={(e) => setIntroText(e.target.value)}
                    rows={4}
                    placeholder="Your introduction text..."
                />
                <p className="text-sm text-gray-500">This appears below the headline in the Hero section</p>
            </div>
        </div>
    )
}
