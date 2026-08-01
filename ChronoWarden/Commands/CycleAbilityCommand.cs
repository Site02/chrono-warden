using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace ChronoWarden.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class CycleAbilityCommand : ICommand
{
    public string Command => "cwcycle";
    public string[] Aliases => new[] { "时序切换" };
    public string Description => "时序守望者切换当前主动技能。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        ChronoWardenPlugin? plugin = ChronoWardenPlugin.Instance;
        if (player is null || plugin is null)
        {
            response = "当前无法使用此命令。";
            return false;
        }

        plugin.Manager.CycleAbility(player);
        response = "技能已切换。";
        return true;
    }
}
