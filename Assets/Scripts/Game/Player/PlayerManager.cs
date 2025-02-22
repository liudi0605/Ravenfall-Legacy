﻿using Assets.Scripts;
using RavenNest.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private const string CacheDirectory = "data/";
    private const string CacheFileNameOld = "statcache.json";
    private const string CacheFileName = "data/statcache.bin";
    private const string CacheKey = "Ahgjkeaweg12!2KJAHgkhjeAhgegaeegjasdgauyEGIUM";

    //private readonly List<PlayerController> activePlayers = new List<PlayerController>();

    // Do not clear out this one. the rest of the dictionaries can be cleared. but this is for debugging purposes    
    private readonly Dictionary<string, string> userIdToNameLookup = new Dictionary<string, string>();


    private readonly Dictionary<string, PlayerController> playerTwitchIdLookup = new Dictionary<string, PlayerController>();
    private readonly Dictionary<string, PlayerController> playerNameLookup = new Dictionary<string, PlayerController>();
    private readonly Dictionary<Guid, PlayerController> playerIdLookup = new Dictionary<Guid, PlayerController>();

    private readonly List<PlayerController> playerList = new List<PlayerController>();

    //private readonly object mutex = new object();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameSettings settings;
    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private IoCContainer ioc;

    //public readonly ConcurrentDictionary<Guid, Skills> StoredStats
    //    = new ConcurrentDictionary<Guid, Skills>();

    public readonly ConcurrentQueue<RavenNest.Models.Player> PlayerQueue
        = new ConcurrentQueue<RavenNest.Models.Player>();

    private DateTime lastCacheSave = DateTime.MinValue;

    private ConcurrentQueue<Func<PlayerController>> addPlayerQueue = new ConcurrentQueue<Func<PlayerController>>();

    public bool LoadingPlayers;

    public PlayerController LastAddedPlayer;

    private void LateUpdate()
    {
        if (this.gameManager == null || this.gameManager.RavenNest == null || !this.gameManager.RavenNest.Authenticated || !this.gameManager.RavenNest.SessionStarted)
            return;

        if (addPlayerQueue.Count > 0)
        {
            LoadingPlayers = true;
            if (GameSystems.frameCount % 2 == 0)
            {
                if (addPlayerQueue.TryDequeue(out var addPlayer))
                {
                    addPlayer();
                }
            }

            if (addPlayerQueue.Count == 0)
            {
                gameManager.PostGameRestore();
            }
        }
        else { LoadingPlayers = false; }
    }

    void Start()
    {
        if (!gameManager) gameManager = GetComponent<GameManager>();
        if (!settings) settings = GetComponent<GameSettings>();
        if (!ioc) ioc = GetComponent<IoCContainer>();

        //LoadStatCache();
    }

    internal bool TryGetPlayerName(string userId, out string name)
    {
        return userIdToNameLookup.TryGetValue(userId, out name);
    }

    internal async Task<PlayerController> JoinAsync(TwitchPlayerInfo data, GameClient client, bool userTriggered, bool isBot = false, Guid? characterId = null)
    {
        var Game = gameManager;
        try
        {

            if (string.IsNullOrEmpty(data.UserId))
            {
                Shinobytes.Debug.LogError("A user tried to join the game but had no UserId.");
                return null;
            }

            var addPlayerRequest = data;
            if (Game.RavenNest.SessionStarted)
            {
                if (!Game.Items.Loaded)
                {
                    if (userTriggered)
                    {
                        client?.SendMessage(addPlayerRequest.Username, Localization.GAME_NOT_LOADED);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Game not finished loading.");
                    return null;
                }

                if (Contains(addPlayerRequest.UserId))
                {
                    var alreadyInGameMessage = addPlayerRequest.Username + " failed to be added back to the game. Player is already in game.";
                    if (userTriggered)
                    {
                        client?.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                        Shinobytes.Debug.Log(alreadyInGameMessage);
                        return null;
                    }
                    else
                    {
                        // Try remove the player. this will replace current player with the new one if it has a characterId
                        // Note: this may be a potential bug later if you have one character in and then it get replaced by another.
                        //       the risk of this happening is extremely slim though.
                        var existingPlayer = GetPlayerByUserId(addPlayerRequest.UserId);
                        if (existingPlayer && (characterId == null || existingPlayer.Id == characterId))
                        {
                            Remove(existingPlayer);
                        }
                        else
                        {
                            Shinobytes.Debug.Log(alreadyInGameMessage);
                            return null;
                        }
                    }
                }

                if (!isBot)
                {
                    Game.EventTriggerSystem.SendInput(addPlayerRequest.UserId, "join");
                }

                var playerInfo = await Game.RavenNest.PlayerJoinAsync(
                    new RavenNest.Models.PlayerJoinData
                    {
                        Identifier = string.IsNullOrEmpty(addPlayerRequest.Identifier) ? "0" : addPlayerRequest.Identifier,
                        CharacterId = characterId ?? Guid.Empty,
                        Moderator = addPlayerRequest.IsModerator,
                        Subscriber = addPlayerRequest.IsSubscriber,
                        Vip = addPlayerRequest.IsVip,
                        UserId = addPlayerRequest.UserId,
                        UserName = addPlayerRequest.Username,
                        IsGameRestore = !userTriggered
                    });

                if (playerInfo == null)
                {
                    if (userTriggered)
                    {
                        client?.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED);
                    }
                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Missing PlayerInfo");
                    return null;
                }

                if (!playerInfo.Success)
                {
                    if (userTriggered)
                    {
                        client?.SendMessage(addPlayerRequest.Username, playerInfo.ErrorMessage);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. " + playerInfo.ErrorMessage);
                    return null;
                }

                var player = AddPlayer(addPlayerRequest, playerInfo.Player, isBot);
                if (player)
                {
                    if (userTriggered && !player.IsBot)
                    {
                        userIdToNameLookup[playerInfo.Player.UserId] = playerInfo.Player.UserName;
                        gameManager.SavePlayerStates();
                        client?.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_WELCOME);
                    }
                    return player;
                }
                else
                {
                    if (userTriggered)
                    {
                        client?.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                    }
                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Player is already in game.");
                }
            }
            else
            {
                if (userTriggered)
                    client?.SendMessage(addPlayerRequest.Username, Localization.GAME_NOT_READY);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
        return null;
    }

    private PlayerController AddPlayer(TwitchPlayerInfo twitchUser, RavenNest.Models.Player playerInfo, bool isBot, bool isGameRestore = false)
    {
        var Game = gameManager;
        var player = Game.SpawnPlayer(playerInfo, twitchUser, isGameRestore: isGameRestore);
        if (player)
        {
            player.Movement.Unlock();
            player.IsBot = isBot;
            if (player.IsBot)
            {
                player.Bot = this.gameObject.AddComponent<BotPlayerController>();
                player.Bot.playerController = player;
                if (player.UserId != null && !player.UserId.StartsWith("#"))
                {
                    player.UserId = "#" + player.UserId;
                }
            }

            player.PlayerNameHexColor = twitchUser.Color;
            if (player.IsBroadcaster && !player.IsBot)
            {
                Game.EventTriggerSystem.TriggerEvent("join", TimeSpan.FromSeconds(1));
            }
            LastAddedPlayer = player;
            // receiver:cmd|arg1|arg2|arg3|
            return player;
        }
        return null;
    }

    //private void LoadStatCache()
    //{
    //    if (Shinobytes.IO.File.Exists(CacheFileName))
    //    {
    //        var data = Shinobytes.IO.File.ReadAllText(CacheFileName);
    //        var json = StringCipher.Decrypt(data, CacheKey);
    //        LoadStatCache(Newtonsoft.Json.JsonConvert.DeserializeObject<List<StatCacheData>>(json));
    //    }
    //}

    //private void LoadStatCache(List<StatCacheData> lists)
    //{
    //    foreach (var l in lists)
    //    {
    //        StoredStats[l.Id] = l.Skills;
    //    }
    //}

    //private void SaveStatCache()
    //{
    //    if (!System.IO.Directory.Exists(CacheDirectory))
    //        System.IO.Directory.CreateDirectory(CacheDirectory);

    //    var list = new List<StatCacheData>();
    //    foreach (var k in StoredStats.Keys)
    //    {
    //        list.Add(new StatCacheData
    //        {
    //            Id = k,
    //            Skills = StoredStats[k]
    //        });
    //    }

    //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
    //    var data = StringCipher.Encrypt(json, CacheKey);
    //    Shinobytes.IO.File.WriteAllText(CacheFileName, data);
    //}

    public bool Contains(string userId)
    {
        return GetPlayerByUserId(userId);
    }

    //void Update()
    //{
    //    //var sinceLastSave = DateTime.UtcNow - lastCacheSave;
    //    //if (sinceLastSave >= TimeSpan.FromSeconds(10))
    //    //{
    //    //    SaveStatCache();
    //    //    lastCacheSave = DateTime.UtcNow;
    //    //}
    //}

    public IReadOnlyList<PlayerController> GetAllBots()
    {
        return playerList.AsList(x => x.IsBot);
    }
    public IReadOnlyList<PlayerController> GetAllPlayers()
    {
        return playerList;
    }

    public PlayerController Spawn(
        Vector3 position,
        RavenNest.Models.Player playerDefinition,
        TwitchPlayerInfo twitchUser,
        StreamRaidInfo raidInfo)
    {

        if (playerTwitchIdLookup.ContainsKey(playerDefinition.UserId))
        {
            return null;
        }

        var player = Instantiate(playerControllerPrefab);
        if (!player)
        {
            Shinobytes.Debug.LogError("Player Prefab not found!!!");
            return null;
        }

        player.transform.position = position;

        return Add(player.GetComponent<PlayerController>(), playerDefinition, twitchUser, raidInfo);
    }

    internal IReadOnlyList<PlayerController> GetAllModerators()
    {
        return playerTwitchIdLookup.Values.Where(x => x.IsModerator).ToList();
    }
    internal IReadOnlyList<PlayerController> GetAllGameAdmins()
    {
        return playerTwitchIdLookup.Values.Where(x => x.IsGameAdmin).ToList();
    }

    public PlayerController GetPlayer(TwitchPlayerInfo twitchUser)
    {
        var player = GetPlayerByUserId(twitchUser.UserId);
        if (!player) player = GetPlayerByName(twitchUser.Username);
        if (player)
        {
            player.UpdateTwitchUser(twitchUser);
        }
        return player;
    }

    public PlayerController GetPlayerByUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        if (playerTwitchIdLookup.TryGetValue(userId, out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                playerTwitchIdLookup.Remove(plr.UserId);
                playerNameLookup.Remove(plr.Name.ToLower());
                playerIdLookup.Remove(plr.Id);
                return null;
            }
            return plr;
        }

        return playerTwitchIdLookup.Values.FirstOrDefault(x => x.Id.ToString().Equals(userId, StringComparison.InvariantCultureIgnoreCase));
    }

    public PlayerController GetPlayerByName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return null;

        playerName = playerName.StartsWith("@") ? playerName.Substring(1) : playerName;

        if (playerNameLookup.TryGetValue(playerName.ToLower(), out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                playerTwitchIdLookup.Remove(plr.UserId);
                playerNameLookup.Remove(plr.Name.ToLower());
                playerIdLookup.Remove(plr.Id);
                return null;
            }
            return plr;
        }

        return null;
    }

    public int GetPlayerCount(bool includeNpc = false)
    {
        return playerTwitchIdLookup.Values.Count(x => includeNpc || !x.IsNPC);
    }

    public PlayerController GetPlayerById(Guid characterId)
    {
        if (playerTwitchIdLookup == null)
        {
            return null;
        }

        if (playerIdLookup.TryGetValue(characterId, out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                playerTwitchIdLookup.Remove(plr.UserId);
                playerNameLookup.Remove(plr.Name.ToLower());
                playerIdLookup.Remove(plr.Id);
                return null;
            }
            return plr;
        }

        return null;
    }

    public PlayerController GetPlayerByIndex(int index)
    {
        if (playerList.Count <= index)
        {
            return null;
        }

        return playerList[index];
    }

    public void Remove(PlayerController player)
    {
        if (playerTwitchIdLookup.TryGetValue(player.UserId, out var plrToRemove))
        {
            gameManager.Village.TownHouses.InvalidateOwnership(player);
        }

        if (player)
        {
            playerTwitchIdLookup.Remove(player.UserId);
            playerNameLookup.Remove(player.PlayerName.ToLower());
            playerIdLookup.Remove(player.Id);
            playerList.Remove(player);
            player.OnRemoved();
            Destroy(player.gameObject);
        }
    }

    public IReadOnlyList<PlayerController> FindPlayers(string query)
    {
        return playerList.Where(x => x.PlayerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlayerController Add(
        PlayerController player,
        RavenNest.Models.Player def,
        TwitchPlayerInfo twitchUser,
        StreamRaidInfo raidInfo)
    {

        player.SetPlayer(def, twitchUser, raidInfo, gameManager);
        playerTwitchIdLookup[player.UserId] = player;
        playerNameLookup[player.PlayerName.ToLower()] = player;
        playerIdLookup[player.Id] = player;
        playerList.Add(player);

        gameManager.Village.TownHouses.InvalidateOwnership(player);
        return player;
    }

    internal void UpdateRestedState(RavenNest.Models.PlayerRestedUpdate data)
    {
        if (data == null) return;
        var player = GetPlayerById(data.CharacterId);
        if (player == null) return;
        player.SetRestedState(data);
    }

    internal async Task RestoreAsync(List<GameCachePlayerItem> players)
    {
        // even if players count is 0, do the request.
        // it will ensure the server later removes players not being recovered.
        //if (players.Count == 0) return;

        var failed = new List<GameCachePlayerItem>();
        try
        {
            UnityEngine.Debug.Log("Send Restore to server with " + players.Count + " players.");
            if (players.Count == 0)
            {
                await gameManager.RavenNest.Players.RestoreAsync(new PlayerRestoreData { Characters = new Guid[0] });
                return;
            }

            var id = players.Select(x => x.CharacterId).ToArray();
            var result = await gameManager.RavenNest.Players.RestoreAsync(new PlayerRestoreData
            {
                Characters = id,
            });

            var i = 0;
            foreach (var playerInfo in result.Players)
            {
                var requested = players[i++];

                try
                {
                    if (!playerInfo.Success || playerInfo.Player == null)
                    {
                        failed.Add(requested);
                        Shinobytes.Debug.LogError("Failed to restore player (" + requested.TwitchUser.Username + "): " + playerInfo.ErrorMessage);
                        continue;
                    }

                    addPlayerQueue.Enqueue(() => AddPlayer(requested.TwitchUser, playerInfo.Player, false, true));

                    //var player = AddPlayer(false, requested.TwitchUser, playerInfo.Player);
                    //if (!player)
                    //{
                    //    failed.Add(requested);
                    //}
                }
                catch (Exception e)
                {
                    failed.Add(requested);
                }
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to restore players: " + exc);
            gameManager.RavenBot.Announce("Failed to restore players. See game log files for more details.");
            return;
        }

        if (failed.Count > 0)
        {
            if (failed.Count > 10)
            {
                gameManager.RavenBot.Announce((players.Count - failed.Count) + " out of " + players.Count + " was added back to the game.");
            }
            else
            {
                gameManager.RavenBot.Announce(failed.Count + " players failed to be added back: " + String.Join(", ", failed.Select(x => x.TwitchUser.Username)));
            }
        }
        else
        {

            gameManager.RavenBot.Announce(players.Count + " players restored.");
        }
    }


    //internal Skills GetStoredPlayerSkills(Guid id)
    //{
    //    if (StoredStats.TryGetValue(id, out var skills)) return skills;
    //    return null;
    //}
}

public class StatCacheData
{
    //public string UserId { get; set; }
    public Guid Id { get; set; }
    public Skills Skills { get; set; }
}


public static class StringCipher
{
    // This constant is used to determine the keysize of the encryption algorithm in bits.
    // We divide this by 8 within the code below to get the equivalent number of bytes.
    private const int Keysize = 256;

    // This constant determines the number of iterations for the password bytes generation function.
    private const int DerivationIterations = 1000;

    public static string Encrypt(string plainText, string passPhrase)
    {
        // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        var saltStringBytes = Generate256BitsOfRandomEntropy();
        var ivStringBytes = Generate256BitsOfRandomEntropy();
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                            var cipherTextBytes = saltStringBytes;
                            cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                            memoryStream.Close();
                            cryptoStream.Close();
                            return Convert.ToBase64String(cipherTextBytes);
                        }
                    }
                }
            }
        }
    }

    //public static string Decrypt(string cipherText, string passPhrase)
    //{
    //    // Get the complete stream of bytes that represent:
    //    // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
    //    var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
    //    // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
    //    var saltStringBytes = cipherTextBytesWithSaltAndIv.Slice(0, Keysize / 8).ToArray();
    //    // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
    //    var ivStringBytes = cipherTextBytesWithSaltAndIv.Slice(Keysize / 8, Keysize / 8).ToArray();
    //    // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
    //    var cipherTextBytes = cipherTextBytesWithSaltAndIv.Slice((Keysize / 8) * 2, cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

    //    using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
    //    {
    //        var keyBytes = password.GetBytes(Keysize / 8);
    //        using (var symmetricKey = new RijndaelManaged())
    //        {
    //            symmetricKey.BlockSize = 256;
    //            symmetricKey.Mode = CipherMode.CBC;
    //            symmetricKey.Padding = PaddingMode.PKCS7;
    //            using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
    //            {
    //                using (var memoryStream = new MemoryStream(cipherTextBytes))
    //                {
    //                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
    //                    {
    //                        var plainTextBytes = new byte[cipherTextBytes.Length];
    //                        var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
    //                        memoryStream.Close();
    //                        cryptoStream.Close();
    //                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    private static byte[] Generate256BitsOfRandomEntropy()
    {
        var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
        using (var rngCsp = new RNGCryptoServiceProvider())
        {
            // Fill the array with cryptographically secure random bytes.
            rngCsp.GetBytes(randomBytes);
        }
        return randomBytes;
    }
}