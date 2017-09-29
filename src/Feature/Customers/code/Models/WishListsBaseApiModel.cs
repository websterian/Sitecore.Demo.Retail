//-----------------------------------------------------------------------
// <copyright file="WishListsBaseApiModel.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the WishListsBaseApiModel class.</summary>
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
    using System.Collections.Generic;

    using Sitecore.Commerce.Entities.WishLists;
    using Sitecore.Commerce.Services;

    /// <summary>
    /// Json result for wish lists operations.
    /// </summary>
    public class WishListsBaseApiModel
    {
        private List<WishListHeaderItemBaseApiModel> _wishLists = new List<WishListHeaderItemBaseApiModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WishListsBaseApiModel"/> class.
        /// </summary>
        public WishListsBaseApiModel()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WishListsBaseApiModel"/> class.
        /// </summary>
        /// <param name="result">The service provider result.</param>
        public WishListsBaseApiModel(ServiceProviderResult result)
        {
        }    

        /// <summary>
        /// Gets the wish lists.
        /// </summary>
        public List<WishListHeaderItemBaseApiModel> WishLists 
        { 
            get 
            { 
                return this._wishLists; 
            } 
        }

        /// <summary>
        /// Initializes the specified wish lists.
        /// </summary>
        /// <param name="wishLists">The wish lists.</param>
        public virtual void Initialize(IEnumerable<WishListHeader> wishLists)
        {
            if (wishLists == null)
            {
                return;
            }

            foreach (var wishList in wishLists)
            {
                this._wishLists.Add(new WishListHeaderItemBaseApiModel(wishList));
            }
        }
    }
}