# Woong Blog

## Introduction

Woong Blog is a personal portfolio and publishing application for presenting works, study notes, media-rich project pages, and profile content.

The project is built as a split-stack web application: a Next.js public/admin frontend, an ASP.NET Core API backend, PostgreSQL persistence, and Docker-based runtime packaging.

## Technology Stack

- **Frontend:** Next.js 16, React, TypeScript, App Router, Tailwind CSS
- **Backend:** ASP.NET Core, .NET 10, C#
- **Database:** PostgreSQL 16
- **Runtime:** Docker Compose, nginx
- **Authentication:** ASP.NET Core cookie sessions with OpenID Connect integration
- **Media:** Backend-owned upload, storage, and public media serving
- **Testing:** Playwright, Vitest, xUnit
- **CI/CD:** GitHub Actions and GHCR image publishing

## Architecture

The application uses a browser-to-nginx entrypoint.

```text
Browser
  -> nginx
    -> Next.js frontend
    -> ASP.NET Core API
      -> PostgreSQL
      -> media storage
```

Runtime responsibilities are split clearly:

- **nginx** routes frontend pages, API traffic, and media requests.
- **Next.js** owns public pages, admin UI, server-rendered views, and client interactions.
- **ASP.NET Core** owns authentication, admin mutations, public APIs, uploads, media serving, and persistence boundaries.
- **PostgreSQL** stores content, settings, users, and media metadata.
- **Docker Compose** defines the local, staging, and production-like runtime shape.

The repository keeps `dev` as the integration branch and `main` as the production branch.

## License

This project is proprietary.

Copyright (c) 2026 Woonggon Kim. All rights reserved.

See [LICENSE](./LICENSE) for the full license terms.
