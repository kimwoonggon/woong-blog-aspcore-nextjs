FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY backend/WoongBlog.sln ./backend/WoongBlog.sln
COPY backend/src/WoongBlog.Api/ ./backend/src/WoongBlog.Api/
COPY backend/tests/WoongBlog.Api.Tests/ ./backend/tests/WoongBlog.Api.Tests/

WORKDIR /src/backend/src/WoongBlog.Api
RUN dotnet restore ../../WoongBlog.sln
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV CODEX_HOME=/root/.codex
RUN apk add --no-cache bubblewrap ffmpeg nodejs npm \
    && npm install -g @openai/codex \
    && mkdir -p "$CODEX_HOME" \
    && chmod 700 "$CODEX_HOME"
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WoongBlog.Api.dll"]
