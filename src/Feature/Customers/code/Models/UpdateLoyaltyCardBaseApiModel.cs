//-----------------------------------------------------------------------
// <copyright file="UpdateLoyaltyCardBaseApiModel.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the UpdateLoyaltyCardBaseApiModel class.</summary>
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
 
namespace Sitecore.Feature.Commerce.Customers.Models
{
    using Sitecore.Commerce.Services;
    using Sitecore.Diagnostics;
    using Sitecore.Foundation.Commerce.Models;

    /// <summary>
    /// The Json result of a request to retrieve the available states.
    /// </summary>
    public class UpdateLoyaltyCardBaseApiModel : BaseApiModel

{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLoyaltyCardBaseApiModel"/> class.
    /// </summary>
    public UpdateLoyaltyCardBaseApiModel()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLoyaltyCardBaseApiModel"/> class.
    /// </summary>
    /// <param name="result">The service provider result.</param>
    public UpdateLoyaltyCardBaseApiModel(ServiceProviderResult result)
        : base(result)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the loyalty card was successfully updated.
    /// </summary>
    public bool WasUpdated { get; set; }

    /// <summary>
    /// Initializes the specified was updated.
    /// </summary>
    /// <param name="wasUpdated">if set to <c>true</c> [was updated].</param>
    public virtual void Initialize(bool wasUpdated)
    {
        Assert.ArgumentNotNull(wasUpdated, "wasUpdated");

        this.WasUpdated = wasUpdated;
    }
}
}