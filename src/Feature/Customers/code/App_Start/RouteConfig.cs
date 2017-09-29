//-----------------------------------------------------------------------
// <copyright file="RouteConfig.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the RouteConfig class.</summary>
//-----------------------------------------------------------------------
// Copyright 2016 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Foundation.Commerce.Models;

namespace Sitecore.Feature.Commerce.Customers
{
    public static class RouteConfig
    {
        private static readonly List<ApiControllerMapping> _apiInfoList = new List<ApiControllerMapping>
        {
            new ApiControllerMapping("account-getcurrentuser", "Customers", "GetCurrentUser"),
            new ApiControllerMapping("account-register", "Customers", "Register"),
            new ApiControllerMapping("account-addresslist", "Customers", "AddressList"),
            new ApiControllerMapping("account-addressdelete", "Customers", "AddressDelete"),
            new ApiControllerMapping("account-addressmodify", "Customers", "AddressModify"),
            new ApiControllerMapping("account-updateprofile", "Customers", "UpdateProfile"),
            new ApiControllerMapping("account-changepassword", "Customers", "ChangePassword"),
            new ApiControllerMapping("loyalty-activateaccount", "Loyalty", "ActivateAccount"),
            new ApiControllerMapping("loyalty-activeloyaltycards", "Loyalty", "ActiveLoyaltyCards"),
            new ApiControllerMapping("loyalty-getloyaltycards", "Loyalty", "GetLoyaltyCards"),
            new ApiControllerMapping("wishlist-addwishliststocart", "WishList", "AddWishListsToCart"),
            new ApiControllerMapping("wishlist-activewishlists", "WishList", "ActiveWishLists"),
            new ApiControllerMapping("wishlist-addtowishlist", "WishList", "AddToWishList"),
            new ApiControllerMapping("wishlist-createwishlist", "WishList", "CreateWishList"),
            new ApiControllerMapping("wishlist-deletelineitem", "WishList", "DeleteLineItem"),
            new ApiControllerMapping("wishlist-deletewishlist", "WishList", "DeleteWishList"),
            new ApiControllerMapping("wishlist-getwishlist", "WishList", "GetWishList"),
            new ApiControllerMapping("wishlist-updatelineitem", "WishList", "UpdateLineItem"),
            new ApiControllerMapping("wishlist-updatewishlist", "WishList", "UpdateWishList"),
        };

        public static void RegisterRoutes(RouteCollection routes)
        {
            foreach (var apiInfo in _apiInfoList)
            {
                routes.MapRoute(
                    apiInfo.Name,
                    apiInfo.Url,
                    new {controller = apiInfo.Controller, action = apiInfo.Action, id = UrlParameter.Optional});
            }

            routes.MapRoute(
                name: "logoff",
                url: "logoff",
                defaults: new { controller = "Customers", action = "LogOff", storefront = UrlParameter.Optional }
            );

            routes.MapRoute(
               name: "oauthv2redirect",
               url: "oauthv2redirect",
               defaults: new { controller = "Customers", action = "OAuthV2Redirect", userName = UrlParameter.Optional, siteName = UrlParameter.Optional }
            );

            routes.MapRoute(
               name: "acsredirect",
               url: "acsredirect",
               defaults: new { controller = "Customers", action = "AcsRedirect", userName = UrlParameter.Optional, siteName = UrlParameter.Optional }
            );
        }
    }
}