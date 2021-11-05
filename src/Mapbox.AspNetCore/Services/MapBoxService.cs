﻿using Mapbox.AspNetCore.Helpers;
using Mapbox.AspNetCore.Interfaces;
using Mapbox.AspNetCore.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Mapbox.AspNetCore.Services
{
    public class MapBoxService : IMapboxService
    {
        private readonly IMapboxKeyService mapboxKeyService;
        private readonly HttpClient httpClient;

        public MapBoxService(HttpClient httpClient, IMapboxKeyService mapboxKeyService)
        {
            this.mapboxKeyService = mapboxKeyService;
            this.httpClient = httpClient;
        }

        public async Task<MapboxResults> GeocodingAsync(GeocodingParameters parameters)
        {
            string apiKey = mapboxKeyService.ApiKey();

            if (string.IsNullOrEmpty(parameters.Query))
            {
                throw new ArgumentException("Query is required");
            }

            string urlQuery = Constants.URL_GEOCODING_API + "mapbox.places/";

            urlQuery += $"{HttpUtility.UrlEncode(parameters.Query)}.json";
            urlQuery += $"?limit={parameters.Limit}";

            if (!string.IsNullOrEmpty(parameters.CountryCode))
            {
                urlQuery += $"&country={parameters.CountryCode}";
            }

            if (parameters.AutoComplete)
            {
                urlQuery += "&autocomplete=true";
            }

            if (parameters.Proximity != null && parameters.Proximity.Latitude != 0d && parameters.Proximity.Longitude != 0d)
            {
                string latitude = parameters.Proximity.Latitude.ToString("0.000", CultureInfo.InvariantCulture);
                string longitude = parameters.Proximity.Longitude.ToString("0.000", CultureInfo.InvariantCulture);

                urlQuery += $"&proximity={longitude}%2C{latitude}";
            }

            urlQuery += $"&access_token={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Get, urlQuery);
            request.Headers.Add("Accept", "application/vnd.github.v3+json");

            var response = await this.httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var apiResults = JsonSerializer.Deserialize<MapboxApiResult>(responseJson);

                if (parameters.MinRelevance != 0d && apiResults.features != null)
                {
                    apiResults.features = apiResults.features.Where(f => f.relevance >= parameters.MinRelevance).ToList();
                }

                var results = apiResults.ConvertResults();

                if (!string.IsNullOrEmpty(parameters.PostCodeOnly) && results.Places != null)
                {
                    results.Places = results.Places.Where(x => x.City.PostCode == parameters.PostCodeOnly).ToList();
                }

                return results;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }

        public async Task<MapboxResult> ReverseGeocodingAsync(ReverseGeocodingParameters parameters)
        {
            string apiKey = mapboxKeyService.ApiKey();

            string urlQuery = Constants.URL_GEOCODING_API + "mapbox.places/";
            if (parameters.Coordinates != null && parameters.Coordinates.Latitude != 0d && parameters.Coordinates.Longitude != 0d)
            {
                string latitude = parameters.Coordinates.Latitude.ToString("0.000", CultureInfo.InvariantCulture);
                string longitude = parameters.Coordinates.Longitude.ToString("0.000", CultureInfo.InvariantCulture);

                urlQuery += $"{longitude}%2C{latitude}.json?limit=1";
            }
            else
            {
                throw new ArgumentException("Coordinates are required");
            }

            urlQuery += $"&access_token={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Get, urlQuery);
            request.Headers.Add("Accept", "application/vnd.github.v3+json");

            var response = await this.httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var apiResults = JsonSerializer.Deserialize<MapboxApiResult>(responseString).ConvertResults();
                return new MapboxResult() { Place = apiResults.Places.FirstOrDefault() };
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }
    }
}
