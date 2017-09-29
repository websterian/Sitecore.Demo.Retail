//-----------------------------------------------------------------------
// <copyright file="AccountController.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the AccountController class.</summary>
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

using Microsoft.IdentityModel.Protocols;

using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Entities.Customers;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Feature.Commerce.Customers.Models;
using Sitecore.Foundation.Commerce;
using Sitecore.Foundation.Commerce.Configuration;
using Sitecore.Foundation.Commerce.Extensions;
using Sitecore.Foundation.Commerce.Managers;
using Sitecore.Foundation.Commerce.Models;
using Sitecore.Foundation.Commerce.Models.InputModels;
using Sitecore.Foundation.Commerce.Util;
using Sitecore.Foundation.SitecoreExtensions.Attributes;
using Sitecore.Links;
using Sitecore.Mvc.Controllers;

namespace Sitecore.Feature.Commerce.Customers.Controllers
{
    using Sitecore.Commerce;

    public class CustomersController : SitecoreController
    {
        public enum ManageMessageId
        {
            ChangePasswordSuccess, 
            SetPasswordSuccess, 
            RemoveLoginSuccess
        }

        public CustomersController(AccountManager accountManager, CountryManager countryManager, CommerceUserContext commerceUserContext, StorefrontContext storefrontContext)
        {
            this.AccountManager = accountManager;
            this.CommerceUserContext = commerceUserContext;
            this.StorefrontContext = storefrontContext;
            this.CountryManager = countryManager;
        }

        private CountryManager CountryManager { get; }
        private AccountManager AccountManager { get; }
        private CommerceUserContext CommerceUserContext { get; }
        public StorefrontContext StorefrontContext { get; }
        public int MaxNumberOfAddresses => Settings.GetIntSetting("Commerce.MaxNumberOfAddresses", 10);

        /// <summary>
        /// Handles a user trying to log off
        /// </summary>
        /// <returns>The view to display to the user after logging off</returns>
        [HttpGet]
        [Authorize]
        public ActionResult LogOff()
        {
            if (!Context.User.IsAuthenticated)
            {
                return this.Redirect("/login");
            }

            return this.View();
        }

        /// <summary>
        /// Federateds the signout.
        /// </summary>
        /// <returns>Current rendering view</returns>
        [HttpGet]
        [AllowAnonymous]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult FederatedSignOut()
        {
            IdentityProviderClientConfigurationElement providerClient = OpenIdConnectUtilities.GetCurrentProviderSettings();
            Uri externalLogOffUri = providerClient.LogOffUrl;

            OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.CookieCurrentProvider);
            OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.CookieCurrentProviderType);
            OpenIdConnectUtilities.CleanUpOnSignOutOrAuthFailure(this.HttpContext);

            var model = new FederatedSignOutApiModel() { LogOffUri = externalLogOffUri };
            return this.View(model);
        }

        public static void RemoveCookie(HttpContextBase context, string cookieName)
        {
            context.Request.Cookies.Remove(cookieName);
            HttpCookie expiredCookie = new HttpCookie(cookieName)
            {
                Expires = DateTime.UtcNow.AddDays(-1), 
                Value = null
            };

            context.Response.SetCookie(expiredCookie);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult LogOffAndRedirect()
        {
            if (Context.User.IsAuthenticated)
            {
                var ctx = this.Request.GetOwinContext();
                var cookies = ctx.Response.Cookies;

                ctx.Authentication.SignOut(OpenIdConnectUtilities.ApplicationCookieAuthenticationType);

                // Clean up openId nonce cookie. This is just a workaround. Ideally, we should be calling 'ctx.Authentication.SignOut(providerClient.Name)'              
                foreach (string cookieName in this.ControllerContext.HttpContext.Request.Cookies.AllKeys)
                {
                    if (cookieName.StartsWith("OpenIdConnect.nonce.", StringComparison.OrdinalIgnoreCase))
                    {
                        OpenIdConnectUtilities.RemoveCookie(cookieName);
                    }
                }
            }

            this.AccountManager.Logout();
            return this.Redirect("/federatedSignout");
        }

        /// <summary>
        /// An action to handle displaying the login form
        /// </summary>
        /// <param name="returnUrl">A location to redirect the user to</param>
        /// <param name="existingAccount">The existing account.</param>
        /// <param name="externalIdProvider">The external identifier provider.</param>
        /// <returns>
        /// The view to display to the user
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult Login(string returnUrl, string existingAccount, string externalIdProvider)
        {
            if (Context.User.IsAuthenticated)
            {
                return this.Redirect("/accountmanagement");
            }

            this.ViewBag.ReturnUrl = returnUrl;

            var model = new LoginApiModel();
            model.IsActivationFlow = !string.IsNullOrEmpty(existingAccount);

            if (model.IsActivationFlow)
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture, 
                    "Congratulations! We were able to successfully link your '{0}' account with the {1} account belonging to '{2}'. Please sign in to access your account.", 
                    Context.Site.Name, 
                    externalIdProvider, 
                    existingAccount);
                model.Message = message;
            }

            List<IdentityProviderApiModel> providers = new List<IdentityProviderApiModel>();
            IDictionary<string, IdentityProviderClientConfigurationElement> identityProviderDictionary = GetIdentityProvidersFromConfig();
            foreach (IdentityProviderClientConfigurationElement provider in identityProviderDictionary.Values)
            {
                MediaItem providerImage = Context.Database.GetItem(provider.ImageUrl.OriginalString);
                providers.Add(new IdentityProviderApiModel() { Name = provider.Name, Image = providerImage });
            }

            model.Providers.AddRange(providers);
            return this.View(model);
        }

        /// <summary>
        /// Handles a user trying to login
        /// </summary>
        /// <param name="provider">The name of the open Id provider</param>
        /// <returns>The view to display to the user</returns>     
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("Login")]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult LoginPost(string provider)
        {
            IdentityProviderClientConfigurationElement providerConfig = OpenIdConnectUtilities.GetIdentityProviderFromConfiguration(provider);
            switch (providerConfig.ProviderType)
            {
                case IdentityProviderType.OpenIdConnect:
                    this.ControllerContext.HttpContext.GetOwinContext().Authentication.Challenge(providerConfig.Name);
                    return new HttpUnauthorizedResult();

                case IdentityProviderType.ACS:

                    // Storing cookie with current provider (used in Logoff).
                    OpenIdConnectUtilities.SetCookie(
                        this.HttpContext, 
                        OpenIdConnectUtilities.CookieCurrentProvider, 
                        providerConfig.Name);
                    OpenIdConnectUtilities.SetCookie(
                        this.HttpContext, 
                        OpenIdConnectUtilities.CookieCurrentProviderType, 
                        providerConfig.ProviderType.ToString());

                    string url = string.Format(
                        CultureInfo.InvariantCulture, 
                        "{0}v2/wsfederation?wa=wsignin1.0&wtrealm={1}", 
                        providerConfig.Issuer, 
                        providerConfig.RedirectUrl);
                    this.Response.Redirect(url, true);
                    break;

                default:
                    SecurityException securityException =
                        new SecurityException(
                            string.Format(
                                CultureInfo.InvariantCulture, 
                                "The identity provider type {0} is not supported", 
                                providerConfig.ProviderType));
                    throw securityException;
            }

            return null;
        }

        /// <summary>
        /// Action invoked on being redirected from open identity provider.
        /// </summary>
        /// <returns>View after being redirected from open identity provider.</returns>
        /// <exception cref="System.NotSupportedException">Thrown when email claim does not exist.</exception>
        public ActionResult OAuthV2Redirect()
        {
            IdentityProviderClientConfigurationElement currentProvider = OpenIdConnectUtilities.GetCurrentProviderSettings();

            // Check whether provider returned an error which could be a case if a user rejected a consent.           
            string errorCode = this.HttpContext.Request.Params["error"];
            if (errorCode != null)
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture, 
                    "The provider {0} returned error code {1} while processing user's credentials.", 
                    currentProvider.Name, 
                    errorCode);
                this.Response.Redirect("~", false);
                this.HttpContext.ApplicationInstance.CompleteRequest();
                return null;
            }

            string authorizationCode = OpenIdConnectUtilities.ValidateRequestAndGetAuthorizationCode();

            if (authorizationCode == null)
            {
                string message = "Unable to find the authorization code for the login request.";
                SecurityException securityException = new SecurityException(message);
                throw securityException;
            }

            string bodyParameters = string.Format(
                CultureInfo.InvariantCulture, 
                "grant_type=authorization_code&code={0}&redirect_uri={1}&client_id={2}&client_secret={3}", 
                authorizationCode, 
                currentProvider.RedirectUrl, 
                currentProvider.ClientId, 
                currentProvider.ClientSecret);

            OpenIdConnectConfiguration providerDiscoveryDocument = OpenIdConnectUtilities.GetDiscoveryDocument(currentProvider.Issuer);

            string returnValuesJson = OpenIdConnectUtilities.HttpPost(new Uri(providerDiscoveryDocument.TokenEndpoint), bodyParameters);

            TokenEndpointResponse tokenResponse = OpenIdConnectUtilities.DeserilizeJson<TokenEndpointResponse>(returnValuesJson);

            JwtSecurityToken token = OpenIdConnectUtilities.GetIdToken(tokenResponse.IdToken);

            Claim emailClaim = token.Claims.SingleOrDefault(c => string.Equals(c.Type, OpenIdConnectUtilities.Email, StringComparison.OrdinalIgnoreCase));

            string email = null;

            // IdentityServer does not return email claim.
            if (emailClaim != null)
            {
                email = emailClaim.Value;
            }

            return this.GetRedirectionBasedOnAssociatedCustomer(tokenResponse.IdToken, currentProvider.ProviderType, email);
        }

        /// <summary>
        /// Action invoked on being redirected from ACS identity provider.
        /// </summary>
        /// <returns>View after being redirected from ACS identity provider.</returns>
        /// <exception cref="System.NotSupportedException">Thrown when email claim does not exist.</exception>        
        public ActionResult AcsRedirect()
        {
            string documentContents;
            using (Stream receiveStream = this.HttpContext.Request.InputStream)
            {
                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                documentContents = readStream.ReadToEnd();
            }

            string acsToken = OpenIdConnectUtilities.GetAcsToken(documentContents);

            JwtSecurityToken token = new JwtSecurityToken(acsToken);
            var emailClaim = token.Claims.FirstOrDefault(t => t.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

            string email = null;

            // Not all providers provide the claim, for instance, Windows Live ID does not.
            if (emailClaim != null)
            {
                email = emailClaim.Value;
            }

            return this.GetRedirectionBasedOnAssociatedCustomer(acsToken, IdentityProviderType.ACS, email);
        }

        private ActionResult GetRedirectionBasedOnAssociatedCustomer(string authToken, IdentityProviderType identityProviderType, string email)
        {
            OpenIdConnectUtilities.SetTokenCookie(authToken);

            var customerResult = this.AccountManager.GetCustomer().ServiceProviderResult;
            CommerceCustomer customer = customerResult.CommerceCustomer;
            if (customerResult.Success && customer != null)
            {
                if (identityProviderType == IdentityProviderType.OpenIdConnect)
                {
                    OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.CookieState);
                    OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.CookieNonce);
                }

                return this.RegisterExistingUser(customer);
            }
            else
            {
                string url = string.Format(CultureInfo.InvariantCulture, "/Register?isActivationPending={0}&email={1}", customerResult.Properties["IsRequestToLinkToExistingCustomerPending"], email);
                return this.Redirect(url);
            }
        }

        /// <summary>
        /// Registers the specified user.
        /// </summary>
        /// <param name="commerceUser">The commerce user.</param>
        /// <returns>
        /// Redirects to the Home page after registration
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult RegisterExistingUser(CommerceCustomer commerceUser)
        {
            try
            {
                Assert.ArgumentNotNull(commerceUser, "commerceUser");
                RegisterBaseResultApiModel result = new RegisterBaseResultApiModel();

                var userResponse = this.AccountManager.GetUser(commerceUser.Name);
                if (userResponse.Result == null)
                {
                    // create the user in Sitecore
                    var inputModel = new RegisterUserInputModel { UserName = commerceUser.Name, Password = System.Web.Security.Membership.GeneratePassword(8, 4) };
                    inputModel.FirstName = commerceUser.Properties["FirstName"] as string ?? string.Empty;
                    inputModel.LastName = commerceUser.Properties["LastName"] as string ?? string.Empty;
                    var response = this.AccountManager.RegisterUser(inputModel);
                    if (!response.ServiceProviderResult.Success || response.Result == null)
                    {
                        result.SetErrors(response.ServiceProviderResult);
                        return this.Json(result, JsonRequestBehavior.AllowGet);
                    }
                }

                var isLoggedIn = this.AccountManager.Login(commerceUser.Name, false);
                if (isLoggedIn)
                {
                    return this.Redirect("/");
                }
                else
                {
                    result.SetErrors(new List<string> { "Could not create user" });
                }

                return this.Json(result);
            }
            catch (Sitecore.Commerce.OpenIDConnectionClosedUnexpectedlyException)
            {
                this.CleanNotAuthorizedSession();
                return this.Redirect("/login");
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("Register", e), JsonRequestBehavior.AllowGet);
            }
        }

        public virtual void CleanNotAuthorizedSession()
        {
            var ctx = this.Request.GetOwinContext();
            ctx.Authentication.SignOut(OpenIdConnectUtilities.ApplicationCookieAuthenticationType);
            OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.OpenIdCookie);

            // Clean up openId nonce cookie. This is just a workaround. Ideally, we should be calling 'ctx.Authentication.SignOut(providerClient.Name)'              
            foreach (string cookieName in this.ControllerContext.HttpContext.Request.Cookies.AllKeys)
            {
                if (cookieName.StartsWith("OpenIdConnect.nonce.", StringComparison.OrdinalIgnoreCase))
                {
                    OpenIdConnectUtilities.RemoveCookie(cookieName);
                    break;
                }
            }
        }

        private static IDictionary<string, IdentityProviderClientConfigurationElement> GetIdentityProvidersFromConfig()
        {
            IDictionary<string, IdentityProviderClientConfigurationElement> identityProvierLookUp = new Dictionary<string, IdentityProviderClientConfigurationElement>();

            RetailConfiguration retailConfiguration = (RetailConfiguration) OpenIdConnectUtilities.DynamicsConnectorConfiguration.GetSection(OpenIdConnectUtilities.ConfigurationSectionName);
            foreach (IdentityProviderClientConfigurationElement provider in retailConfiguration.IdentityProviders)
            {
                identityProvierLookUp.Add(provider.Name, provider);
            }

            return identityProvierLookUp;
        }

        [HttpGet]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult Addresses()
        {
            if (!Context.User.IsAuthenticated)
            {
                return this.Redirect("/login");
            }

            return this.View();
        }

        [HttpGet]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult EditProfile()
        {
            var model = new ProfileModel();

            if (!Context.User.IsAuthenticated)
            {
                return this.Redirect("/login");
            }

            var user = this.CommerceUserContext.Current;
            if (user == null)
            {
                return this.View(model);
            }

            model.FirstName = user.FirstName;
            model.Email = user.Email;
            model.EmailRepeat = user.Email;
            model.LastName = user.LastName;
            model.TelephoneNumber = user.Phone as string;

            return this.View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult ForgotPassword()
        {
            return this.View();
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult ForgotPasswordConfirmation(string userName)
        {
            this.ViewBag.UserName = userName;

            return this.View();
        }

        [HttpGet]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult ChangePassword()
        {
            if (!Context.User.IsAuthenticated)
            {
                return this.Redirect("/login");
            }

            return this.View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public ActionResult Register(RegisterUserInputModel inputModel)
        {
            RegisterBaseResultApiModel result = new RegisterBaseResultApiModel();
            try
            {
                Assert.ArgumentNotNull(inputModel, "RegisterInputModel");

                if (string.Equals(inputModel.SignupSelection, "NewAccount", StringComparison.OrdinalIgnoreCase))
                {
                    inputModel.Password = System.Web.Security.Membership.GeneratePassword(8, 4);
                    var response = this.AccountManager.RegisterUser(inputModel);
                    if (response.ServiceProviderResult.Success && response.Result != null)
                    {
                        var isLoggedIn = this.AccountManager.Login(response.Result.UserName, false);
                        if (!isLoggedIn)
                        {
                            result.Success = false;
                            result.SetErrors(new List<string> { "Could not create user" });
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.SetErrors(response.ServiceProviderResult);
                    }

                    return this.Json(result, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string emailOfExistingCustomer = inputModel.LinkupEmail;

                    var response = this.AccountManager.InitiateLinkToExistingCustomer(emailOfExistingCustomer);
                    if (response.ServiceProviderResult.Success && response.Result != null)
                    {
                        ////Clean up auth cookies completely. We need to be signed out.
                        OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.CookieCurrentProvider);
                        OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.CookieCurrentProviderType);
                        OpenIdConnectUtilities.RemoveCookie(OpenIdConnectUtilities.OpenIdCookie);

                        result.UserName = response.Result.Name;
                        result.IsSignupFlow = true;
                        return this.Json(result, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        result.Success = false;
                        result.SetErrors(response.ServiceProviderResult);
                        return this.Json(result, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (AggregateException ex)
            {
                result.Success = false;
                result.SetErrors(StorefrontConstants.KnownActionNames.RegisterActionName, ex.InnerExceptions[0]);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Sitecore.Commerce.OpenIDConnectionClosedUnexpectedlyException)
            {
                this.CleanNotAuthorizedSession();
                return this.Redirect("/login");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.SetErrors(StorefrontConstants.KnownActionNames.RegisterActionName, ex);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Starts the sign up process.
        /// </summary>
        /// <param name="email">The email address from external identifier.</param>
        /// <param name="isActivationPending">if set to <c>true</c> [is activation pending].</param>
        /// <returns>
        /// The view for entering sign up information.
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register(string email, bool? isActivationPending)
        {
            RegisterViewModel registerViewModel = new RegisterViewModel()
            {
                UserName = email
            };

            if (isActivationPending == true)
            {
                registerViewModel.Errors.Add("A previous request to link this user to an exiting account is already pending. Any new actions will override the previous request.");
            }

            return this.View(registerViewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult ChangePassword(ChangePasswordInputModel inputModel)
        {
            try
            {
                Assert.ArgumentNotNull(inputModel, nameof(inputModel));
                var result = this.CreateJsonResult<ChangePasswordApiModel>();
                if (result.HasErrors)
                {
                    return this.Json(result, JsonRequestBehavior.AllowGet);
                }

                var response = this.AccountManager.UpdateUserPassword(this.CommerceUserContext.Current.UserName, inputModel);
                result = new ChangePasswordApiModel(response.ServiceProviderResult);
                if (response.ServiceProviderResult.Success)
                {
                    result.Initialize(this.CommerceUserContext.Current.UserName);
                }

                return this.Json(result);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("ChangePassword", e), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SkipAnalyticsTracking]
        public ActionResult AccountHomeProfile()
        {
            if (!Context.User.IsAuthenticated)
            {
                return this.Redirect("/login");
            }

            var model = new ProfileModel();
            var user = this.CommerceUserContext.Current;
            if (user != null)
            {
                model.FirstName = user.FirstName;
                model.Email = user.Email;
                model.LastName = user.LastName;
                model.TelephoneNumber = user.Phone;
            }

            var item = Context.Item.Children.SingleOrDefault(p => p.Name == "EditProfile");

            if (item != null)
            {
                // If there is a specially EditProfile then use it
                this.ViewBag.EditProfileLink = LinkManager.GetDynamicUrl(item);
            }
            else
            {
                // Else go global Edit Profile
                item = Context.Item.Database.GetItem("/sitecore/content/Home/MyAccount/Profile");
                this.ViewBag.EditProfileLink = LinkManager.GetDynamicUrl(item);
            }

            return this.View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult AddressList()
        {
            var result = new AddressListItemApiModel();
            try
            {
                
                var addresses = this.AllAddresses(result);
                var countries = this.GetAvailableCountries(result);
                result.Initialize(addresses, countries);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Sitecore.Commerce.OpenIDConnectionClosedUnexpectedlyException)
            {
                this.CleanNotAuthorizedSession();
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("AddressList", e), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult AddressDelete(DeleteAddressInputModelItem model)
        {
            try
            {
                Assert.ArgumentNotNull(model, nameof(model));

                var validationResult = this.CreateJsonResult();
                if (validationResult.HasErrors)
                {
                    return this.Json(validationResult, JsonRequestBehavior.AllowGet);
                }

                var addresses = new List<IParty>();
                var response = this.AccountManager.RemovePartiesFromUser(Context.User.Name, model.ExternalId);
                var result = new AddressListItemApiModel(response.ServiceProviderResult);
                if (response.ServiceProviderResult.Success)
                {
                    addresses = this.AllAddresses(result);
                }

                result.Initialize(addresses, null);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("AddressDelete", e), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult AddressModify(PartyInputModelItem model)
        {
            try
            {
                Assert.ArgumentNotNull(model, nameof(model));

                var validationResult = this.CreateJsonResult();
                if (validationResult.HasErrors)
                {
                    return this.Json(validationResult, JsonRequestBehavior.AllowGet);
                }

                var addresses = new List<IParty>();
                var user = this.CommerceUserContext.Current;
                var result = new AddressListItemApiModel();
                
                var customer = new CommerceCustomer { ExternalId = user.UserId };
                var party = new Party
                {
                    ExternalId = model.ExternalId, 
                    FirstName = model.Name, 
                    LastName = string.Empty, 
                    Address1 = model.Address1, 
                    City = model.City, 
                    Country = model.Country, 
                    State = model.State, 
                    ZipPostalCode = model.ZipPostalCode, 
                    PartyId = model.PartyId, 
                    IsPrimary = model.IsPrimary
                };

                if (string.IsNullOrEmpty(party.ExternalId))
                {
                    // Verify we have not reached the maximum number of addresses supported.
                    int numberOfAddresses = this.AllAddresses(result).Count;
                    if (numberOfAddresses >= this.MaxNumberOfAddresses)
                    {
                        var message = "Address limit reached";
                        result.Errors.Add(string.Format(CultureInfo.InvariantCulture, message, numberOfAddresses));
                        result.Success = false;
                    }
                    else
                    {
                        var response = this.AccountManager.AddParties(user.UserName, new List<IParty> { model });
                        result.SetErrors(response.ServiceProviderResult);
                        if (response.ServiceProviderResult.Success)
                        {
                            addresses = this.AllAddresses(result);
                        }

                        result.Initialize(addresses, null);
                    }
                }
                else
                {
                    var response = this.AccountManager.UpdateParties(user.UserName, new List<IParty> { model });
                    result.SetErrors(response.ServiceProviderResult);
                    if (response.ServiceProviderResult.Success)
                    {
                        addresses = this.AllAddresses(result);
                    }

                    result.Initialize(addresses, null);
                }

                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (OpenIDConnectionClosedUnexpectedlyException e)
            {
                this.CleanNotAuthorizedSession();
                return this.Json(new ErrorApiModel("Login", e), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("AddressModify", e), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult UpdateProfile(ProfileModel model)
        {
            try
            {
                Assert.ArgumentNotNull(model, nameof(model));
                var result = this.CreateJsonResult<ProfileApiModel>();
                if (result.HasErrors)
                {
                    return this.Json(result, JsonRequestBehavior.AllowGet);
                }

                if (this.CommerceUserContext.Current == null)
                {
                    return this.Json(result);
                }

                var response = this.AccountManager.UpdateUser(this.CommerceUserContext.Current.UserName, model);
                result.SetErrors(response.ServiceProviderResult);
                if (response.ServiceProviderResult.Success && !string.IsNullOrWhiteSpace(model.Password) && !string.IsNullOrWhiteSpace(model.PasswordRepeat))
                {
                    var changePasswordModel = new ChangePasswordInputModel {NewPassword = model.Password, ConfirmPassword = model.PasswordRepeat};
                    var passwordChangeResponse = this.AccountManager.UpdateUserPassword(this.CommerceUserContext.Current.UserName, changePasswordModel);
                    result.SetErrors(passwordChangeResponse.ServiceProviderResult);
                    if (passwordChangeResponse.ServiceProviderResult.Success)
                    {
                        result.Initialize(response.ServiceProviderResult);
                    }
                }

                return this.Json(result);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("UpdateProfile", e), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateJsonAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult GetCurrentUser()
        {
            try
            {
                if (this.CommerceUserContext.Current == null)
                {
                    var anonymousResult = new UserApiModel();
                    return this.Json(anonymousResult, JsonRequestBehavior.AllowGet);
                }

                var result = new UserApiModel();
                result.Initialize(this.CommerceUserContext.Current);

                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("GetCurrentUser", e), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        [SkipAnalyticsTracking]
        public JsonResult ForgotPassword(ForgotPasswordInputModel model)
        {
            try
            {
                Assert.ArgumentNotNull(model, nameof(model));
                var result = this.CreateJsonResult<ForgotPasswordApiModel>();
                if (result.HasErrors)
                {
                    return this.Json(result, JsonRequestBehavior.AllowGet);
                }

                var resetResponse = this.AccountManager.ResetUserPassword(model);
                if (!resetResponse.ServiceProviderResult.Success)
                {
                    return this.Json(new ForgotPasswordApiModel(resetResponse.ServiceProviderResult));
                }

                result.Initialize(model.Email);
                return this.Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return this.Json(new ErrorApiModel("ForgotPassword", e), JsonRequestBehavior.AllowGet);
            }
        }

        public string UpdateUserName(string userName)
        {

            var defaultDomain = this.AccountManager.GetCommerceUsersDomain();
            return !userName.StartsWith(defaultDomain, StringComparison.OrdinalIgnoreCase) ? string.Concat(defaultDomain, @"\", userName) : userName;
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (this.Url.IsLocalUrl(returnUrl))
            {
                return this.Redirect(returnUrl);
            }

            return this.Redirect("/");
        }

        private Dictionary<string, string> GetAvailableCountries(AddressListItemApiModel result)
        {
            var countries = new Dictionary<string, string>();
            var response = this.CountryManager.GetAvailableCountries();
            if (response.ServiceProviderResult.Success && response.Result != null)
            {
                countries = response.Result;
            }

            result.SetErrors(response.ServiceProviderResult);
            return countries;
        }

        private List<IParty> AllAddresses(AddressListItemApiModel result)
        {
            var addresses = new List<IParty>();
            var response = this.AccountManager.GetCustomerParties(this.CommerceUserContext.Current.UserName);
            if (response.ServiceProviderResult.Success && response.Result != null)
            {
                addresses = response.Result.ToList();
            }

            result.SetErrors(response.ServiceProviderResult);
            return addresses;
        }
    }
}