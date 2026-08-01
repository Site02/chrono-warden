using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace ChronoWarden;

internal sealed class WardenManager
{
    private readonly ChronoWardenPlugin plugin;
    private readonly Dictionary<int, WardenState> wardens = new();
    private readonly System.Random random = new();
    private CoroutineHandle loop;
    private bool roundActive;

    public WardenManager(ChronoWardenPlugin plugin) => this.plugin = plugin;

    private Config Config => plugin.Config;

    public void StartLoop() => loop = Timing.RunCoroutine(GameLoop());

    public void StopLoop() => Timing.KillCoroutines(loop);

    public void OnRoundStarted()
    {
        roundActive = true;
        ResetRound(false);
        Timing.CallDelayed(2f, SelectInitialWardens);
    }

    public void OnRoundEnded(RoundEndedEventArgs ev)
    {
        roundActive = false;
        ResetRound(false);
    }

    public void OnChangedRole(PlayerChangedRoleEventArgs ev)
    {
        if (!wardens.TryGetValue(ev.Player.PlayerId, out WardenState state) || ev.NewRole.RoleTypeId == RoleTypeId.ClassD)
            return;

        if (state.IsReviving && ev.NewRole.RoleTypeId == RoleTypeId.Spectator)
            return;

        RemoveWarden(ev.Player, "角色已变化，时序能力解除。", false);
    }

    public void OnTogglingNoclip(PlayerTogglingNoclipEventArgs ev)
    {
        if (!wardens.ContainsKey(ev.Player.PlayerId))
            return;

        ev.IsAllowed = false;
        CycleAbility(ev.Player);
    }

    public void OnDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        if (!wardens.TryGetValue(ev.Player.PlayerId, out WardenState state) || ev.Item.Type != ItemType.Coin)
            return;

        ev.IsAllowed = false;
        if (Time.time < state.NextAbilityAt)
        {
            ev.Player.SendHint($"<color=#ffb347>技能冷却中：{state.NextAbilityAt - Time.time:0.0}s</color>", 2f);
            return;
        }

        switch (state.SelectedAbility)
        {
            case WardenAbility.PhaseShield:
                UseShield(ev.Player, state);
                break;
            case WardenAbility.TemporalPulse:
                UsePulse(ev.Player, state);
                break;
            case WardenAbility.Rewind:
                UseRewind(ev.Player, state);
                break;
        }
    }

    public void OnDeath(PlayerDeathEventArgs ev)
    {
        if (wardens.TryGetValue(ev.Player.PlayerId, out WardenState victimState))
        {
            if (TryLastChance(ev.Player, victimState))
                return;

            wardens.Remove(ev.Player.PlayerId);
        }

        if (ev.Attacker is null || ev.Attacker == ev.Player || !wardens.TryGetValue(ev.Attacker.PlayerId, out WardenState killerState))
            return;

        killerState.Kills++;
        killerState.Energy = Mathf.Min(100f, killerState.Energy + Config.KillEnergy);
        int newLevel = Mathf.Clamp(1 + (killerState.Kills / Math.Max(1, Config.KillsPerLevel)), 1, 3);
        if (newLevel > killerState.Level)
        {
            killerState.Level = newLevel;
            ev.Attacker.MaxHealth += 15f;
            ev.Attacker.Health = Mathf.Min(ev.Attacker.MaxHealth, ev.Attacker.Health + 35f);
            ev.Attacker.SendBroadcast($"<color=#66e0ff><b>时序同步提升至 Lv.{newLevel}</b></color>\n最大生命与恢复效率增强。", 6, Broadcast.BroadcastFlags.Normal, true);
        }
    }

    public bool Assign(Player player, out string response)
    {
        if (player.Role != RoleTypeId.ClassD)
        {
            response = $"{player.Nickname} 必须是 D 级人员。";
            return false;
        }

        if (wardens.ContainsKey(player.PlayerId))
        {
            response = $"{player.Nickname} 已经是时序守望者。";
            return false;
        }

        MakeWarden(player);
        response = $"已将 {player.Nickname} 指定为时序守望者。";
        return true;
    }

    public bool Remove(Player player, out string response)
    {
        if (!wardens.ContainsKey(player.PlayerId))
        {
            response = $"{player.Nickname} 不是时序守望者。";
            return false;
        }

        RemoveWarden(player, "管理员已解除你的特殊角色。", true);
        response = $"已移除 {player.Nickname} 的时序守望者身份。";
        return true;
    }

    public string GetStatus()
    {
        if (wardens.Count == 0)
            return "当前没有时序守望者。";

        return string.Join("\n", wardens.Select(pair =>
        {
            Player? player = Player.Get(pair.Key);
            WardenState state = pair.Value;
            return player is null
                ? $"#{pair.Key}（离线）"
                : $"#{pair.Key} {player.Nickname} | Lv.{state.Level} | 能量 {state.Energy:0} | 击杀 {state.Kills} | {AbilityName(state.SelectedAbility)}";
        }));
    }

    public void ResetRound(bool reseed)
    {
        foreach (int playerId in wardens.Keys.ToArray())
        {
            Player? player = Player.Get(playerId);
            if (player is not null)
                RestorePlayer(player);
        }

        wardens.Clear();
        if (reseed && roundActive)
            SelectInitialWardens();
    }

    public void ApplyReloadedConfig()
    {
        foreach (int playerId in wardens.Keys.ToArray())
        {
            Player? player = Player.Get(playerId);
            if (player is null)
            {
                wardens.Remove(playerId);
                continue;
            }

            player.MaxHealth = Config.MaxHealth + ((wardens[playerId].Level - 1) * 15f);
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
    }

    private void SelectInitialWardens()
    {
        if (!Config.IsEnabled || !roundActive || Config.MaxWardens <= 0)
            return;

        List<Player> candidates = Player.ReadyList
            .Where(player => player.Role == RoleTypeId.ClassD && !wardens.ContainsKey(player.PlayerId))
            .OrderBy(_ => random.Next())
            .ToList();

        foreach (Player player in candidates)
        {
            if (wardens.Count >= Config.MaxWardens)
                break;

            if (random.NextDouble() * 100d <= Config.SpawnChance)
                MakeWarden(player);
        }
    }

    private void MakeWarden(Player player)
    {
        WardenState state = new();
        wardens[player.PlayerId] = state;
        player.MaxHealth = Config.MaxHealth;
        player.Health = Config.MaxHealth;
        player.AddItem(ItemType.Coin);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Adrenaline);
        player.SendBroadcast(
            "<color=#66e0ff><b>你是「时序守望者」</b></color>\n" +
            "丢出硬币：施放当前技能；轻击硬币：切换技能。\n" +
            "技能依次为护盾 / 脉冲 / 回溯，能量会自动恢复，击杀可升级。",
            Config.IntroDuration,
            Broadcast.BroadcastFlags.Normal,
            true);
    }

    private void RemoveWarden(Player player, string hint, bool restore)
    {
        wardens.Remove(player.PlayerId);
        if (restore)
            RestorePlayer(player);
        player.SendHint(hint, 4f);
    }

    private void RestorePlayer(Player player)
    {
        if (player.Role != RoleTypeId.ClassD)
            return;

        player.MaxHealth = 100f;
        player.Health = Mathf.Min(player.Health, player.MaxHealth);
        player.ArtificialHealth = 0f;
    }

    private void UseShield(Player player, WardenState state)
    {
        float cost = Mathf.Max(10f, Config.ShieldCost - ((state.Level - 1) * 5f));
        if (!SpendEnergy(player, state, cost))
            return;

        float shield = Config.ShieldAmount + ((state.Level - 1) * 15f);
        player.MaxArtificialHealth = Mathf.Max(player.MaxArtificialHealth, shield);
        player.ArtificialHealth = Mathf.Max(player.ArtificialHealth, shield);
        BeginCooldown(state);
        player.SendHint($"<color=#66e0ff>相位护盾已展开：{shield:0} AHP</color>", 3f);
    }

    private void UsePulse(Player player, WardenState state)
    {
        float cost = Mathf.Max(20f, Config.PulseCost - ((state.Level - 1) * 5f));
        if (!SpendEnergy(player, state, cost))
            return;

        int affected = 0;
        foreach (Player target in Player.ReadyList)
        {
            if (target == player || !target.IsAlive || Vector3.Distance(player.Position, target.Position) > Config.PulseRadius)
                continue;

            if (target.IsHuman)
            {
                float healing = Config.PulseHealing + ((state.Level - 1) * 10f);
                target.Health = Mathf.Min(target.MaxHealth, target.Health + healing);
                target.SendHint("<color=#66e0ff>附近的时序脉冲修复了你的伤势。</color>", 2f);
            }
            else
            {
                float damage = Config.PulseDamage + ((state.Level - 1) * 10f);
                target.Damage(damage, "时滞脉冲");
            }

            affected++;
        }

        BeginCooldown(state);
        player.SendHint($"<color=#66e0ff>时滞脉冲释放，影响 {affected} 个目标。</color>", 3f);
    }

    private void UseRewind(Player player, WardenState state)
    {
        float cost = Mathf.Max(35f, Config.RewindCost - ((state.Level - 1) * 5f));
        if (!SpendEnergy(player, state, cost))
            return;

        TimeSnapshot? snapshot = FindSnapshot(state, Config.RewindSeconds);
        if (!snapshot.HasValue)
        {
            state.Energy = Mathf.Min(100f, state.Energy + cost);
            player.SendHint("<color=#ffb347>尚无足够的时间记录。</color>", 2f);
            return;
        }

        player.Position = snapshot.Value.Position;
        player.Health = Mathf.Clamp(snapshot.Value.Health, 1f, player.MaxHealth);
        BeginCooldown(state);
        player.SendHint($"<color=#66e0ff>已回溯 {Config.RewindSeconds:0.#} 秒。</color>", 3f);
    }

    private bool TryLastChance(Player player, WardenState state)
    {
        if (!Config.EnableLastChance || state.Level < 3 || state.LastChanceUsed || state.Energy < 100f)
            return false;

        TimeSnapshot? snapshot = FindSnapshot(state, Config.RewindSeconds);
        if (!snapshot.HasValue)
            return false;

        state.LastChanceUsed = true;
        state.IsReviving = true;
        state.Energy = 0f;
        TimeSnapshot target = snapshot.Value;
        Timing.CallDelayed(0.1f, () =>
        {
            if (player.Role == RoleTypeId.Spectator)
            {
                player.SetRole(RoleTypeId.ClassD);
                Timing.CallDelayed(0.5f, () =>
                {
                    player.Position = target.Position;
                    player.MaxHealth = Config.MaxHealth + 30f;
                    player.Health = Mathf.Max(45f, target.Health);
                    player.AddItem(ItemType.Coin);
                    player.SendBroadcast("<color=#66e0ff><b>时间拒绝了你的死亡。</b></color>\n濒死回溯本局只能触发一次。", 7, Broadcast.BroadcastFlags.Normal, true);
                });
            }
        });
        return true;
    }

    private IEnumerator<float> GameLoop()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
            if (!Config.IsEnabled)
                continue;

            foreach (int playerId in wardens.Keys.ToArray())
            {
                Player? player = Player.Get(playerId);
                if (player is null || !player.IsAlive || player.Role != RoleTypeId.ClassD)
                    continue;

                WardenState state = wardens[playerId];
                state.Energy = Mathf.Min(100f, state.Energy + Config.EnergyPerSecond + ((state.Level - 1) * 0.75f));
                state.History.Enqueue(new TimeSnapshot(Time.time, player.Position, player.Health));
                while (state.History.Count > 0 && Time.time - state.History.Peek().Timestamp > Math.Max(20f, Config.RewindSeconds + 3f))
                    state.History.Dequeue();

                player.SendHint(
                    $"<align=right><color=#66e0ff><b>时序守望者 Lv.{state.Level}</b></color>\n" +
                    $"能量 {state.Energy:0}/100 | 击杀 {state.Kills}\n" +
                    $"当前：{AbilityName(state.SelectedAbility)}（丢硬币施放）\n" +
                    "轻击硬币切换技能</align>",
                    1.2f);
            }
        }
    }

    public void CycleAbility(Player player)
    {
        if (!wardens.TryGetValue(player.PlayerId, out WardenState state))
            return;

        state.SelectedAbility = (WardenAbility)(((int)state.SelectedAbility + 1) % 3);
        player.SendHint($"<color=#66e0ff>已切换：{AbilityName(state.SelectedAbility)}</color>", 2f);
    }

    private bool SpendEnergy(Player player, WardenState state, float amount)
    {
        if (state.Energy >= amount)
        {
            state.Energy -= amount;
            return true;
        }

        player.SendHint($"<color=#ffb347>能量不足：需要 {amount:0}，当前 {state.Energy:0}。</color>", 2f);
        return false;
    }

    private void BeginCooldown(WardenState state) => state.NextAbilityAt = Time.time + Mathf.Max(1f, Config.AbilityCooldown - (state.Level - 1));

    private static TimeSnapshot? FindSnapshot(WardenState state, float seconds)
    {
        TimeSnapshot? result = null;
        float targetTime = Time.time - seconds;
        foreach (TimeSnapshot snapshot in state.History)
        {
            result = snapshot;
            if (snapshot.Timestamp >= targetTime)
                break;
        }
        return result;
    }

    private static string AbilityName(WardenAbility ability) => ability switch
    {
        WardenAbility.PhaseShield => "相位护盾",
        WardenAbility.TemporalPulse => "时滞脉冲",
        WardenAbility.Rewind => "时间回溯",
        _ => "未知",
    };
}
