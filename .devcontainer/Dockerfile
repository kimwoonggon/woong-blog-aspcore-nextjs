FROM mcr.microsoft.com/devcontainers/dotnet:dev-10.0-noble

SHELL ["/bin/bash", "-lc"]

RUN source /usr/local/share/nvm/nvm.sh \
    && nvm install 22.22.2 \
    && nvm alias default 22.22.2 \
    && nvm use 22.22.2 \
    && npm install -g @openai/codex \
    && ln -sf "$(command -v node)" /usr/local/bin/node \
    && ln -sf "$(command -v npm)" /usr/local/bin/npm \
    && ln -sf "$(command -v npx)" /usr/local/bin/npx \
    && ln -sf "$(command -v codex)" /usr/local/bin/codex
