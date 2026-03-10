## Weather Dashboard (ASP.NET + Tailwind + Open‑Meteo)

A modern weather dashboard built with **ASP.NET Core Razor Pages**, **Tailwind CSS**, and the free **Open‑Meteo** APIs for geocoding and forecasts.  
Type any city and see live current conditions, wrapped in a responsive, dark UI.

### Features

- **City search** with automatic geocoding (Open‑Meteo Geocoding API).
- **Current conditions** (temperature, feels‑like, humidity, wind, description) from Open‑Meteo.
- **Modern Tailwind UI** with responsive layout and glassy “Now” card.
- **Docker‑ready**: multi‑stage Dockerfile builds Tailwind assets and publishes the .NET app.

For a deeper technical walkthrough, see `WEATHER_APP_GUIDE.md`.

---

### Requirements

- .NET SDK 8.0
- Node.js + npm (for local Tailwind builds)
- (Optional) Docker

---

### Running locally (without Docker)

From the project root:

```bash
# 1) Install JS deps (once)
npm install

# 2a) Run Tailwind in watch mode during development
npm run dev:css

# OR 2b) Build Tailwind once
npm run build:css

# 3) Run the ASP.NET app
dotnet run
```

Open the URL printed by `dotnet run` (usually `https://localhost:5001` or `http://localhost:5000`) and search for a city.

---

### Docker

This repo includes a multi‑stage `Dockerfile` that:

1. Uses the .NET **SDK** image to:
   - Install Node.js + npm.
   - Run `npm install` and `npm run build:css` (Tailwind build).
   - `dotnet publish` the app into `/app/publish`.
2. Uses the smaller .NET **ASP.NET runtime** image to run the published app on port **8080**.

Build and run:

```bash
docker build -t practice1-weather .
docker run -p 8080:8080 --name practice1-weather practice1-weather
```

Then open `http://localhost:8080`.

---

### Deployment overview

You can deploy this app like any ASP.NET Core site:

1. **Publish**:

```bash
npm install
npm run build:css
dotnet publish -c Release -o out
```

2. Deploy the `out` folder to:
   - **Azure App Service** (zip deploy or GitHub Actions).
   - A Linux VM behind **NGINX** as a reverse proxy.
   - Any hosting platform that supports ASP.NET Core or Docker.

If you are using Docker, push the built image to a registry (GitHub Container Registry, Docker Hub, etc.) and run it on your platform of choice.

