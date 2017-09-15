using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Commerce.Connect.CommerceServer.Inventory.Models;
using Sitecore.Commerce.Entities.Inventory;
using Sitecore.Data.Items;
using Sitecore.Feature.Commerce.Catalog.Models;
using Sitecore.Feature.Commerce.Catalog.Services;
using Sitecore.Foundation.Commerce;
using Sitecore.Foundation.Commerce.Extensions;
using Sitecore.Foundation.Commerce.Managers;
using Sitecore.Foundation.Commerce.Models;
using Sitecore.Foundation.Commerce.Repositories;
using Sitecore.Foundation.DependencyInjection;
using Sitecore.Foundation.SitecoreExtensions.Extensions;
using Sitecore.Mvc.Presentation;

namespace Sitecore.Feature.Commerce.Catalog.Factories
{
    [Service]
    public class ProductViewModelFactory
    {
        public ProductViewModelFactory(CatalogItemContext catalogItemContext, CatalogManager catalogManager, InventoryManager inventoryManager, CommerceUserContext commerceUserContext, StorefrontContext storefrontContext, ProductOverlayImageService productOverlayImageService)
        {
            CatalogItemContext = catalogItemContext;
            CatalogManager = catalogManager;
            InventoryManager = inventoryManager;
            CommerceUserContext = commerceUserContext;
            StorefrontContext = storefrontContext;
            ProductOverlayImageService = productOverlayImageService;
        }

        public CatalogItemContext CatalogItemContext { get; }
        private CatalogManager CatalogManager { get; }
        private InventoryManager InventoryManager { get; }
        private CommerceUserContext CommerceUserContext { get; }
        private StorefrontContext StorefrontContext { get; }
        private ProductOverlayImageService ProductOverlayImageService { get; }

        public ProductViewModel CreateFromCatalogItemContext()
        {
            if (CatalogItemContext.IsCategory)
                return null;

            var productItem = CatalogItemContext.Current?.Item;
            if (productItem == null)
            {
                return null;
            }

            RenderingContext.Current.Rendering.Item = productItem;
            return Create(RenderingContext.Current.Rendering.Item);
        }

        public ProductViewModel Create(Item item, bool loadPrice = true, bool loadStock = true)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!IsValid(item))
            {
                throw new ArgumentException("Invalid item type. Must be a product.", nameof(item));
            }

            if (item.IsWildcardItem())
            {
                return CreateFromCatalogItemContext();
            }

            var cacheKey = $"CurrentProductViewModel{item.ID}";
            var productViewModel = GetFromCache(cacheKey);
            if (productViewModel != null)
            {
                return productViewModel;
            }

            var variants = item.Children.Select(c => new VariantViewModel(c)).ToList();

            productViewModel = new ProductViewModel(item, variants);
            productViewModel.ProductName = productViewModel.Title;

            PopulateCategoryInformation(productViewModel);

            if(loadStock)
                PopulateStockInformation(productViewModel);

            if(loadPrice)
                PopulatePriceInformation(productViewModel);

            PopulateRatings(productViewModel);
            PopulateImages(productViewModel);

            return AddToCache(cacheKey, productViewModel);
        }

        private void PopulateImages(ProductViewModel productViewModel)
        {
            productViewModel.OverlayImage = this.ProductOverlayImageService.GetProductOverlayImage(productViewModel);
        }

        private void PopulateRatings(ProductViewModel productViewModel)
        {
            productViewModel.CustomerAverageRating = CatalogManager.GetProductRating(productViewModel.Item);
        }

        private void PopulatePriceInformation(ProductViewModel productViewModel)
        {
            CatalogManager.GetProductPrice(productViewModel);
        }

        private void PopulateCategoryInformation(ProductViewModel productViewModel)
        {
            if (CatalogItemContext.Current == null)
            {
                return;
            }

            productViewModel.ParentCategoryId = CatalogItemContext.Current.CategoryId;
            var category = CatalogManager.GetCategory(productViewModel.ParentCategoryId);
            if (category != null)
            {
                productViewModel.ParentCategoryName = category.Title;
            }
        }

        public bool IsValid(Item item)
        {
            if (item == null)
                return false;
            if (item.IsDerived(Foundation.Commerce.Templates.Commerce.Product.Id))
                return true;
            return item.IsWildcardItem() && IsValid(CatalogItemContext.Current?.Item);
        }

        private void PopulateStockInformation(ProductViewModel model)
        {
            if (StorefrontContext.Current == null)
                return;

            var inventoryProducts = new List<CommerceInventoryProduct> { new CommerceInventoryProduct { ProductId = model.ProductId, CatalogName = model.CatalogName } };
            var response = InventoryManager.GetStockInformation(inventoryProducts, StockDetailsLevel.StatusAndAvailability);
            if (!response.ServiceProviderResult.Success || response.Result == null)
            {
                return;
            }

            var stockInfos = response.Result;
            var stockInfo = stockInfos.FirstOrDefault();
            if (stockInfo == null || stockInfo.Status == null)
            {
                return;
            }

            model.StockStatus = stockInfo.Status;
            model.StockStatusName = InventoryManager.GetStockStatusName(model.StockStatus);
            if (stockInfo.AvailabilityDate != null)
            {
                model.StockAvailabilityDate = stockInfo.AvailabilityDate.Value.ToDisplayedDate();
            }
        }


        public static ProductViewModel GetFromCache(string cacheKey)
        {
            var value = HttpRuntime.Cache[cacheKey];
            if (value is ProductViewModel)
                return value as ProductViewModel;

            return HttpContext.Current?.Items[cacheKey] as ProductViewModel;
        }

        public static ProductViewModel AddToCache(string cacheKey, ProductViewModel value)
        {
            if (HttpRuntime.Cache.Get(cacheKey) != null)
            {
                HttpRuntime.Cache[cacheKey] = value;
            }
            else
            {
                HttpRuntime.Cache.Add(  cacheKey,
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