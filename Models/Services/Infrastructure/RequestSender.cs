using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OptionDependentHttpclientFactory.Models.Options;

namespace OptionDependentHttpclientFactory.Models.Services.Infrastructure
{
    public class RequestSender : IRequestSender
    {
        private readonly IOptionsMonitor<List<EndpointOption>> options;
        private readonly IMemoryCache cache;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public RequestSender(ILoggerFactory loggerFactory, IMemoryCache cache, IConfiguration configuration, IOptionsMonitor<List<EndpointOption>> options)
        {
            this.logger = loggerFactory.CreateLogger(nameof(RequestSender));
            this.configuration = configuration;
            this.cache = cache;
            this.options = options;
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            EndpointOption endpoint = SelectEndpointForRequest(request);
            string cacheKey = CalculateCacheKeyForEndpoint(endpoint);
            HttpClient client = GetOrCreateHttpClient(endpoint, cacheKey);
            HttpResponseMessage response = await client.SendAsync(request);
            return response;
        }

        private HttpClient GetOrCreateHttpClient(EndpointOption endpoint, string cacheKey)
        {
            var cachedHttpClient = cache.GetOrCreate<HttpClient>(cacheKey, entry => {
                //Questa entry della cache verrà invalidata al ricaricamento della configurazione
                entry.AddExpirationToken(configuration.GetReloadToken());
                //Mi sottoscrivo alla rimozione dalla cache, così che possa fare il dispose
                entry.PostEvictionCallbacks.Add(GetPostEvictionCallbackRegistration());

                //Creo l'istanza che verrà messa in cache
                HttpClient client = CreateHttpClient(endpoint);
                return client;
            });
            return cachedHttpClient;
        }

        private HttpClient CreateHttpClient(EndpointOption endpoint)
        {
            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(endpoint.Proxy))
            {
                handler.Proxy = new WebProxy(endpoint.Proxy);
                handler.UseProxy = true;
            }
            //TODO: dalle opzioni potrebbero arrivare anche autenticazione, headers, ecc.

            var client = new HttpClient(handler, disposeHandler: true);
            logger.LogInformation($"Created a new instance of HttpClient for endpoint '{endpoint.Name}'");
            return client;
        }

        private PostEvictionCallbackRegistration GetPostEvictionCallbackRegistration()
        {
            var registration = new PostEvictionCallbackRegistration();
            registration.EvictionCallback = DisposeValue;
            return registration;
        }

        private void DisposeValue(object key, object value, EvictionReason reason, object state)
        {
            logger.LogInformation($"Disposed {value?.GetType().Name} for key '{key}' after eviction for reason: '{reason}'");
            (value as IDisposable)?.Dispose();
        }

        private string CalculateCacheKeyForEndpoint(EndpointOption endpoint)
        {
            return $"endpoint-{endpoint.Selector}";
        }

        private EndpointOption SelectEndpointForRequest(HttpRequestMessage request)
        {
            var endpoints = options.CurrentValue;
            foreach (var endpoint in endpoints)
            {
                if (endpoint.Matches(request))
                {
                    logger.LogInformation($"Selected endpoint '{endpoint.Name}' for request to URL {request.RequestUri}");
                    return endpoint;
                }
            }
            string errorMessage = $"Couldn't selet an endpoint for request to URL '{request.RequestUri}'";
            logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }
}