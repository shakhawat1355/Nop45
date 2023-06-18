using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram.Models
{
    public record RewardPointsOnDateModel : BaseNopEntityModel, ILocalizedModel<LocalizedModel>, ISettingsModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        public RewardPointsOnDateModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableCustomerRoles = new List<SelectListItem>();
            Locales = new List<LocalizedModel>();
        }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.Points")]
        public int Points { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.AwardingDate")]
        [UIHint("DateTime")]
        public DateTime AwardingDateUtc { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.Fields.Message")]
        public string Message { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.CustomerRole")]
        public int CustomerRoleId { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
        public string CustomerRole { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ExtendedRewardPointsProgram.RewardPointsOnDate.Fields.Store")]
        public int StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
        public string Store { get; set; }

        public IList<LocalizedModel> Locales { get; set; }
    }
}
