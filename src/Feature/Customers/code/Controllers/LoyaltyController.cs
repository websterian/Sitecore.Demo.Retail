//-----------------------------------------------------------------------
// <copyright file="LoyaltyController.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the LoyaltyController class.</summary>
//-----------------------------------------------------------------------
// Copyright 2016 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------
namespace Sitecore.Feature.Commerce.Customers.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using Sitecore;
    using Sitecore.Commerce.Contacts;
    using Sitecore.Commerce.Entities.LoyaltyPrograms;
    using Sitecore.Diagnostics;
    using Sitecore.Feature.Commerce.Customers.Models;
    using Sitecore.Foundation.Commerce;
    using Sitecore.Foundation.Commerce.Controllers;
    using Sitecore.Foundation.Commerce.Extensions;
    using Sitecore.Foundation.Commerce.Managers;
    using Sitecore.Foundation.Commerce.Models;

    using LoyaltyRewardPoint = Sitecore.Commerce.Connect.DynamicsRetail.Entities.LoyaltyPrograms.LoyaltyRewardPoint;

    /// <summary>
    /// Controller for the Loyalty program
    /// </summary>
    public class LoyaltyController : AxBaseController
    {
        #region Properties

        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyController" /> class.
        /// </summary>
        /// <param name="loyaltyProgramManager">The loyalty program manager.</param>
        /// <param name="cartManager">The cart manager.</param>
        /// <param name="accountManager">The account manager.</param>
        /// <param name="contactFactory">The contact factory.</param>
        /// <param name="commerceUserContext"></param>
        /// <param name="storefrontContext"></param>
        public LoyaltyController(
            LoyaltyProgramManager loyaltyProgramManager, 
            CartManager cartManager, 
            AccountManager accountManager, 
            ContactFactory contactFactory,
            CommerceUserContext commerceUserContext,
            StorefrontContext storefrontContext)
            : base(accountManager, contactFactory)
        {
            this.LoyaltyProgramManager = loyaltyProgramManager;
            this.CartManager = cartManager;
            this.CommerceUserContext = commerceUserContext;
            this.StorefrontContext = storefrontContext;
        }

        /// <summary>
        /// Gets the storefront context.
        /// </summary>
        public StorefrontContext StorefrontContext { get; }

        /// <summary>
        /// Gets the commerce user context.
        /// </summary>
        private CommerceUserContext CommerceUserContext { get; }

        /// <summary>
        /// Gets or sets the loyalty program manager.
        /// </summary>
        /// <value>
        /// The loyalty program manager.
        /// </value>
        public LoyaltyProgramManager LoyaltyProgramManager { get; protected set; }

        /// <summary>
        /// Gets or sets the cart manager.
        /// </summary>
        /// <value>
        /// The cart manager.
        /// </value>
        public CartManager CartManager { get; protected set; }

        #endregion

        #region Controller actions

        /// <summary>
        ///  Main controller action
        /// </summary>
        /// <returns>My loyalty cards view</returns>
        [HttpGet]
        public override ActionResult Index()
        {
            if (!Context.User.IsAuthenticated)
            {
                return this.Redirect("/login");
            }

            return this.View("~/Views/Customers/LoyaltyCards.cshtml");
        }

        /// <summary>
        /// Gets the loyalty cards.
        /// </summary>
        /// <returns>A list of loyalty cards</returns>
        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public JsonResult GetLoyaltyCards()
        {
            try
            {
                var loyaltyCards = new List<LoyaltyCard>();
                var userResponse = this.AccountManager.GetUser(Context.User.Name);
                var result = new LoyaltyCardsBaseApiModel(userResponse.ServiceProviderResult);
                if (userResponse.ServiceProviderResult.Success && userResponse.Result != null)
                {
                    loyaltyCards = this.AllCards(result, true);
                    if (result.Success)
                    {
                        foreach (var card in loyaltyCards)
                        {
                            foreach (var loyaltyRewardPoint in card.RewardPoints)
                            {
                                var point = (LoyaltyRewardPoint)loyaltyRewardPoint;
                                var transactionResponse = this.LoyaltyProgramManager.GetLoyaltyCardTransactions(card.ExternalId, point.RewardPointId, 50);
                                if (transactionResponse.ServiceProviderResult.Success && transactionResponse.Result != null)
                                {
                                    point.SetPropertyValue("Transactions", transactionResponse.Result.ToList());
                                }

                                result.SetErrors(transactionResponse.ServiceProviderResult);
                            }
                        }
                    }
                }

                result.Initialize(loyaltyCards);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Sitecore.Commerce.OpenIDConnectionClosedUnexpectedlyException e)
            {
                this.CleanNotAuthorizedSession();
                return this.Json(new ErrorApiModel("Login", e), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("GetLoyaltyCards", e), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// The active loyalty cards.
        /// </summary>
        /// <returns>
        /// The <see cref="JsonResult"/>.
        /// </returns>
        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public JsonResult ActiveLoyaltyCards()
        {
            try
            {
                var loyaltyCards = new List<LoyaltyCard>();
                var userResponse = this.AccountManager.GetUser(Context.User.Name);
                var result = new LoyaltyCardsBaseApiModel(userResponse.ServiceProviderResult);
                if (userResponse.ServiceProviderResult.Success && userResponse.Result != null)
                {
                    loyaltyCards = this.AllCards(result, false).Take(5).ToList();
                }

                result.Initialize(loyaltyCards);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Sitecore.Commerce.OpenIDConnectionClosedUnexpectedlyException e)
            {
                this.CleanNotAuthorizedSession();
                return this.Json(new ErrorApiModel("Login", e), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("ActiveLoyaltyCards", e), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// The activate account.
        /// </summary>
        /// <returns>
        /// The <see cref="JsonResult"/>.
        /// </returns>
        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public JsonResult ActivateAccount()
        {
            try
            {
                var loyaltyCards = new List<LoyaltyCard>();
                var userResponse = this.AccountManager.GetUser(Context.User.Name);
                var result = new LoyaltyCardsBaseApiModel(userResponse.ServiceProviderResult);
                if (userResponse.ServiceProviderResult.Success && userResponse.Result != null)
                {
                    var response = this.LoyaltyProgramManager.ActivateAccount(this.StorefrontContext.Current, this.CommerceUserContext.Current.UserId);
                    result.SetErrors(response.ServiceProviderResult);

                    if (response.ServiceProviderResult.Success && response.Result != null && !string.IsNullOrEmpty(response.Result.CardNumber))
                    {
                        loyaltyCards = this.AllCards(result, false);
                    }
                }

                result.Initialize(loyaltyCards);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Sitecore.Commerce.OpenIDConnectionClosedUnexpectedlyException e)
            {
                this.CleanNotAuthorizedSession();
                return this.Json(new ErrorApiModel("Login", e), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("ActivateAccount", e), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// The all cards.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <param name="withDetails">
        /// The with details.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        private List<LoyaltyCard> AllCards(BaseApiModel result, bool withDetails)
        {
            var response = this.LoyaltyProgramManager.GetLoyaltyCards(this.StorefrontContext.Current, this.CommerceUserContext.Current.UserId, withDetails);
            var cards = new List<LoyaltyCard>();
            if (response.ServiceProviderResult.Success && response.Result != null)
            {
                cards = response.Result.ToList();
            }

            result.SetErrors(response.ServiceProviderResult);
            return cards;
        }

        #endregion
    }
}