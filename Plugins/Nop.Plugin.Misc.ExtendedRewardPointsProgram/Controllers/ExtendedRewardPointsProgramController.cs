using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Misc.ExtendedRewardPointsProgram.Domain;
using Nop.Plugin.Misc.ExtendedRewardPointsProgram.Models;
using Nop.Plugin.Misc.ExtendedRewardPointsProgram.Services;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram.Controllers
{
    public class ExtendedRewardPointsProgramController : BasePluginController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly IRewardPointsOnDateSettingsService _rewardPointsOnDateSettingsService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        public ExtendedRewardPointsProgramController(ICustomerService customerService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService,
            ISettingService settingService,
            IStoreService storeService,
            IStoreContext storeContext,
            IRewardPointsOnDateSettingsService rewardPointsOnDateSettingsService,
            INotificationService notificationService,
            IPermissionService permissionService)
        {
            this._customerService = customerService;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._localizedEntityService = localizedEntityService;
            this._settingService = settingService;
            this._storeService = storeService;
            this._storeContext = storeContext;
            this._rewardPointsOnDateSettingsService = rewardPointsOnDateSettingsService;
            _notificationService = notificationService;
            this._permissionService = permissionService;
        }

        #endregion

        #region Utilities

        protected async Task<RewardPointsModel> PrepareModelAsync(RewardPointsSettings settings, int storeScope, string title, string description)
        {
            //common settings
            var model = settings.ToSettingsModel<RewardPointsModel>();
              
            //some of specific settings
            model.ActiveStoreScopeConfiguration = storeScope;
            model.ActivatePointsImmediately = settings.ActivationDelay <= 0;
            model.Title = title;
            model.Description = description;

            //localization. no multi-store support for localization yet
             await AddLocalesAsync(_languageService, model.Locales, async (locale, languageId) =>
                locale.Message = (await settings.GetLocalizedSettingAsync(x => x.Message, languageId, 0, false, false)));

            //overridable per store settings
            if (storeScope > 0)
            {
                model.IsEnabled_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.IsEnabled, storeScope);
                model.Points_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Points, storeScope);
                model.ActivationDelay_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ActivationDelay, storeScope);
                model.Message_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Message, storeScope);
                model.Minutes_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Minutes, storeScope);
            }

            return model;
        }

        [NonAction]
        protected async Task SaveSettingsAsync(RewardPointsSettings settings, RewardPointsModel model, int storeScope)
        {
            //common settings
            settings = model.ToSettings(settings);

            //some of specific settings
            if (model.ActivatePointsImmediately)
                settings.ActivationDelay = 0;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.IsEnabled, model.IsEnabled_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.Points, model.Points_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.ActivationDelay, model.ActivationDelay_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.ActivationDelayPeriodId, model.ActivationDelay_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.Message, model.Message_OverrideForStore, storeScope, false);
            
            //localization. no multi-store support for localization yet
            foreach (var localized in model.Locales)
            {
                await settings.SaveLocalizedSettingAsync(x => x.Message, localized.LanguageId, localized.Message);
            }
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            var model = new ExtendedRewardPointsProgramModel();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();

            //prepare models
            model.ForBlogComments = await PrepareModelAsync(await _settingService.LoadSettingAsync<RewardPointsForBlogCommentsSettings>(storeId), storeId,
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForBlogComments"),
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForBlogComments.Hint"));

            model.ForFirstPurchase = await PrepareModelAsync(await _settingService.LoadSettingAsync<RewardPointsForFirstPurchaseSettings>(storeId), storeId,
               await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForFirstPurchase"),
               await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForFirstPurchase.Hint"));

            model.ForNewsComments = await PrepareModelAsync(await _settingService.LoadSettingAsync<RewardPointsForNewsCommentsSettings>(storeId), storeId,
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForNewsComments"),
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForNewsComments.Hint"));

            model.ForNewsletterSubscriptions = await PrepareModelAsync(await _settingService.LoadSettingAsync<RewardPointsForNewsletterSubscriptionsSettings>(storeId), storeId,
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForNewsletterSubscriptions"),
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForNewsletterSubscriptions.Hint"));

            model.ForProductReviews = await PrepareModelAsync(await _settingService.LoadSettingAsync<RewardPointsForProductReviewsSettings>(storeId), storeId,
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForProductReviews"),
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForProductReviews.Hint"));

            model.ForRegistration = await PrepareModelAsync(await _settingService.LoadSettingAsync<RewardPointsForRegistrationSettings>(storeId), storeId,
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForRegistration"),
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForRegistration.Hint"));

            var settings = await _settingService.LoadSettingAsync<RewardPointsForFastPurchaseSettings>(storeId);
            model.ForFastPurchase = await PrepareModelAsync(settings, storeId,
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase"),
                await _localizationService.GetResourceAsync("Plugins.Misc.ExtendedRewardPointsProgram.ForFastPurchase.Hint"));

            model.ForFastPurchase.Minutes = settings.Minutes;
            model.ForFastPurchase.Minutes_OverrideForStore = storeId > 0 && await _settingService.SettingExistsAsync(settings, x => x.Minutes, storeId);

            return View("~/Plugins/Misc.ExtendedRewardPointsProgram/Views/Configure.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public async Task<IActionResult> Configure(ExtendedRewardPointsProgramModel model)
        {
            if (!ModelState.IsValid)
                return await Configure();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();

            //save settings
            await SaveSettingsAsync(await _settingService.LoadSettingAsync<RewardPointsForBlogCommentsSettings>(storeId), model.ForBlogComments, storeId);
            await SaveSettingsAsync(await _settingService.LoadSettingAsync<RewardPointsForFirstPurchaseSettings>(storeId), model.ForFirstPurchase, storeId);
            await SaveSettingsAsync(await _settingService.LoadSettingAsync<RewardPointsForNewsCommentsSettings>(storeId), model.ForNewsComments, storeId);
            await SaveSettingsAsync(await _settingService.LoadSettingAsync<RewardPointsForNewsletterSubscriptionsSettings>(storeId), model.ForNewsletterSubscriptions, storeId);
            await SaveSettingsAsync(await _settingService.LoadSettingAsync<RewardPointsForProductReviewsSettings>(storeId), model.ForProductReviews, storeId);
            await SaveSettingsAsync(await _settingService.LoadSettingAsync<RewardPointsForRegistrationSettings>(storeId), model.ForRegistration, storeId);

            var rewardPointsForFastPurchaseSettings = await _settingService.LoadSettingAsync<RewardPointsForFastPurchaseSettings>(storeId);
            await SaveSettingsAsync(rewardPointsForFastPurchaseSettings, model.ForFastPurchase, storeId);
            rewardPointsForFastPurchaseSettings.Minutes = model.ForFastPurchase.Minutes;
            await _settingService.SaveSettingOverridablePerStoreAsync(rewardPointsForFastPurchaseSettings, x => x.Minutes, model.ForFastPurchase.Minutes_OverrideForStore, storeId, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #region Reward points on specific dates

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RewardPointsOnDateList(RewardPointsOnDateSearchModel searchModel)
        {
            var allSettings = await _rewardPointsOnDateSettingsService.GetAllRewardPointsOnDateSettingsAsync(pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);
            var model = allSettings.Select(async settings =>
            {
                //prepare common settings
                var model = settings.ToSettingsModel<RewardPointsOnDateModel>();

                //some of specific settings
                if (model.StoreId > 0)
                {
                    var store = await _storeService.GetStoreByIdAsync(model.StoreId);
                    model.Store = store != null ? store.Name : "Deleted";
                }
                else
                    model.Store = await _localizationService.GetResourceAsync("Admin.Common.All");

                if (model.CustomerRoleId > 0)
                {
                    var role = await _customerService.GetCustomerRoleByIdAsync(model.CustomerRoleId);
                    model.CustomerRole = role != null ? role.Name : "Deleted";
                }
                else
                    model.CustomerRole = await _localizationService.GetResourceAsync("Admin.Common.All");

                return model;
            }).ToList();

            return Json(model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RewardPointsOnDateCreateOrUpdate(int pointsId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var model = new RewardPointsOnDateModel();
            var settings = await _rewardPointsOnDateSettingsService.GetRewardPointsOnDateSettingsByIdAsync(pointsId);

            if (settings != null)
            {
                model = settings.ToSettingsModel<RewardPointsOnDateModel>();

                //localization
                await AddLocalesAsync(_languageService, model.Locales, async (locale, languageId) =>
                {
                    locale.Message = await _localizationService.GetLocalizedAsync(settings, x => x.Message, languageId, false, false);
                });
            }
            else
            {
                await AddLocalesAsync(_languageService, model.Locales);
                model.AwardingDateUtc = DateTime.UtcNow;
            }

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "0" });
            foreach (var store in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });

            //customer roles
            model.AvailableCustomerRoles.Add(new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "0" });
            foreach (var role in await _customerService.GetAllCustomerRolesAsync(true))
                model.AvailableCustomerRoles.Add(new SelectListItem { Text = role.Name, Value = role.Id.ToString() });

            return View("~/Plugins/Misc.ExtendedRewardPointsProgram/Views/CreateOrUpdateRewardPointsOnDateSettings.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RewardPointsOnDateCreateOrUpdate(string btnId, RewardPointsOnDateModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var settings = await _rewardPointsOnDateSettingsService.GetRewardPointsOnDateSettingsByIdAsync(model.Id);
            if (settings != null)
            {
                //update existing settings
                settings = model.ToSettings(settings);
                await _rewardPointsOnDateSettingsService.UpdateRewardPointsOnDateSettingsAsync(settings);
            }
            else
            {
                //create new settings
                //settings = model.MapTo(new RewardPointsOnDateSettings());
                settings = model.ToSettings(new RewardPointsOnDateSettings());
                await _rewardPointsOnDateSettingsService.InsertRewardPointsOnDateSettingsAsync(settings);
            }

            //localization
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.SaveLocalizedValueAsync(settings, x => x.Message, localized.Message, localized.LanguageId);
            }

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;

            return View("~/Plugins/Misc.ExtendedRewardPointsProgram/Views/CreateOrUpdateRewardPointsOnDateSettings.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RewardPointsOnDateDelete(int id)
        {
            var settings = await _rewardPointsOnDateSettingsService.GetRewardPointsOnDateSettingsByIdAsync(id);
            if (settings != null)
                await _rewardPointsOnDateSettingsService.DeleteRewardPointsOnDateSettingsAsync(settings);

            return new NullJsonResult();
        }

        #endregion

        #endregion
    }
}