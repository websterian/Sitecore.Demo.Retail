using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Commerce.Services.Orders;
using Sitecore.Diagnostics;
using Sitecore.Foundation.Commerce.Extensions;
using Sitecore.Foundation.Commerce.Models;
using Sitecore.Foundation.Commerce.Util;

namespace Sitecore.Foundation.Commerce.Managers
{
    public class CountryManager : IManager
    {
        public CountryManager(OrderServiceProvider orderServiceProvider)
        {
            Assert.ArgumentNotNull(orderServiceProvider, nameof(orderServiceProvider));

            OrderServiceProvider = orderServiceProvider;
        }

        private OrderServiceProvider OrderServiceProvider { get; set; }

        public ManagerResponse<GetAvailableCountriesResult, Dictionary<string, string>> GetAvailableCountries()
        {
            var request = new GetAvailableCountriesRequest();
            var result = new GetAvailableCountriesResult();
            var cachedCountries = GetFromCache<GetAvailableCountriesResult>("GetAvailableCountriesResult");

            if(cachedCountries != null)
            {
                result = cachedCountries;
            }
            else
            {
                result = OrderServiceProvider.GetAvailableCountries(request);
                AddToCache("GetAvailableCountriesResult", result);
            }

            result.WriteToSitecoreLog();

            var response = new ManagerResponse<GetAvailableCountriesResult, Dictionary<string, string>>(result, new Dictionary<string, string>(result.AvailableCountries));
            
            return response;
        }

        public ManagerResponse<GetAvailableRegionsResult, Dictionary<string, string>> GetAvailableRegions(string countryCode)
        {
            Assert.ArgumentNotNullOrEmpty(countryCode, nameof(countryCode));

            var request = new GetAvailableRegionsRequest(countryCode);
            var result = new GetAvailableRegionsResult();
            var key = $"GetAvailableRegionsResult{countryCode}";

            var cachedRegions = GetFromCache<GetAvailableRegionsResult>(key);

            if (cachedRegions != null)
            {
                result = cachedRegions;
            }
            else
            {
                result = OrderServiceProvider.GetAvailableRegions(request);
                AddToCache(key, result);
            }

            result.WriteToSitecoreLog();

            var response = new ManagerResponse<GetAvailableRegionsResult, Dictionary<string, string>>(result, new Dictionary<string, string>(result.AvailableRegions));
            
            return response;
        }

        public static T GetFromCache<T>(string cacheKey)
        {
            var value = HttpRuntime.Cache[cacheKey];
            if (value is T)
                return (T) value;

            return default(T);
        }

        public static T AddToCache<T>(string cacheKey, T value)
        {
            if (HttpRuntime.Cache.Get(cacheKey) != null)
            {
                HttpRuntime.Cache[cacheKey] = value;
            }
            else
            {
                HttpRuntime.Cache.Add(cacheKey,
                                        value,
                                        null,
                                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                                        System.Web.Caching.Cache.NoSlidingExpiration,
                                        System.Web.Caching.CacheItemPriority.Default,
                                        null);
            }
            return value;
        }
    }
}