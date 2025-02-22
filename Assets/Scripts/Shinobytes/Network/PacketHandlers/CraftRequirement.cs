﻿using RavenNest.Models;
using System.Linq;
using System.Text;

public class CraftRequirement : ChatBotCommandHandler<TradeItemRequest>
{
    public CraftRequirement(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            client.SendMessage(player, Localization.MSG_CRAFT_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var msg = GetItemCraftingRequirements(player, client, item.Item.Item);
        client.SendMessage(player, msg);
    }

    private string GetItemCraftingRequirements(PlayerController player, GameClient client, Item item)
    {
        var requiredItemsStr = new StringBuilder();
        if (item != null)
        {
            requiredItemsStr.Append("You need ");
            if (item.WoodCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Wood)}/{Utility.FormatValue(item.WoodCost)} Wood, ");
            }

            if (item.OreCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Ore)}/{Utility.FormatValue(item.OreCost)} Ore, ");
            }

            foreach (var req in item.CraftingRequirements)
            {
                var requiredItem = Game.Items.Get(req.ResourceItemId);
                var ownedNumber = 0L;
                var items = player.Inventory.GetInventoryItems(req.ResourceItemId);
                if (items != null)
                {
                    ownedNumber = (long)items.Sum(x => x.Amount);
                }
                requiredItemsStr.Append($"{Utility.FormatValue(ownedNumber)}/{Utility.FormatValue(req.Amount)} {requiredItem.Name}, ");
            }
            requiredItemsStr.Append($"crafting level {item.RequiredCraftingLevel} ");
            requiredItemsStr.Append("to craft " + item.Name);
        }
        return requiredItemsStr.ToString();
    }
}
