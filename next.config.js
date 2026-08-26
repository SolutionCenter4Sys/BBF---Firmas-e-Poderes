/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  experimental: {
    typedRoutes: false,
    serverActions: { bodySizeLimit: "50mb" }
  }
};

module.exports = nextConfig;
