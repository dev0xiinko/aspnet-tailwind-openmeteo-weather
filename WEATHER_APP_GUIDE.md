## Weather App Guide

### Overview

This project is an ASP.NET Core Razor Pages app with a Tailwind‑styled weather dashboard on the home page.  
It uses the free [Open‑Meteo APIs](https://open-meteo.com/en/docs) for:

- **Geocoding**: converting a city name into latitude/longitude.
- **Forecast**: fetching current conditions for those coordinates.

### Tech stack

- **Backend**: .NET 8, Razor Pages (`Index` page).
- **HTTP**: `HttpClientFactory` with named clients.
- **Weather**: Open‑Meteo Geocoding API + Forecast API.
- **Styling**: Tailwind CSS 3 compiled to `wwwroot/css/site.css`.

### How the weather flow works

1. **User search**
   - The search box on `Index.cshtml` submits a GET request with `city=...`.
   - `IndexModel.City` is bound via `[BindProperty(SupportsGet = true)]`.

2. **Geocoding (city → lat/lon)**
   - In `Index.cshtml.cs`, `OnGetAsync` calls the Open‑Meteo geocoding API:
     - `GET /v1/search?name={City}&count=1&language=en&format=json` on `https://geocoding-api.open-meteo.com`.
   - It takes the first result’s `latitude`, `longitude`, `name`, and `country`.

3. **Current weather**
   - With those coordinates, it calls the forecast API:
     - `GET /v1/forecast?latitude=..&longitude=..&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,cloud_cover,wind_speed_10m&timezone=auto&temperature_unit=celsius&wind_speed_unit=ms` on `https://api.open-meteo.com`.
   - It reads the `current` object and maps:
     - `temperature_2m` → `CurrentWeatherViewModel.Temperature`
     - `apparent_temperature` → `FeelsLike`
     - `relative_humidity_2m` → `Humidity`
     - `wind_speed_10m` → `WindSpeed`
     - `weather_code` → human‑readable `Description` via a small switch helper.

4. **Rendering**
   - `CurrentWeatherViewModel` is exposed as `Model.CurrentWeather`.
   - `Index.cshtml` uses that to populate:
     - City label
     - Temperature and feels‑like text
     - Description, humidity, and wind speed in the “Now” card.
   - The hourly band and 5‑day outlook are **sample/mock data only** for now.

### Key files

- `Program.cs`
  - Registers Razor Pages and two `HttpClient` instances:
    - `OpenMeteo` for forecast: base `https://api.open-meteo.com/v1/`
    - `OpenMeteoGeocoding` for geocoding: base `https://geocoding-api.open-meteo.com/v1/`

- `Pages/Index.cshtml.cs`
  - Page model (`IndexModel`) with:
    - `City` (GET‑bound search query).
    - `CurrentWeatherViewModel` (data for the UI).
    - `OnGetAsync` which performs geocoding + forecast calls.
    - A helper to map Open‑Meteo weather codes to descriptions.

- `Pages/Index.cshtml`
  - Tailwind UI for the hero, “Now” card, search form, hourly strip, and sample 5‑day outlook.
  - Binds directly to `@Model.City` and `@Model.CurrentWeather`.

- `wwwroot/css/input.css`
  - Tailwind input file with the `@tailwind` directives and a small global reset.

- `tailwind.config.js`
  - Content paths for Razor Pages and JS, plus default theme extension hooks.

### Running the app

1. **Install dependencies** (already done once per machine):

```bash
npm install
```

2. **Build Tailwind CSS**:

- For a one‑off build:

```bash
npm run build:css
```

- For live recompilation during development:

```bash
npm run dev:css
```

3. **Run the ASP.NET app**:

```bash
dotnet run
```

4. **Use the UI**

- Open the root URL (usually `https://localhost:5001` or `http://localhost:5000`).
- Enter a city (e.g. “London”, “Tokyo”, “New York”) and submit.
- The “Now” panel updates using live Open‑Meteo data. The hourly and 5‑day areas are visual samples.

### Extending the app

- **Hourly forecast**: call the same Open‑Meteo forecast endpoint with `hourly=` parameters and replace the mock hourly loop with real data.
- **5‑day forecast**: use `daily=` variables and bind them into the outlook list.
- **Units**: adjust `temperature_unit` or `wind_speed_unit` query parameters to switch to °F or mph.
- **Error states**: add user‑friendly messages when a city is not found or when the API is unavailable.

