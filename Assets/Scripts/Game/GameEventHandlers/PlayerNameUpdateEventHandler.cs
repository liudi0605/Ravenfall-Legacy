﻿using RavenNest.Models;
using UnityEngine;

public class PlayerNameUpdateEventHandler : GameEventHandler<PlayerNameUpdate>
{
    public override void Handle(GameManager gameManager, PlayerNameUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player) return;
        player.PlayerName = data.Name;
        Shinobytes.Debug.Log($"PlayerNameUpdateEventHandler " + data.UserId);
    }
}

