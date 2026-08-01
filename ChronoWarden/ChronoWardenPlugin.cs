using System;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using MEC;

namespace ChronoWarden;

public sealed class ChronoWardenPlugin : Plugin<Config>
{
    public static ChronoWardenPlugin? Instance { get; private set; }

    public override string Name => "Chrono Warden";
    public override string Description => "带能量、三技能、成长与濒死回溯的高深度特殊角色。";
    public override string Author => "CodeBuddy";
    public override Version Version => new(1, 0, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    internal WardenManager Manager { get; private set; } = null!;

    public override void Enable()
    {
        Instance = this;
        Manager = new WardenManager(this);

        ServerEvents.RoundStarted += Manager.OnRoundStarted;
        ServerEvents.RoundEnded += Manager.OnRoundEnded;
        PlayerEvents.ChangedRole += Manager.OnChangedRole;
        PlayerEvents.Death += Manager.OnDeath;
        PlayerEvents.DroppingItem += Manager.OnDroppingItem;
        PlayerEvents.TogglingNoclip += Manager.OnTogglingNoclip;

        Manager.StartLoop();
        Logger.Info($"{Name} v{Version} 已启用。输入 cw reload 可热重载配置。");
    }

    public override void Disable()
    {
        ServerEvents.RoundStarted -= Manager.OnRoundStarted;
        ServerEvents.RoundEnded -= Manager.OnRoundEnded;
        PlayerEvents.ChangedRole -= Manager.OnChangedRole;
        PlayerEvents.Death -= Manager.OnDeath;
        PlayerEvents.DroppingItem -= Manager.OnDroppingItem;
        PlayerEvents.TogglingNoclip -= Manager.OnTogglingNoclip;

        Manager.StopLoop();
        Manager.ResetRound(false);
        Instance = null;
    }

    public bool Reload(out string message)
    {
        try
        {
            LoadConfigs();
            Manager.ApplyReloadedConfig();
            message = $"配置已重载：最大角色 {Config.MaxWardens}，生成率 {Config.SpawnChance:0.#}%。";
            Logger.Info(message);
            return true;
        }
        catch (Exception exception)
        {
            message = $"配置重载失败：{exception.Message}";
            Logger.Error(exception);
            return false;
        }
    }
}
