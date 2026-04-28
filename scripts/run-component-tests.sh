#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

exec dotnet test "$ROOT_DIR/backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj" --filter "Category=Component" "$@"
