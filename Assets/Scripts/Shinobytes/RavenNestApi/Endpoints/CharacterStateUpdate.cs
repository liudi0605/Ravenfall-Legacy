﻿
using System;
using static RavenNest.SDK.Endpoints.WebSocketApi;

namespace RavenNest.SDK.Endpoints
{

    public class CharacterSkillUpdate
    {
        public Guid CharacterId;
        public string UserId;
        public double[] Experience;
        public int[] Level;
    }

    public class CharacterExpUpdate
    {
        public Guid CharacterId;
        public int SkillIndex;
        public int Level;
        public double Experience;
    }

    public class ClientSyncUpdate
    {
        public string ClientVersion;
    }

    public class TimeSyncUpdate
    {
        public TimeSpan Delta;
        public DateTime LocalTime;
        public DateTime ServerTime;
    }

    public class CharacterStateUpdate
    {
        public CharacterStateUpdate(
            string userId,
            Guid characterId,
            int health,
            string island,
            string duelOpponent,
            bool inRaid,
            bool inArena,
            bool inDungeon,
            bool inOnsen,
            string task,
            string taskArgument,
            float x, float y, float z)
        {
            UserId = userId;
            CharacterId = characterId;
            Health = health;
            Island = island;
            DuelOpponent = duelOpponent;
            InRaid = inRaid;
            InArena = inArena;
            InDungeon = inDungeon;
            InOnsen = inOnsen;
            Task = task;
            TaskArgument = taskArgument;

            X = x;
            Y = y;
            Z = z;
        }
        public Guid CharacterId;
        public string UserId;
        public int Health;
        public string Island;
        public string DuelOpponent;
        public bool InRaid;
        public bool InArena;
        public bool InDungeon;
        public bool InOnsen;
        public string Task;
        public string TaskArgument;
        public float X;
        public float Y;
        public float Z;
    }
}