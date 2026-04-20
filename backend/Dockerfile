FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LittleMunchkins.Api/LittleMunchkins.Api.csproj LittleMunchkins.Api/
RUN dotnet restore LittleMunchkins.Api/LittleMunchkins.Api.csproj
COPY . .
RUN dotnet publish LittleMunchkins.Api/LittleMunchkins.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install ffmpeg for video preprocessing
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LittleMunchkins.Api.dll"]
