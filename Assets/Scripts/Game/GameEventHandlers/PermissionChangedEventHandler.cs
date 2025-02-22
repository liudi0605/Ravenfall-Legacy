﻿public class PermissionChangedEventHandler : GameEventHandler<Permissions>
{
    public override void Handle(GameManager gameManager, Permissions data)
    {
        //UnityEngine.Debug.LogWarning("User Permission Update received.");
        gameManager.Permissions = data;

        ChunkManager.StrictLevelRequirements = data.StrictLevelRequirements;
    }
}
