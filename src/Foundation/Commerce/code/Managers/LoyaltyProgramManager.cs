//-----------------------------------------------------------------------
// <copyright file="LoyaltyProgramManager.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>The manager class responsible for encapsulating the loyalty program business logic for the site.</summary>
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

namespace Sitecore.Foundation.Commerce.Managers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Sitecore.Commerce.Connect.CommerceServer.Orders.Models;
    using Sitecore.Commerce.Connect.DynamicsRetail.Services.LoyaltyPrograms;
    using Sitecore.Commerce.Entities.LoyaltyPrograms;
    using Sitecore.Commerce.Services;
    using Sitecore.Commerce.Services.LoyaltyPrograms;
    using Sitecore.Diagnostics;
    using Sitecore.Foundation.Commerce.Extensions;
    using Sitecore.Foundation.Commerce.Models;

    /// <summary>
    /// Defines the LoyaltyProgramManager class.
    /// </summary>
    public class LoyaltyProgramManager : IManager
    {
        #region Constructors



        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyProgramManager" /> class.
        /// </summary>
        /// <param name="loyaltyProgramServiceProvider">The loyalty program service provider.</param>
        /// <param name="cartManager">The cart manager.</param>
        public LoyaltyProgramManager(LoyaltyCardServiceProvider loyaltyProgramServiceProvider, CartManager cartManager)
        {
            this.LoyaltyProgramServiceProvider = loyaltyProgramServiceProvider;
            this.CartManager = cartManager;
        }

        #endregion

        #region Properties (public)

        /// <summary>
        /// Gets or sets the loyalty program service provider.
        /// </summary>
        /// <value>
        /// The loyalty program service provider.
        /// </value>
        public LoyaltyCardServiceProvider LoyaltyProgramServiceProvider { get; protected set; }

        /// <summary>
        /// Gets or sets the cart manager.
        /// </summary>
        /// <value>
        /// The cart manager.
        /// </value>
        public CartManager CartManager { get; protected set; }

        #endregion

        #region Methods (public. virtual)

        /// <summary>
        /// Activates the loyalty account for the current user cart.
        /// </summary>
        /// <param name="storefront">
        /// The storefront.
        /// </param>
        /// <param name="userId">
        /// The user Id.
        /// </param>
        /// <returns>
        /// The manager response where the loyalty card is returned in the Result.
        /// </returns>
        public virtual ManagerResponse<JoinLoyaltyProgramResult, LoyaltyCard> ActivateAccount([NotNull] CommerceStorefront storefront, string userId)
        {
            var result = new JoinLoyaltyProgramResult { Success = false };
            var cartResult = this.CartManager.GetCart(userId);
            if (!cartResult.ServiceProviderResult.Success || cartResult.Result == null)
            {
                result.SystemMessages.ToList().AddRange(cartResult.ServiceProviderResult.SystemMessages);
                return new ManagerResponse<JoinLoyaltyProgramResult, LoyaltyCard>(result, null);
            }

            var errorResult = new JoinLoyaltyProgramResult() { Success = false };

            // Limit the number of loyalty programs a user may join.
            var currentLoyaltyProgramResult = this.GetLoyaltyCards(storefront, userId, false);
            if (!currentLoyaltyProgramResult.ServiceProviderResult.Success)
            {
                currentLoyaltyProgramResult.ServiceProviderResult.SystemMessages.ToList().ForEach(m => errorResult.SystemMessages.Add(m));
                return new ManagerResponse<JoinLoyaltyProgramResult, LoyaltyCard>(errorResult, null);
            }

            //TODO : Replace with call to Sitecore config item
            var maxLoyaltyProgramsToJoin = 10;
            if (currentLoyaltyProgramResult.Result.Count() >= maxLoyaltyProgramsToJoin)
            {
                var message = "You have reached the maximum number of loyalty programs to join {0}";
                message = string.Format(CultureInfo.InvariantCulture, message, maxLoyaltyProgramsToJoin);
                errorResult.SystemMessages.Add(new SystemMessage() { Message = message });
                return new ManagerResponse<JoinLoyaltyProgramResult, LoyaltyCard>(errorResult, null);
            }
            
            var cart = (CommerceCart)cartResult.ServiceProviderResult.Cart;
            var request = new Sitecore.Commerce.Connect.DynamicsRetail.Services.LoyaltyPrograms.JoinLoyaltyProgramRequest(userId, storefront.ShopName) { CartId = cart.ExternalId };
            result = this.LoyaltyProgramServiceProvider.JoinLoyaltyProgram(request);

            result.WriteToSitecoreLog();
            return new ManagerResponse<JoinLoyaltyProgramResult, LoyaltyCard>(result, result.LoyaltyCard);
        }

        /// <summary>
        /// Associates the loyalty card with current cart.
        /// </summary>
        /// <param name="storefront">The storefront.</param>
        /// <param name="visitorContext">The visitor context.</param>
        /// <param name="loyaltyCardNumber">The loyalty card number.</param>
        /// <returns>
        /// The manager response where the result is returned indicating the success or failure of the operation.
        /// </returns>
        public virtual ManagerResponse<UpdateLoyaltyCardIdResult, bool> AssociateLoyaltyCardWithCurrentCart([NotNull] CommerceStorefront storefront, string userId, string loyaltyCardNumber)
        {
            Assert.ArgumentNotNull(storefront, "storefront");
            Assert.ArgumentNotNullOrEmpty(loyaltyCardNumber, "loyaltyCardNumber");

            var result = new UpdateLoyaltyCardIdResult { Success = false };
            var cartResult = this.CartManager.GetCart(userId);
            if (!cartResult.ServiceProviderResult.Success || cartResult.Result == null)
            {
                result.SystemMessages.ToList().AddRange(cartResult.ServiceProviderResult.SystemMessages);
                return new ManagerResponse<UpdateLoyaltyCardIdResult, bool>(result, result.Success);
            }

            var cart = cartResult.ServiceProviderResult.Cart;

            var request = new UpdateLoyaltyCardIdRequest(userId, loyaltyCardNumber, cart.ExternalId);
            result = this.LoyaltyProgramServiceProvider.UpdateLoyaltyCardId(request);
            
            result.WriteToSitecoreLog();
            return new ManagerResponse<UpdateLoyaltyCardIdResult, bool>(result, result.Success);
        }

        /// <summary>
        /// Gets the loyalty cards.
        /// </summary>
        /// <param name="storefront">The storefront.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="getDetails">The getDetails specify if card details should be returned.</param>
        /// <returns>The manager response where the enumerable list of loyalty cards is returned in the Result.</returns>
        public virtual ManagerResponse<GetLoyaltyCardsResult, IEnumerable<LoyaltyCard>> GetLoyaltyCards([NotNull] CommerceStorefront storefront, [NotNull] string userId, bool getDetails)
        {
            Assert.ArgumentNotNull(storefront, "storefront");
            Assert.ArgumentNotNullOrEmpty(userId, "userId");

            var request = new GetLoyaltyCardsRequest(userId, storefront.ShopName);
            request.Properties.Add("GetDetails", getDetails);            

            var result = this.LoyaltyProgramServiceProvider.GetLoyaltyCards(request);
            if (result.Success && result.LoyaltyCards != null)
            {
                return new ManagerResponse<GetLoyaltyCardsResult, IEnumerable<LoyaltyCard>>(result, result.LoyaltyCards.ToList());
            }
            result.WriteToSitecoreLog();
            return new ManagerResponse<GetLoyaltyCardsResult, IEnumerable<LoyaltyCard>>(result, new List<LoyaltyCard>());
        }

        /// <summary>
        /// Gets the loyalty card transactions.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <param name="rewardPointId">The reward point identifier.</param>
        /// <param name="rowsCount">The rows count.</param>
        /// <returns>The manager response where the enumarable list of loyalty card trabsactions are returned in the Result.</returns>
        public virtual ManagerResponse<GetLoyaltyCardTransactionsResult, IEnumerable<LoyaltyCardTransaction>> GetLoyaltyCardTransactions(string cardNumber, string rewardPointId, int rowsCount)
        {
            var request = new Sitecore.Commerce.Connect.DynamicsRetail.Services.LoyaltyPrograms.GetLoyaltyCardTransactionsRequest(new LoyaltyCard { CardNumber = cardNumber, ExternalId = cardNumber }, rewardPointId) { RowsCount = rowsCount };
            var result = this.LoyaltyProgramServiceProvider.GetLoyaltyCardTransactions(request);
            if (result.Success && result.LoyaltyCardTransactions != null)
            {
                return new ManagerResponse<GetLoyaltyCardTransactionsResult, IEnumerable<LoyaltyCardTransaction>>(result, result.LoyaltyCardTransactions);
            }

            result.WriteToSitecoreLog();
            return new ManagerResponse<GetLoyaltyCardTransactionsResult, IEnumerable<LoyaltyCardTransaction>>(result, new List<LoyaltyCardTransaction>());
        }

        #endregion
    }
}