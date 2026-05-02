#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

exec dotnet test "$ROOT_DIR/backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj" --filter "Category=Unit" "$@"
