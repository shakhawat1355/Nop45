using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Events;
using Nop.Plugin.Misc.ExtendedRewardPointsProgram.Services;
using Nop.Services.Blogs;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.News;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.ExtendedRewardPointsProgram.Infrastructure.Cache
{
    /// <summary>
    /// Represents extended reward points program event consumer
    /// </summary>
    public partial class EventConsumer :
        IConsumer<BlogCommentApprovedEvent>,
        IConsumer<NewsCommentApprovedEvent>,
        IConsumer<ProductReviewApprovedEvent>,
        IConsumer<CustomerRegisteredEvent>,
        IConsumer<EmailSubscribedEvent>,
        IConsumer<OrderPaidEvent>,
        IConsumer<EntityInsertedEvent<ShoppingCartItem>>
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderService _orderService;
        private readonly IRewardPointService _rewardPointService;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;
        private readonly IBlogService _blogService;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly INewsService _newsService;
        private readonly RewardPointsForBlogCommentsSettings _rewardPointsSettingsForBlogComments;
        private readonly RewardPointsForFastPurchaseSettings _rewardPointsSettingsForFastPurchase;
        private readonly RewardPointsForFirstPurchaseSettings _rewardPointsSettingsForFirstPurchase;
        private readonly RewardPointsForNewsCommentsSettings _rewardPointsSettingsForNewsComments;
        private readonly RewardPointsForNewsletterSubscriptionsSettings _rewardPointsSettingsForNewsletterSubscriptions;
        private readonly RewardPointsForProductReviewsSettings _rewardPointsSettingsForProductReviews;
        private readonly RewardPointsForRegistrationSettings _rewardPointsForRegistrationSettings;

        #endregion

        #region Ctor

        public EventConsumer(ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IOrderService orderService,
            IRewardPointService rewardPointService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            IBlogService blogService,
            IProductService productService,
            IShoppingCartService shoppingCartService,
            INewsService newsService,
            RewardPointsForBlogCommentsSettings rewardPointsSettingsForBlogComments,
            RewardPointsForFastPurchaseSettings rewardPointsForFastPurchaseSettings,
            RewardPointsForFirstPurchaseSettings rewardPointsSettingsForFirstPurchase,
            RewardPointsForNewsCommentsSettings rewardPointsSettingsForNewsComments,
            RewardPointsForNewsletterSubscriptionsSettings rewardPointsSettingsForNewsletterSubscriptions,
            RewardPointsForProductReviewsSettings rewardPointsSettingsForProductReviews,
            RewardPointsForRegistrationSettings rewardPointsForRegistrationSettings)
        {
            this._customerService = customerService;
            this._genericAttributeService = genericAttributeService;
            this._orderService = orderService;
            this._rewardPointService = rewardPointService;
            this._storeContext = storeContext;
            this._localizationService = localizationService;
            _blogService = blogService;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _newsService = newsService;
            this._rewardPointsSettingsForBlogComments = rewardPointsSettingsForBlogComments;
            this._rewardPointsSettingsForFastPurchase = rewardPointsForFastPurchaseSettings;
            this._rewardPointsSettingsForFirstPurchase = rewardPointsSettingsForFirstPurchase;
            this._rewardPointsSettingsForNewsComments = rewardPointsSettingsForNewsComments;
            this._rewardPointsSettingsForNewsletterSubscriptions = rewardPointsSettingsForNewsletterSubscriptions;
            this._rewardPointsSettingsForProductReviews = rewardPointsSettingsForProductReviews;
            this._rewardPointsForRegistrationSettings = rewardPointsForRegistrationSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get reward points activation date
        /// </summary>
        /// <param name="settings">Reward points settings</param>
        /// <returns>Date and time or null for the instant activation</returns>
        protected DateTime? GetRewardPointsActivationDate(RewardPointsSettings settings)
        {
            if (settings.ActivationDelay <= 0)
                return null;

            var delayPeriod = (RewardPointsActivatingDelayPeriod)settings.ActivationDelayPeriodId;
            var delayInHours = delayPeriod.ToHours(settings.ActivationDelay);

            return DateTime.UtcNow.AddHours(delayInHours);
        }

        #endregion

        #region Methods

        #region Blog comments

        public async Task HandleEventAsync(BlogCommentApprovedEvent blogCommentEvent)
        {
            if (!_rewardPointsSettingsForBlogComments.IsEnabled)
                return;

            if (blogCommentEvent.BlogComment.CustomerId < 1)
                return;

            var customer = await _customerService.GetCustomerByIdAsync(blogCommentEvent.BlogComment.CustomerId);
            if (customer == null)
                return;

            var blogPost = await _blogService.GetBlogPostByIdAsync(blogCommentEvent.BlogComment.BlogPostId);
            if (blogPost == null)
                return;

            //reward user only for the first approving
            if (await _genericAttributeService.GetAttributeAsync<bool>(blogCommentEvent.BlogComment, "CustomerAwardedForBlogComment", blogCommentEvent.BlogComment.StoreId))
                return;

            //check whether delay is set
            var activationDate = GetRewardPointsActivationDate(_rewardPointsSettingsForBlogComments);

            //get message for the current customer
            var languageId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.LanguageIdAttribute, blogCommentEvent.BlogComment.StoreId);
            var message = await _rewardPointsSettingsForBlogComments.GetLocalizedSettingAsync(settings => settings.Message, languageId, blogCommentEvent.BlogComment.StoreId);

            //add reward points for approved blog post comment
            await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, _rewardPointsSettingsForBlogComments.Points,
                blogCommentEvent.BlogComment.StoreId, string.Format(message, blogPost.Title), activatingDate: activationDate);

            //mark that customer was already awarded for the comment
            await _genericAttributeService.SaveAttributeAsync(blogCommentEvent.BlogComment, "CustomerAwardedForBlogComment", true, blogCommentEvent.BlogComment.StoreId);
        }

        #endregion

        #region News comments

        public async Task HandleEventAsync(NewsCommentApprovedEvent newsCommentEvent)
        {
            if (!_rewardPointsSettingsForNewsComments.IsEnabled)
                return;

            if (newsCommentEvent.NewsComment.CustomerId < 1)
                return;

            var customer = await _customerService.GetCustomerByIdAsync(newsCommentEvent.NewsComment.CustomerId);
            if (customer == null)
                return;

            var newsItem = await _newsService.GetNewsByIdAsync(newsCommentEvent.NewsComment.NewsItemId);
            if (newsItem == null)
                return;

            //reward user only for the first approving
            if (await _genericAttributeService.GetAttributeAsync<bool>(newsCommentEvent.NewsComment, "CustomerAwardedForNewsComment", newsCommentEvent.NewsComment.StoreId))
                return;

            //check whether delay is set
            var activationDate = GetRewardPointsActivationDate(_rewardPointsSettingsForNewsComments);

            //get message for the current customer
            var languageId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.LanguageIdAttribute, newsCommentEvent.NewsComment.StoreId);
            var message = await _rewardPointsSettingsForNewsComments.GetLocalizedSettingAsync(settings => settings.Message, languageId, newsCommentEvent.NewsComment.StoreId);

            //add reward points for approved news comment
            await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, _rewardPointsSettingsForNewsComments.Points,
                newsCommentEvent.NewsComment.StoreId, string.Format(message, newsItem.Title), activatingDate: activationDate);

            //mark that customer was already awarded for the comment
            await _genericAttributeService.SaveAttributeAsync(newsCommentEvent.NewsComment, "CustomerAwardedForNewsComment", true, newsCommentEvent.NewsComment.StoreId);
        }

        #endregion

        #region Product reviews

        public async Task HandleEventAsync(ProductReviewApprovedEvent productReviewEvent)
        {
            if (!_rewardPointsSettingsForProductReviews.IsEnabled)
                return;

            if (productReviewEvent.ProductReview.CustomerId < 1)
                return;

            var customer = await _customerService.GetCustomerByIdAsync(productReviewEvent.ProductReview.CustomerId);
            if (customer == null)
                return;

            var product = await _productService.GetProductByIdAsync(productReviewEvent.ProductReview.ProductId);
            if (product == null)
                return;

            //reward user only for the first approving
            if (await _genericAttributeService.GetAttributeAsync<bool>(productReviewEvent.ProductReview, "CustomerAwardedForProductReview", productReviewEvent.ProductReview.StoreId))
                return;

            //check whether delay is set
            var activationDate = GetRewardPointsActivationDate(_rewardPointsSettingsForProductReviews);

            //get message for the current customer
            var languageId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.LanguageIdAttribute, productReviewEvent.ProductReview.StoreId);
            var message = await _rewardPointsSettingsForProductReviews.GetLocalizedSettingAsync(settings => settings.Message, languageId, productReviewEvent.ProductReview.StoreId);
            var productName = await _localizationService.GetLocalizedAsync(product, product => product.Name, languageId);

            //add reward points for approved product review
            await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, _rewardPointsSettingsForProductReviews.Points,
                productReviewEvent.ProductReview.StoreId, string.Format(message, productName), activatingDate: activationDate);

            //mark that customer was already awarded for the review
            await _genericAttributeService.SaveAttributeAsync(productReviewEvent.ProductReview, "CustomerAwardedForProductReview", true, productReviewEvent.ProductReview.StoreId);
        }

        #endregion

        #region Registration

        public async Task HandleEventAsync(CustomerRegisteredEvent registeredEvent)
        {
            if (!_rewardPointsForRegistrationSettings.IsEnabled)
                return;

            if (registeredEvent.Customer == null)
                return;

            //check whether delay is set
            var activationDate = GetRewardPointsActivationDate(_rewardPointsForRegistrationSettings);

            //get message for the current customer
            var languageId = await _genericAttributeService.GetAttributeAsync<int>(registeredEvent.Customer, NopCustomerDefaults.LanguageIdAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
            var message = await _rewardPointsForRegistrationSettings.GetLocalizedSettingAsync(settings => settings.Message, languageId, (await _storeContext.GetCurrentStoreAsync()).Id);

            //add reward points for approved product review
            await _rewardPointService.AddRewardPointsHistoryEntryAsync(registeredEvent.Customer, _rewardPointsForRegistrationSettings.Points,
                 (await _storeContext.GetCurrentStoreAsync()).Id, message, activatingDate: activationDate);
        }

        #endregion

        #region Email subscriptions

        public async Task HandleEventAsync(EmailSubscribedEvent subscribedEvent)
        {
            if (!_rewardPointsSettingsForNewsletterSubscriptions.IsEnabled)
                return;

            //find customer
            var customer = await _customerService.GetCustomerByEmailAsync(subscribedEvent.Subscription.Email);
            if (customer == null)
                return;

            //reward user only for the first subscription
            if (await _genericAttributeService.GetAttributeAsync<bool>(subscribedEvent.Subscription, "CustomerAwardedForSubscription", subscribedEvent.Subscription.StoreId))
                return;

            //check whether delay is set
            var activationDate = GetRewardPointsActivationDate(_rewardPointsSettingsForNewsletterSubscriptions);

            //get message for the current customer
            var languageId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.LanguageIdAttribute, subscribedEvent.Subscription.StoreId);
            var message = await _rewardPointsSettingsForNewsletterSubscriptions.GetLocalizedSettingAsync(settings => settings.Message, languageId, subscribedEvent.Subscription.StoreId);

            //add reward points for newsletter subscription
            await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, _rewardPointsSettingsForNewsletterSubscriptions.Points,
                subscribedEvent.Subscription.StoreId, message, activatingDate: activationDate);

            await _genericAttributeService.SaveAttributeAsync(subscribedEvent.Subscription, "CustomerAwardedForSubscription", true, subscribedEvent.Subscription.StoreId);
        }

        #endregion

        #region Fast purchase

        public async Task HandleEventAsync(EntityInsertedEvent<ShoppingCartItem> insertedShoppingCartItem)
        {
            if (!_rewardPointsSettingsForFastPurchase.IsEnabled)
                return;

            if (insertedShoppingCartItem.Entity.CustomerId < 1)
                return;

            var customer = await _customerService.GetCustomerByIdAsync(insertedShoppingCartItem.Entity.CustomerId);
            if (customer == null)
                return;

            var shoppingCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, insertedShoppingCartItem.Entity.StoreId);

            if (shoppingCart == null || shoppingCart.Count > 1)
                return;

            //record time of the first adding shopping cart item
            await _genericAttributeService.SaveAttributeAsync<DateTime?>(customer, "PurchaseStartTime", DateTime.UtcNow, insertedShoppingCartItem.Entity.StoreId);
        }

        #endregion

        #region First purchase

        public async Task HandleEventAsync(OrderPaidEvent orderPaidEvent)
        {
            if (orderPaidEvent.Order.CustomerId < 1)
                return;

            var customer = await _customerService.GetCustomerByIdAsync(orderPaidEvent.Order.CustomerId);
            if (customer == null)
                return;

            //reward points for the first purchase
            if (_rewardPointsSettingsForFirstPurchase.IsEnabled)
            {
                //check whether this order is the first
                var paidOrders = await _orderService.SearchOrdersAsync(customerId: orderPaidEvent.Order.CustomerId, psIds: new[] { (int)PaymentStatus.Paid }.ToList());
                if (paidOrders.TotalCount > 1)
                    return;

                //check whether delay is set
                var activationDate = GetRewardPointsActivationDate(_rewardPointsSettingsForFirstPurchase);

                //get message for the current customer
                var message = await _rewardPointsSettingsForFirstPurchase.GetLocalizedSettingAsync(settings => settings.Message,
                    orderPaidEvent.Order.CustomerLanguageId, orderPaidEvent.Order.StoreId);

                //add reward points for the first purchase
                await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, _rewardPointsSettingsForFirstPurchase.Points,
                    orderPaidEvent.Order.StoreId, message, activatingDate: activationDate);
            }

            //reward points for the fast purchase
            if (_rewardPointsSettingsForFastPurchase.IsEnabled)
            {
                //get start time of purchase
                var purchaseStartTime = await _genericAttributeService.GetAttributeAsync<DateTime?>(customer, "PurchaseStartTime", orderPaidEvent.Order.StoreId);
                if (!purchaseStartTime.HasValue)
                    return;

                //clear start time
                await _genericAttributeService.SaveAttributeAsync<DateTime?>(customer, "PurchaseStartTime", null, orderPaidEvent.Order.StoreId);

                //compare the time of purchase with the set time span
                if (DateTime.UtcNow.Subtract(purchaseStartTime.Value) > TimeSpan.FromMinutes(_rewardPointsSettingsForFastPurchase.Minutes ?? 0))
                    return;

                //check whether delay is set
                var activationDate = GetRewardPointsActivationDate(_rewardPointsSettingsForFastPurchase);

                //get message for the current customer
                var message = await _rewardPointsSettingsForFastPurchase.GetLocalizedSettingAsync(settings => settings.Message,
                    orderPaidEvent.Order.CustomerLanguageId, orderPaidEvent.Order.StoreId);

                //add reward points for the first purchase
                await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, _rewardPointsSettingsForFastPurchase.Points,
                    orderPaidEvent.Order.StoreId, message, activatingDate: activationDate);
            }
        }

        #endregion

        #endregion
    }
}
