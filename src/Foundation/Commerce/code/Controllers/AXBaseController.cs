//-----------------------------------------------------------------------
// <copyright file="AxBaseController.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the Dynamics base controller.</summary>
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

namespace Sitecore.Foundation.Commerce.Controllers
{
    using System;
    using System.Web;

    using Managers;

    using Sitecore.Commerce.Contacts;
    using Sitecore.Diagnostics;
    using Sitecore.Foundation.Commerce.Util;
    using Sitecore.Mvc.Controllers;

    /// <summary>
    /// Defines the AxBaseController class.
    /// </summary>
    public class AxBaseController : SitecoreController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxBaseController"/> class.
        /// </summary>
        /// <param name="accountManager">The account manager.</param>
        /// <param name="contactFactory">The contact factory.</param>
        public AxBaseController(AccountManager accountManager, ContactFactory contactFactory)
            : base()
        { 
            this.AccountManager = accountManager;
        }

        /// <summary>
        /// Gets or sets the account manager.
        /// </summary>
        /// <value>
        /// The account manager.
        /// </value>
        public AccountManager AccountManager { get; set; }

        /// <summary>
        /// Cleans the not authorized session.
        /// </summary>
        public virtual void CleanNotAuthorizedSession()
        {
            var ctx = this.Request.GetOwinContext();
            ctx.Authentication.SignOut(OpenIdConnectUtilities.ApplicationCookieAuthenticationType);
            OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.OpenIdCookie);

            // Clean up openId nonce cookie. This is just a workaround. Ideally, we should be calling 'ctx.Authentication.SignOut(providerClient.Name)'              
            foreach (var cookieName in this.ControllerContext.HttpContext.Request.Cookies.AllKeys)
            {
                if (cookieName.StartsWith("OpenIdConnect.nonce.", StringComparison.OrdinalIgnoreCase))
                {
                    OpenIdConnectUtilities.RemoveCookie(cookieName);
                    break;
                }
            }        
        }
    }
}