using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Plugin.Misc.ExtendedRewardPointsProgram.Services;
using Nop.Services.Blogs;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.News;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram
{
    /// <summary>
    /// Represents extended reward points program processor
    /// </summary>
    public class ExtendedRewardPointsProgramProcessor : BasePlugin, IMiscPlugin
    {
        #region Constants

        private const string EXTENDED_REWARD_POINTS_PROGRAM_TASK_TYPE = "Nop.Plugin.Misc.ExtendedRewardPointsProgram.Services.ExtendedRewardPointsProgramTask, Nop.Plugin.Misc.ExtendedRewardPointsProgram";

        #endregion

        #region Fields

        private readonly IBlogService _blogService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILocalizationService _localizationService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly INewsService _newsService;
        private readonly IProductService _productService;
        private readonly IRewardPointsOnDateSettingsService _rewardPointsOnDateSettingsService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        
        #endregion

        #region Ctor

        public ExtendedRewardPointsProgramProcessor(IBlogService blogService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            ILocalizationService localizationService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            INewsService newsService,
            IProductService productService,
            IRewardPointsOnDateSettingsService rewardPointsOnDateSettingsService,
            IScheduleTaskService scheduleTaskService,
            ISettingService settingService,
            IWebHelper webHelper)
        {
            this._blogService = blogService;
            this._customerService = customerService;
            this._genericAttributeService = genericAttributeService;
            this._languageService = languageService;
            this._localizedEntityService = localizedEntityService;
            this._localizationService = localizationService;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._newsService = newsService;
            this._productService = productService;
            this._rewardPointsOnDateSettingsService = rewardPointsOnDateSettingsService;
            this._scheduleTaskService = scheduleTaskService;
            this._settingService = settingService;
            this._webHelper = webHelper;
        }

        #endregion
        
        #region Methods
        
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ExtendedRewardPointsProgram/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async System.Threading.Tasks.Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new RewardPointsForBlogCommentsSettings
            {
                Message = "Earned promotion for the comment to blog post {0}"
            });

            await _settingService.SaveSettingAsync(new RewardPointsForFastPurchaseSettings
            {
                Message = "Earned promotion for the fast purchase",
                Minutes = 15
            });

            await _settingService.SaveSettingAsync(new RewardPointsForFirstPurchaseSettings
            {
                Message = "Earned promotion for the first purchase"
            });

            await _settingService.SaveSettingAsync(new RewardPointsForNewsCommentsSettings
            {
                Message = "Earned promotion for the comment to news {0}"
            });

            await _settingService.SaveSettingAsync(new RewardPointsForNewsletterSubscriptionsSettings
            {
                Message = "Earned promotion for the newsletter subscription"
            });

            await _settingService.SaveSettingAsync(new RewardPointsForProductReviewsSettings
            {
                Message = "Earned promotion for the review to product {0}"
            });

            await _settingService.SaveSettingAsync(new RewardPointsForRegistrationSettings
            {
                Message = "Earned promotion for the registration"
            });

            //task for awarding on specific dates
            if (await _scheduleTaskService.GetTaskByTypeAsync(EXTENDED_REWARD_POINTS_PROGRAM_TASK_TYPE) == null)
            {
                await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
                {
                    Enabled = true,
                    Name = "Extended reward points program task",
                    Seconds = 3600,
                    Type = EXTENDED_REWARD_POINTS_PROGRAM_TASK_TYPE
                });
            }

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Misc.ExtendedRewardPointsProgram"] = "Extended reward points settings",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.ActivatePointsImmediately"] = "Activate points immediately",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.ActivatePointsImmediately.Hint"] = "Activates bonus points immediately after their calculation",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.ActivationDelay"] = "Reward points activation",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.ActivationDelay.Hint"] = "Specify how many days (hours) must elapse before earned points become active.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.IsEnabled"] = "Enabled",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.IsEnabled.Hint"] = "Check to enable reward points program.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.Message"] = "Message",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.Message.Hint"] = "Enter the message for reward points history.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.Points"] = "Points",
                ["Plugins.Misc.ExtendedRewardPointsProgram.Fields.Points.Hint"] = "Specify number of awarded points",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForBlogComments"] = "Reward points for blog post comments",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForBlogComments.Hint"] = "Points are awarded after a customer left a comment to a certain blog post and it has been approved by store owner.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase"] = "Reward points for the fast purchase",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase.Fields.Minutes"] = "Minutes",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase.Fields.Minutes.Hint"] = "Specify the time interval in minutes during which the user must complete a purchase.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase.Hint"] = "Points are awarded after an order get paid in a certain period after a customer add product to cart.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForFirstPurchase"] = "Reward points for the first purchase",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForFirstPurchase.Hint"] = "Points are awarded after a customer made a first purchase in online store.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForNewsComments"] = "Reward points for news comments",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForNewsComments.Hint"] = "Points are awarded after a customer left a comment to a certain news item and it has been approved by store owner.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForNewsletterSubscriptions"] = "Reward points for newsletter subscriptions",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForNewsletterSubscriptions.Hint"] = "Points are awarded after a customer subscribed to a newsletter.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForProductReviews"] = "Reward points for product reviews",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForProductReviews.Hint"] = "Points are awarded after a customer left a review to a product and it has been approved by store owner.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForRegistration"] = "Reward points for the registration",
                ["Plugins.Misc.ExtendedRewardPointsProgram.ForRegistration.Hint"] = "Points are awarded after a customer passed a registration.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate"] = "Reward points on specific date",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.AddNew"] = "Add new reward points on specific date settings",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Edit"] = "Edit reward points on specific date settings",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.AwardingDate"] = "Date of awarding",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.AwardingDate.Hint"] = "Specify date and time of awarding in UTC",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.CustomerRole"] = "Customer role",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.CustomerRole.Hint"] = "Select customer role for which the settings will be available.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.Store"] = "Store",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.Store.Hint"] = "Option to limit this settings to a certain store.",
                ["Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Hint"] = "It’s possible to award points on specific dates, for examples holidays. Customers will receive a message notification about that. That functionality can be limited by customer roles and stores."

            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async System.Threading.Tasks.Task UninstallAsync()
        {
            //generic attributes
            var onDateSettings = (await _rewardPointsOnDateSettingsService.GetAllRewardPointsOnDateSettingsAsync()).ToList();
            onDateSettings.ForEach(setting => _genericAttributeService.SaveAttributeAsync(setting, "CustomersAwardedOnDate", string.Empty));

            (await _blogService.GetAllCommentsAsync()).ToList().ForEach(comment =>
                _genericAttributeService.SaveAttributeAsync(comment, "CustomerAwardedForBlogComment", string.Empty, comment.StoreId));

            (await _newsService.GetAllCommentsAsync()).ToList().ForEach(comment =>
                _genericAttributeService.SaveAttributeAsync(comment, "CustomerAwardedForNewsComment", string.Empty, comment.StoreId));

            (await _productService.GetAllProductReviewsAsync(0, null)).ToList().ForEach(review =>
                _genericAttributeService.SaveAttributeAsync(review, "CustomerAwardedForProductReview", string.Empty, review.StoreId));

            (await _newsLetterSubscriptionService.GetAllNewsLetterSubscriptionsAsync()).ToList().ForEach(subscription =>
                _genericAttributeService.SaveAttributeAsync(subscription, "CustomerAwardedForSubscription", string.Empty, subscription.StoreId));

            (await _customerService.GetAllCustomersAsync()).ToList().ForEach(async customer =>
                await _genericAttributeService.DeleteAttributesAsync(
                    (await _genericAttributeService.GetAttributesForEntityAsync(customer.Id, "Customer"))
                    .Where(attribute => attribute.Key.Equals("PurchaseStartTime")).ToList()));

            //localized properties
            var localizedSettings = new[]
            {
                await _settingService.GetSettingAsync("RewardPointsForBlogCommentsSettings.Message"),
                await _settingService.GetSettingAsync("RewardPointsForFastPurchaseSettings.Message"),
                await _settingService.GetSettingAsync("RewardPointsForFirstPurchaseSettings.Message"),
                await _settingService.GetSettingAsync("RewardPointsForNewsCommentsSettings.Message"),
                await _settingService.GetSettingAsync("RewardPointsForNewsletterSubscriptionsSettings.Message"),
                await _settingService.GetSettingAsync("RewardPointsForProductReviewsSettings.Message")
            }.Where(setting => setting != null).ToList();

            foreach (var language in await _languageService.GetAllLanguagesAsync(true))
            {
                localizedSettings.ForEach(setting => _localizedEntityService.SaveLocalizedValueAsync(setting, x => x.Value, string.Empty, language.Id));
                onDateSettings.ForEach(setting => _localizedEntityService.SaveLocalizedValueAsync(setting, x => x.Message, string.Empty, language.Id));
            }

            //settings
            await _settingService.DeleteSettingAsync<RewardPointsForBlogCommentsSettings>();
            await _settingService.DeleteSettingAsync<RewardPointsForFastPurchaseSettings>();
            await _settingService.DeleteSettingAsync<RewardPointsForFirstPurchaseSettings>();
            await _settingService.DeleteSettingAsync<RewardPointsForNewsCommentsSettings>();
            await _settingService.DeleteSettingAsync<RewardPointsForNewsletterSubscriptionsSettings>();
            await _settingService.DeleteSettingAsync<RewardPointsForProductReviewsSettings>();
            await _settingService.DeleteSettingAsync<RewardPointsForRegistrationSettings>();

            //scheduled task
            var task = await _scheduleTaskService.GetTaskByTypeAsync(EXTENDED_REWARD_POINTS_PROGRAM_TASK_TYPE);
            if (task != null)
                await _scheduleTaskService.DeleteTaskAsync(task);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.ExtendedRewardPointsProgram");

            await base.UninstallAsync();
        }

        #endregion
    }
}
