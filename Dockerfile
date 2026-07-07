FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ArcadiaDevs.Viora.Platform/ArcadiaDevs.Viora.Platform.csproj", "ArcadiaDevs.Viora.Platform/"]
RUN dotnet restore "ArcadiaDevs.Viora.Platform/ArcadiaDevs.Viora.Platform.csproj"

COPY . .
WORKDIR "/src/ArcadiaDevs.Viora.Platform"
RUN dotnet publish "ArcadiaDevs.Viora.Platform.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Render injects PORT at runtime (defaults to 10000) and requires the
# container to bind to it; local `docker run` without PORT set falls back
# to 8080. Must be resolved at container start (shell-form ENTRYPOINT), not
# baked in via a static ENV, since Render's PORT value isn't known at build
# time.
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet ArcadiaDevs.Viora.Platform.dll"]
