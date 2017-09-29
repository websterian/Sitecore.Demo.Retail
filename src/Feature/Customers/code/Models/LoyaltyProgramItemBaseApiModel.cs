//-----------------------------------------------------------------------
// <copyright file="LoyaltyProgramItemBaseApiModel.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the LoyaltyProgramItemBaseApiModel class.</summary>
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sitecore.Commerce.Entities.LoyaltyPrograms;
    using Sitecore.Commerce.Services;
    using Sitecore.Diagnostics;
    using Sitecore.Foundation.Commerce.Models;

    /// <summary>
    /// Json result for loyalty program operations.
    /// </summary>
    public class LoyaltyProgramItemBaseApiModel : BaseApiModel
    {
        /// <summary>
        /// The _tiers.
        /// </summary>
        private readonly List<LoyaltyTierItemBaseApiModel> _tiers = new List<LoyaltyTierItemBaseApiModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyProgramItemBaseApiModel"/> class.
        /// </summary>
        public LoyaltyProgramItemBaseApiModel()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyProgramItemBaseApiModel"/> class.
        /// </summary>
        /// <param name="result">The service provider result.</param>
        public LoyaltyProgramItemBaseApiModel(ServiceProviderResult result)
            : base(result)
        {
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the program identifier.
        /// </summary>
        /// <value>
        /// The program identifier.
        /// </value>
        public string ProgramId { get; set; }

        /// <summary>
        /// Gets the loyalty tiers.
        /// </summary>
        /// <value>
        /// The loyalty tiers.
        /// </value>
        public List<LoyaltyTierItemBaseApiModel> LoyaltyTiers
        {
            get
            {
                return this._tiers;
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyProgramItemBaseApiModel" /> class.
        /// </summary>
        /// <param name="program">The program.</param>
        public virtual void Initialize(LoyaltyProgramStatus program)
        {
            Assert.ArgumentNotNull(program, "program");

            this.Name = program.Name;
            this.Description = program.Description;
            this.ProgramId = program.ExternalId;

            foreach (var tier in program.LoyaltyTiers)
            {
                var cardTier = program.LoyaltyCardTiers.FirstOrDefault(ct => ct.TierId.Equals(tier.TierId, StringComparison.OrdinalIgnoreCase));
                var result = new LoyaltyTierItemBaseApiModel();
                result.Initialize(tier, cardTier);
                this._tiers.Add(result);
            }
        }
    }
}