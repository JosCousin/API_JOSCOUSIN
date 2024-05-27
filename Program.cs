using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AirQualityApp
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string NINJA_API_KEY = "CTTF0zvHuEzqCd8jfx60ng==DUg7wyfofck1Vgjy"; // Remplacez par votre clé API Ninja

        static async Task Main(string[] args)
        {
            Console.WriteLine("Please provide a country code (e.g., 'FR' for France):");
            string country = Console.ReadLine();

            List<City> cities = await FetchTopCitiesAsync(country);
            List<City> rankedCities = await RankCitiesByAirQualityAsync(cities);
            DisplayCities(rankedCities);
        }

        static async Task<List<City>> FetchTopCitiesAsync(string country)
        {
            string ninjaUrl = $"https://api.api-ninjas.com/v1/city?country={country}&limit=15";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-Api-Key", NINJA_API_KEY);
            var response = await client.GetStringAsync(ninjaUrl);
            var data = JsonConvert.DeserializeObject<List<NinjaCityResponse>>(response);
            List<City> cities = new List<City>();

            foreach (var item in data)
            {
                cities.Add(new City { Name = item.Name, State = item.Region, Country = country });
            }

            return cities;
        }

        static async Task<List<City>> RankCitiesByAirQualityAsync(List<City> cities)
        {
            List<City> rankedCities = new List<City>();

            foreach (var city in cities)
            {
                try
                {
                    string airQualityUrl = $"https://api.api-ninjas.com/v1/airquality?city={Uri.EscapeDataString(city.Name)}";
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("X-Api-Key", NINJA_API_KEY);
                    var response = await client.GetAsync(airQualityUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<AirQualityResponse>(responseBody);
                        city.AirQualityIndex = result.Aqi;
                        rankedCities.Add(city);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch AQI for {city.Name}: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching AQI for {city.Name}: {ex.Message}");
                }
            }

            rankedCities.Sort((x, y) => x.AirQualityIndex.CompareTo(y.AirQualityIndex));
            return rankedCities;
        }

        static void DisplayCities(List<City> cities)
        {
            Console.WriteLine("Cities ranked by air quality (from best to worst):");
            foreach (var city in cities)
            {
                Console.WriteLine($"{city.Name}, {city.State}: AQI = {city.AirQualityIndex}");
            }
        }
    }

    public class City
    {
        public string Name { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public int AirQualityIndex { get; set; }
    }

    public class NinjaCityResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }

    public class AirQualityResponse
    {
        [JsonProperty("aqi")]
        public int Aqi { get; set; }
    }
}
