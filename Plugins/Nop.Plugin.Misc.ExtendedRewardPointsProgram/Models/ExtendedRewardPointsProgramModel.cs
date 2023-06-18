﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram.Models
{
    public record ExtendedRewardPointsProgramModel : BaseNopModel
    {
        public ExtendedRewardPointsProgramModel()
        {
            ForBlogComments = new RewardPointsModel();
            ForFirstPurchase = new RewardPointsModel();
            ForNewsComments = new RewardPointsModel();
            ForNewsletterSubscriptions = new RewardPointsModel();
            ForProductReviews = new RewardPointsModel();
            ForRegistration = new RewardPointsModel();
            ForFastPurchase = new RewardPointsModel();
        }

        public bool IsMultistore { get; set; }

        public RewardPointsModel ForBlogComments { get; set; }

        public RewardPointsModel ForFirstPurchase { get; set; }

        public RewardPointsModel ForNewsComments { get; set; }

        public RewardPointsModel ForNewsletterSubscriptions { get; set; }

        public RewardPointsModel ForProductReviews { get; set; }

        public RewardPointsModel ForRegistration { get; set; }

        public RewardPointsModel ForFastPurchase { get; set; }
    }

    public record RewardPointsModel : BaseNopModel, ILocalizedModel<LocalizedModel>, ISettingsModel
    {
        public RewardPointsModel()
        {
            Locales = new List<LocalizedModel>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.IsEnabled")]
        public bool IsEnabled { get; set; }
        public bool IsEnabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.Points")]
        public int Points { get; set; }
        public bool Points_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.ActivatePointsImmediately")]
        public bool ActivatePointsImmediately { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.ActivationDelay")]
        public int ActivationDelay { get; set; }
        public bool ActivationDelay_OverrideForStore { get; set; }
        public int ActivationDelayPeriodId { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.Message")]
        public string Message { get; set; }
        public bool Message_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase.Fields.Minutes")]
        public int? Minutes { get; set; }
        public bool Minutes_OverrideForStore { get; set; }

        public IList<LocalizedModel> Locales { get; set; }
    }

    public class LocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.Message")]
        public string Message { get; set; }
    }
}