import type { NextConfig } from "next";

const devProxyOrigin = process.env.DEV_PROXY_ORIGIN?.replace(/\/$/, '')

const nextConfig: NextConfig = {
  distDir: process.env.NEXT_DIST_DIR ?? '.next',
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'wrkainkcjswuuotxztrx.supabase.co',
        port: '',
        pathname: '/storage/v1/object/public/**',
      },
      {
        protocol: 'https',
        hostname: 'img.youtube.com',
        port: '',
        pathname: '/vi/**',
      },
    ],
  },
  output: 'standalone',
  async rewrites() {
    if (!devProxyOrigin) {
      return []
    }

    return [
      {
        source: '/api/:path*',
        destination: `${devProxyOrigin}/api/:path*`,
      },
      {
        source: '/media/:path*',
        destination: `${devProxyOrigin}/media/:path*`,
      },
    ]
  },
};

export default nextConfig;
