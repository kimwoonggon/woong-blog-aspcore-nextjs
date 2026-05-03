#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
COVERAGE_ROOT="$ROOT_DIR/coverage/backend"
RUNSETTINGS="$BACKEND_DIR/coverage.runsettings"

usage() {
  cat <<'USAGE'
Usage:
  ./scripts/run-backend-coverage.sh <unit|component|integration|full> [dotnet test args...]

Examples:
  ./scripts/run-backend-coverage.sh unit
  ./scripts/run-backend-coverage.sh component --blame-hang --blame-hang-timeout 5m -v minimal
  ./scripts/run-backend-coverage.sh integration
  ./scripts/run-backend-coverage.sh full
USAGE
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

SUITE="${1:-full}"
if [[ $# -gt 0 ]]; then
  shift
fi

FILTER=""
case "$SUITE" in
  unit)
    LABEL="UnitTests"
    TARGET="$BACKEND_DIR/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj"
    FILTER="Category=Unit"
    ;;
  component)
    LABEL="ComponentTests"
    TARGET="$BACKEND_DIR/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj"
    FILTER="Category=Component"
    ;;
  integration)
    LABEL="IntegrationTests"
    TARGET="$BACKEND_DIR/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj"
    FILTER="Category=Integration"
    ;;
  full)
    LABEL="FullBackendSolution"
    TARGET="$ROOT_DIR/backend/WoongBlog.sln"
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac

RAW_DIR="$COVERAGE_ROOT/$SUITE/raw"
REPORT_DIR="$COVERAGE_ROOT/$SUITE/report"
HISTORY_DIR="$COVERAGE_ROOT/history/$SUITE"

rm -rf "$RAW_DIR" "$REPORT_DIR"
mkdir -p "$RAW_DIR" "$REPORT_DIR" "$HISTORY_DIR"

(
  cd "$BACKEND_DIR"
  dotnet tool restore
)

test_args=(
  test
  "$TARGET"
  "--collect:XPlat Code Coverage"
  --settings
  "$RUNSETTINGS"
  --results-directory
  "$RAW_DIR"
)

if [[ -n "$FILTER" ]]; then
  test_args+=(--filter "$FILTER")
fi

test_args+=("$@")

dotnet "${test_args[@]}"

(
  cd "$BACKEND_DIR"
  dotnet tool run reportgenerator -- \
    "-reports:$RAW_DIR/**/coverage.cobertura.xml" \
    "-targetdir:$REPORT_DIR" \
    "-historydir:$HISTORY_DIR" \
    "-reporttypes:Html;HtmlSummary;MarkdownSummaryGithub;JsonSummary;TextSummary;Cobertura" \
    "-title:WoongBlog Backend $LABEL Coverage"
)

printf 'Coverage report: %s\n' "$REPORT_DIR/index.html"
printf 'Coverage summary: %s\n' "$REPORT_DIR/SummaryGithub.md"
