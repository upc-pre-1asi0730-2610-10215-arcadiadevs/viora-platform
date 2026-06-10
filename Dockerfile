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

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArcadiaDevs.Viora.Platform.dll"]
