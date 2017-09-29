﻿//-----------------------------------------------------------------------
// <copyright file="LoyaltyCardItemBaseApiModel.cs" company="Sitecore Corporation">
//     Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// <summary>Defines the LoyaltyCardItemBaseApiModel class.</summary>
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
    using Sitecore.Commerce.Entities.LoyaltyPrograms;
    using Sitecore.Commerce.Services;
    using Sitecore.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// Json result for loyalty cards operations.
    /// </summary>
    public class LoyaltyCardItemBaseApiModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyCardItemBaseApiModel"/> class.
        /// </summary>
        public LoyaltyCardItemBaseApiModel()
            : base()
        {
            this.RewardPoints = new List<LoyaltyRewardPointItemBaseApiModel>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyCardItemBaseApiModel"/> class.
        /// </summary>
        /// <param name="result">The service provider result.</param>
        public LoyaltyCardItemBaseApiModel(ServiceProviderResult result)
        {
            this.RewardPoints = new List<LoyaltyRewardPointItemBaseApiModel>();
            this.Programs = new List<LoyaltyProgramItemBaseApiModel>();
        }

        /// <summary>
        /// Gets or sets the card number.
        /// </summary>
        /// <value>
        /// The card number.
        /// </value>
        public string CardNumber { get; set; }

        /// <summary>
        /// Gets or sets the reward points.
        /// </summary>
        /// <value>
        /// The reward points.
        /// </value>
        public List<LoyaltyRewardPointItemBaseApiModel> RewardPoints { get; protected set; }

        /// <summary>
        /// Gets or sets the programs.
        /// </summary>
        /// <value>
        /// The programs.
        /// </value>
        public List<LoyaltyProgramItemBaseApiModel> Programs { get; protected set; }

        /// <summary>
        /// Initializes the specified loyalty card.
        /// </summary>
        /// <param name="loyaltyCard">The loyalty card.</param>
        public virtual void Initialize(LoyaltyCard loyaltyCard)
        {
            Assert.ArgumentNotNull(loyaltyCard, "loyaltyCard");

            this.CardNumber = loyaltyCard.CardNumber;

            foreach (var point in loyaltyCard.RewardPoints)
            {
                var result = new LoyaltyRewardPointItemBaseApiModel();
                result.Initialize(point);
                this.RewardPoints.Add(result);
            }

            foreach (var program in ((Sitecore.Commerce.Connect.DynamicsRetail.Entities.LoyaltyPrograms.LoyaltyCard)loyaltyCard).LoyaltyPrograms)
            {
                var result = new LoyaltyProgramItemBaseApiModel();
                result.Initialize(program);
                if (this.Programs == null)
                {
                    this.Programs = new List<LoyaltyProgramItemBaseApiModel>();
                }
                this.Programs.Add(result);
            }
        }
    }
}