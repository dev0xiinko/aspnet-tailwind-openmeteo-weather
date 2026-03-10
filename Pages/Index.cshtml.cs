using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace practice1.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    [BindProperty(SupportsGet = true)]
    public string? City { get; set; }

    public CurrentWeatherViewModel? CurrentWeather { get; private set; }
    public bool IsRateLimited { get; private set; }

    public IndexModel(
        ILogger<IndexModel> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(City))
        {
            City = "San Francisco";
        }

        try
        {
            // 1) Geocode the city name to coordinates using Open-Meteo geocoding
            var geoClient = _httpClientFactory.CreateClient("OpenMeteoGeocoding");
            var geoResponse = await geoClient.GetAsync($"search?name={Uri.EscapeDataString(City)}&count=1&language=en&format=json");

            if (!geoResponse.IsSuccessStatusCode)
            {
                if (geoResponse.StatusCode == (HttpStatusCode)429)
                {
                    IsRateLimited = true;
                    _logger.LogWarning("Open-Meteo geocoding API rate limited (429) for city {City}", City);
                }
                else
                {
                    _logger.LogWarning("Open-Meteo geocoding API returned non-success status {StatusCode}", geoResponse.StatusCode);
                }

                CurrentWeather = GetMockWeather(City);
                return;
            }

            await using var geoStream = await geoResponse.Content.ReadAsStreamAsync();
            using var geoJson = await JsonDocument.ParseAsync(geoStream);

            var results = geoJson.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                _logger.LogWarning("No geocoding results for city {City}", City);
                CurrentWeather = GetMockWeather(City);
                return;
            }

            var location = results[0];
            var latitude = location.GetProperty("latitude").GetDouble();
            var longitude = location.GetProperty("longitude").GetDouble();
            var locationName = location.GetProperty("name").GetString();
            var country = location.TryGetProperty("country", out var countryProp)
                ? countryProp.GetString()
                : null;

            // 2) Fetch current weather from Open-Meteo
            var weatherClient = _httpClientFactory.CreateClient("OpenMeteo");
            var weatherUrl =
                $"forecast?latitude={latitude}&longitude={longitude}" +
                "&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,cloud_cover,wind_speed_10m" +
                "&timezone=auto&temperature_unit=celsius&wind_speed_unit=ms";

            var weatherResponse = await weatherClient.GetAsync(weatherUrl);
            if (!weatherResponse.IsSuccessStatusCode)
            {
                if (weatherResponse.StatusCode == (HttpStatusCode)429)
                {
                    IsRateLimited = true;
                    _logger.LogWarning("Open-Meteo weather API rate limited (429) for city {City}", City);
                }
                else
                {
                    _logger.LogWarning("Open-Meteo weather API returned non-success status {StatusCode}", weatherResponse.StatusCode);
                }

                CurrentWeather = GetMockWeather(City);
                return;
            }

            await using var weatherStream = await weatherResponse.Content.ReadAsStreamAsync();
            using var weatherJson = await JsonDocument.ParseAsync(weatherStream);

            var current = weatherJson.RootElement.GetProperty("current");

            CurrentWeather = new CurrentWeatherViewModel
            {
                City = country is null ? locationName ?? City! : $"{locationName}, {country}",
                Temperature = current.GetProperty("temperature_2m").GetDouble(),
                FeelsLike = current.GetProperty("apparent_temperature").GetDouble(),
                Description = MapWeatherCodeToDescription(current.GetProperty("weather_code").GetInt32()),
                Humidity = current.GetProperty("relative_humidity_2m").GetInt32(),
                WindSpeed = current.GetProperty("wind_speed_10m").GetDouble()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling Open-Meteo APIs");
            CurrentWeather = GetMockWeather(City);
        }
    }

    private static CurrentWeatherViewModel GetMockWeather(string? city)
    {
        return new CurrentWeatherViewModel
        {
            City = string.IsNullOrWhiteSpace(city) ? "San Francisco, CA" : city,
            Temperature = 22,
            FeelsLike = 21,
            Description = "Partly cloudy",
            Humidity = 62,
            WindSpeed = 3.5
        };
    }

    private static string MapWeatherCodeToDescription(int code)
    {
        // WMO weather codes from Open-Meteo docs: https://open-meteo.com/en/docs
        return code switch
        {
            0 => "Clear sky",
            1 or 2 or 3 => "Partly cloudy",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            56 or 57 => "Freezing drizzle",
            61 or 63 or 65 => "Rain",
            66 or 67 => "Freezing rain",
            71 or 73 or 75 => "Snowfall",
            77 => "Snow grains",
            80 or 81 or 82 => "Rain showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => "Unknown"
        };
    }
}

public class CurrentWeatherViewModel
{
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
}
