using System.Collections.Generic;
using UnityEngine;

namespace ChronoWarden;

internal enum WardenAbility
{
    PhaseShield,
    TemporalPulse,
    Rewind,
}

internal readonly struct TimeSnapshot
{
    public TimeSnapshot(float timestamp, Vector3 position, float health)
    {
        Timestamp = timestamp;
        Position = position;
        Health = health;
    }

    public float Timestamp { get; }
    public Vector3 Position { get; }
    public float Health { get; }
}

internal sealed class WardenState
{
    public float Energy { get; set; } = 50f;
    public int Kills { get; set; }
    public int Level { get; set; } = 1;
    public float NextAbilityAt { get; set; }
    public bool LastChanceUsed { get; set; }
    public bool IsReviving { get; set; }
    public WardenAbility SelectedAbility { get; set; }
    public Queue<TimeSnapshot> History { get; } = new();
}
