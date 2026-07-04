# ---- Build stage ----
# The base image is pulled for the build's target platform (native on the Pi, or
# emulated by `docker buildx --platform`), so a plain publish targets that arch.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (copy only csproj/sln first for layer caching)
COPY Labyrinth.slnx global.json ./
COPY src/Labyrinth.Domain/*.csproj           src/Labyrinth.Domain/
COPY src/Labyrinth.Application/*.csproj       src/Labyrinth.Application/
COPY src/Labyrinth.Infrastructure/*.csproj    src/Labyrinth.Infrastructure/
COPY src/Labyrinth.Shared/*.csproj            src/Labyrinth.Shared/
COPY src/Labyrinth.Api/*.csproj               src/Labyrinth.Api/
RUN dotnet restore src/Labyrinth.Api/Labyrinth.Api.csproj

# Build & publish
COPY src/ src/
RUN dotnet publish src/Labyrinth.Api/Labyrinth.Api.csproj -c Release --no-restore -o /app

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# SQLite database lives on a mounted volume so saves survive container restarts.
VOLUME ["/data"]
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Labyrinth.Api.dll"]
