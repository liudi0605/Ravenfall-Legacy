﻿using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public class MarketplaceApi
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        public MarketplaceApi(
            RavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<ItemSellResult> SellItemAsync(string userId, Guid itemId, long amount, long pricePerItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("sell")
                .AddParameter(itemId.ToString())
                .AddParameter((amount).ToString())
                .AddParameter((pricePerItem).ToString())
                .Build()
                .SendAsync<ItemSellResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }

        public Task<ItemBuyResult> BuyItemAsync(string userId, Guid itemId, long amount, long maxPricePerItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("buy")
                .AddParameter(itemId.ToString())
                .AddParameter((amount).ToString())
                .AddParameter((maxPricePerItem).ToString())
                .Build()
                .SendAsync<ItemBuyResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }
    }
}