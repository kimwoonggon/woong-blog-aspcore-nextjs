#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

exec dotnet test "$ROOT_DIR/backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj" --filter "Category=Architecture" "$@"
