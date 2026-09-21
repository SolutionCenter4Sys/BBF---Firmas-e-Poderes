const apiUrl = (
  process.env.API_INTERNAL_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  "http://localhost:8080"
).replace(/\/+$/, "");

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  output: "standalone",
  staticPageGenerationTimeout: 180,
  experimental: {
    typedRoutes: false,
    serverActions: { bodySizeLimit: "50mb" }
  },
  async rewrites() {
    return [
      { source: "/health/:path*", destination: `${apiUrl}/health/:path*` }
    ];
  }
};

module.exports = nextConfig;
