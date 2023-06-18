﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.ExtendedRewardPointsProgram.Domain;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram.Services
{
    /// <summary>
    /// Represents service for the reward points on specific date settings
    /// </summary>
    public partial class RewardPointsOnDateSettingsService : IRewardPointsOnDateSettingsService
    {
        #region Fields

        private readonly IRepository<RewardPointsOnDateSettings> _repository;

        #endregion

        #region Ctor

        public RewardPointsOnDateSettingsService(IRepository<RewardPointsOnDateSettings> repository)
        {
            this._repository = repository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all reward points on specific dates settings
        /// </summary>
        /// <param name="storeId">The store identifier; pass 0 to load all records</param>
        /// <param name="roleId">Customer role identifier; pass 0 to load all records</param>
        /// <param name="date">Date and time in UTC; pass null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Reward points on specific dates settings</returns>
        public virtual async Task<IPagedList<RewardPointsOnDateSettings>> GetAllRewardPointsOnDateSettingsAsync(int storeId = 0, int roleId = 0,
            DateTime? date = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var rez = await _repository.GetAllAsync(query =>
            {
                //filter by store
                if (storeId > 0)
                    query = query.Where(settings => settings.StoreId == 0 || settings.StoreId == storeId);

                //filter by role
                if (roleId > 0)
                    query = query.Where(settings => settings.CustomerRoleId == 0 || settings.CustomerRoleId == roleId);

                //filter by date
                if (date.HasValue)
                    query = query.Where(settings => settings.AwardingDateUtc <= date);

                query = query.OrderByDescending(settings => settings.AwardingDateUtc);

                return query;
            });

            return new PagedList<RewardPointsOnDateSettings>(rez, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets reward points on specific date settings by identifier
        /// </summary>
        /// <param name="settingsId">Settings identifier</param>
        /// <returns>Reward points on specific date settings</returns>
        public virtual async Task<RewardPointsOnDateSettings> GetRewardPointsOnDateSettingsByIdAsync(int settingsId)
        {
            if (settingsId == 0)
                return null;

            return await _repository.GetByIdAsync(settingsId);
        }

        /// <summary>
        /// Inserts reward points on specific date settings
        /// </summary>
        /// <param name="settings">Reward points on specific date settings</param>
        public virtual async Task InsertRewardPointsOnDateSettingsAsync(RewardPointsOnDateSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            await _repository.InsertAsync(settings);
        }

        /// <summary>
        /// Updates reward points on specific date settings
        /// </summary>
        /// <param name="settings">Reward points on specific date settings</param>
        public virtual async Task UpdateRewardPointsOnDateSettingsAsync(RewardPointsOnDateSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            await _repository.UpdateAsync(settings);
        }

        /// <summary>
        /// Deletes reward points on specific date settings
        /// </summary>
        /// <param name="settings">Reward points on specific date settings</param>
        public virtual async Task DeleteRewardPointsOnDateSettingsAsync(RewardPointsOnDateSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            await _repository.DeleteAsync(settings);
        }

        #endregion
    }
}
