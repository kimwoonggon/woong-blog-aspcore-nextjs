FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Portfolio.sln ./
COPY src/Portfolio.Api/ ./src/Portfolio.Api/
COPY tests/Portfolio.Api.Tests/ ./tests/Portfolio.Api.Tests/

WORKDIR /src/src/Portfolio.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
