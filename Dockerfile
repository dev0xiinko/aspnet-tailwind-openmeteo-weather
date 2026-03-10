FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Install Node.js + npm for Tailwind build
RUN apt-get update \
    && apt-get install -y nodejs npm \
    && rm -rf /var/lib/apt/lists/*

# Copy everything and restore
COPY . .

RUN dotnet restore "./practice1.csproj"

# Build frontend assets with Tailwind and publish the app
RUN npm install \
    && npm run build:css \
    && dotnet publish "./practice1.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "practice1.dll"]

