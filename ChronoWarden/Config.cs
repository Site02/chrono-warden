using System.ComponentModel;

namespace ChronoWarden;

public sealed class Config
{
    [Description("是否启用插件。")]
    public bool IsEnabled { get; set; } = true;

    [Description("每局最多生成的时序守望者数量。")]
    public int MaxWardens { get; set; } = 1;

    [Description("每名 D 级人员被选中的基础概率（0-100）。")]
    public float SpawnChance { get; set; } = 35f;

    [Description("角色基础最大生命值。")]
    public float MaxHealth { get; set; } = 135f;

    [Description("能量每秒恢复量。")]
    public float EnergyPerSecond { get; set; } = 2.5f;

    [Description("击杀奖励能量。")]
    public float KillEnergy { get; set; } = 30f;

    [Description("每次升级需要的击杀数。")]
    public int KillsPerLevel { get; set; } = 2;

    [Description("相位护盾消耗。")]
    public float ShieldCost { get; set; } = 35f;

    [Description("相位护盾提供的 AHP。")]
    public float ShieldAmount { get; set; } = 60f;

    [Description("时滞脉冲消耗。")]
    public float PulseCost { get; set; } = 50f;

    [Description("时滞脉冲半径。")]
    public float PulseRadius { get; set; } = 8f;

    [Description("脉冲对敌人造成的伤害。")]
    public float PulseDamage { get; set; } = 35f;

    [Description("脉冲为人类盟友恢复的生命。")]
    public float PulseHealing { get; set; } = 25f;

    [Description("时间回溯消耗。")]
    public float RewindCost { get; set; } = 75f;

    [Description("回溯使用多少秒前的状态。")]
    public float RewindSeconds { get; set; } = 8f;

    [Description("主动技能公共冷却秒数。")]
    public float AbilityCooldown { get; set; } = 8f;

    [Description("满级后可触发一次濒死回溯。")]
    public bool EnableLastChance { get; set; } = true;

    [Description("出生说明广播持续秒数。")]
    public ushort IntroDuration { get; set; } = 12;

    [Description("是否输出调试日志。")]
    public bool Debug { get; set; } = false;
}
