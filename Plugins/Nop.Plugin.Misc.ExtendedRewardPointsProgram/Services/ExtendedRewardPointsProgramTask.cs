﻿using System;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.ScheduleTasks;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram.Services
{
    /// <summary>
    /// Represents task for extended reward points program
    /// </summary>
    public partial class ExtendedRewardPointsProgramTask : IScheduleTask
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IRewardPointsOnDateSettingsService _rewardPointsOnDateSettingsService;
        private readonly IRewardPointService _rewardPointService;
        private readonly IStoreService _storeService;
        private readonly ILocalizationService _localizationService;
        private readonly Core.Domain.Customers.RewardPointsSettings _rewardPointsSettings;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public ExtendedRewardPointsProgramTask(ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IRewardPointsOnDateSettingsService rewardPointsOnDateSettingsService,
            IRewardPointService rewardPointService,
            IStoreService storeService,
            ILocalizationService localizationService,
            Core.Domain.Customers.RewardPointsSettings rewardPointsSettings,
            IStoreContext storeContext)
        {
            this._customerService = customerService;
            this._genericAttributeService = genericAttributeService;
            this._rewardPointsOnDateSettingsService = rewardPointsOnDateSettingsService;
            this._rewardPointService = rewardPointService;
            this._storeService = storeService;
            this._localizationService = localizationService;
            this._rewardPointsSettings = rewardPointsSettings;
            this._storeContext = storeContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes a task
        /// </summary>
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            //find all settings of reward points that were to be awarded to the current date
            var allPastAwardedPointsSettings = await _rewardPointsOnDateSettingsService.GetAllRewardPointsOnDateSettingsAsync(date: DateTime.UtcNow);

            //get not yet awarded
            var notAwarded = await allPastAwardedPointsSettings.WhereAwait(async settings =>
            !await _genericAttributeService.GetAttributeAsync<bool>(settings, "CustomersAwardedOnDate")).ToListAsync();

            //and now award it
            foreach (var settings in notAwarded)
            {
                //find users with appropriate customer roles
                var customerRoles = settings.CustomerRoleId > 0 ? new[] { settings.CustomerRoleId } : null;
                var customers = await _customerService.GetAllCustomersAsync(customerRoleIds: customerRoles);

                //get stores for which current awarding is actual
                var storeIds = settings.StoreId > 0 || _rewardPointsSettings.PointsAccumulatedForAllStores
                    ? new[] {settings.StoreId > 0 ? settings.StoreId : (await _storeContext.GetCurrentStoreAsync()).Id }.ToList()
                    : (await _storeService.GetAllStoresAsync()).Select(store => store.Id).ToList();

                foreach (var storeId in storeIds)
                {
                    foreach (var customer in customers)
                    {
                        //get localized message for appropriate customer
                        var languageId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.LanguageIdAttribute, storeId);
                        var message = await _localizationService.GetLocalizedAsync(settings, setting => setting.Message, languageId);

                        //add reward points on specific date
                        await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, settings.Points, storeId, message);
                    }
                }

                //reward only once for each settings
                await _genericAttributeService.SaveAttributeAsync(settings, "CustomersAwardedOnDate", true);
            }
        }

        #endregion
    }
}
