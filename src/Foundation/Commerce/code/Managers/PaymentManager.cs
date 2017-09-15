//-----------------------------------------------------------------------
// <copyright file="PaymentManager.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>The manager class responsible for encapsulating the payment business logic for the site.</summary>
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

using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
//using Sitecore.Commerce.Engine.Connect.Pipelines.Arguments;
using Sitecore.Commerce.Entities.Payments;
using Sitecore.Commerce.Services;
using Sitecore.Commerce.Services.Payments;
using Sitecore.Diagnostics;
using Sitecore.Foundation.Commerce.Extensions;
using Sitecore.Foundation.Commerce.Models;
using Sitecore.Foundation.Dictionary.Repositories;
using Sitecore.Commerce.Connect.DynamicsRetail.Services.Payments;
//using GetPaymentMethodsRequest = Sitecore.Commerce.Engine.Connect.Services.Payments.GetPaymentMethodsRequest;

namespace Sitecore.Foundation.Commerce.Managers
{
    public class PaymentManager : IManager
    {
        public PaymentManager(PaymentServiceProvider paymentServiceProvider, CartManager cartManager, StorefrontContext storefrontContext)
        {
            Assert.ArgumentNotNull(paymentServiceProvider, nameof(paymentServiceProvider));

            PaymentServiceProvider = paymentServiceProvider;
            CartManager = cartManager;
            StorefrontContext = storefrontContext;
        }

        public PaymentServiceProvider PaymentServiceProvider { get; protected set; }
        public CartManager CartManager { get; protected set; }
        public StorefrontContext StorefrontContext { get; }


        public ManagerResponse<GetPaymentOptionsResult, IEnumerable<PaymentOption>> GetPaymentOptions(string userId)
        {
            var result = new GetPaymentOptionsResult {Success = false};
            var cartResult = CartManager.GetCart(userId);
            if (!cartResult.ServiceProviderResult.Success || cartResult.Result == null)
            {
                result.SystemMessages.ToList().AddRange(cartResult.ServiceProviderResult.SystemMessages);
                return new ManagerResponse<GetPaymentOptionsResult, IEnumerable<PaymentOption>>(result, null);
            }

            if (StorefrontContext.Current == null)
            {
                throw new InvalidOperationException("Cannot be called without a valid storefront context.");
            }
            var request = new GetPaymentOptionsRequest(StorefrontContext.Current.ShopName, cartResult.Result);
            result = PaymentServiceProvider.GetPaymentOptions(request);
            result.WriteToSitecoreLog();

            return new ManagerResponse<GetPaymentOptionsResult, IEnumerable<PaymentOption>>(result, result.PaymentOptions.ToList());
        }

        public ManagerResponse<GetPaymentMethodsResult, IEnumerable<PaymentMethod>> GetPaymentMethods(string userId, PaymentOption paymentOption)
        {
            Assert.ArgumentNotNull(paymentOption, "paymentOption");

            var request = new GetPaymentMethodsRequest(paymentOption);
            var result = this.PaymentServiceProvider.GetPaymentMethods(request);
            result.WriteToSitecoreLog();

            return new ManagerResponse<GetPaymentMethodsResult, IEnumerable<PaymentMethod>>(result, result.PaymentMethods.ToList());
        }

        //public ManagerResponse<PaymentClientTokenResult, string> GetPaymentClientToken()
        //{
        //    var request = new ServiceProviderRequest();
        //    var result = PaymentServiceProvider.RunPipeline<ServiceProviderRequest, PaymentClientTokenResult>("commerce.payments.getClientToken", request);
        //    result.WriteToSitecoreLog();

        //    return new ManagerResponse<PaymentClientTokenResult, string>(result, result.ClientToken);
        //}

        public static string GetPaymentName(string paymentType)
        {
            return DictionaryPhraseRepository.Current.Get($"/Commerce/Payment/{paymentType}", $"[{paymentType}]");
        }

        /// <summary>
        /// Gets the payment service URL.
        /// </summary>       
        /// <returns>PaymentService Url</returns>
        public ManagerResponse<GetPaymentServiceUrlResult, string> GetPaymentServiceUrl(string userId)
        {
            var request = new GetPaymentServiceUrlRequest();
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                request.HostPageOrigin = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}://{1}",
                    HttpContext.Current.Request.Url.Scheme,
                    HttpContext.Current.Request.Url.Authority);
            }

            if (!Context.User.IsAuthenticated)
            {
                request.Properties["CartId"] = userId;
            }

            var result = this.PaymentServiceProvider.GetPaymentServiceUrl(request);

            result.WriteToSitecoreLog();

            return new ManagerResponse<GetPaymentServiceUrlResult, string>(result, result.Url ?? string.Empty);
        }


        /// <summary>
        /// Gets the payment service action result.
        /// </summary>
        /// <param name="accessCode">The access code.</param>
        /// <returns>
        /// The Payment Service action result
        /// </returns>
        public ManagerResponse<GetPaymentServiceTokenizedResult, string> GetPaymentServiceActionResult(string accessCode)
        {
            Assert.ArgumentNotNull(accessCode, "accessCode");

            var request = new GetPaymentServiceActionResultRequest
            {
                Locale = Context.Culture.Name,
                PaymentAcceptResultAccessCode = accessCode
            };

            var result = this.PaymentServiceProvider.RunPipeline<GetPaymentServiceActionResultRequest, GetPaymentServiceTokenizedResult>(Sitecore.Commerce.Pipelines.PipelineName.GetPaymentServiceActionResult, request);

            result.WriteToSitecoreLog();

            return new ManagerResponse<GetPaymentServiceTokenizedResult, string>(result, result.CardToken);
        }
    }
}