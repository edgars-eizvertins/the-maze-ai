# ---- Build stage: compile the Blazor WebAssembly app to static files ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Labyrinth.slnx global.json ./
COPY src/Labyrinth.Shared/*.csproj  src/Labyrinth.Shared/
COPY src/Labyrinth.UI/*.csproj      src/Labyrinth.UI/
RUN dotnet restore src/Labyrinth.UI/Labyrinth.UI.csproj

COPY src/ src/
# In the container the UI is served same-origin behind nginx, so the API base
# URL must be relative — overwrite the dev value before publishing.
RUN echo '{ "ApiBaseUrl": "" }' > src/Labyrinth.UI/wwwroot/appsettings.json \
 && dotnet publish src/Labyrinth.UI/Labyrinth.UI.csproj -c Release -o /app

# ---- Runtime stage: serve with nginx ----
FROM nginx:alpine AS runtime
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY src/Labyrinth.UI/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
