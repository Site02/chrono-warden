using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Commands;
using RemoteAdmin;

namespace ChronoWarden.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ChronoWardenCommand : ICommand, IUsageProvider
{
    public string Command => "chronowarden";
    public string[] Aliases => new[] { "cw" };
    public string Description => "管理时序守望者：reload / refresh / give / remove / list。";
    public string[] Usage => new[] { "reload|refresh|give|remove|list", "[玩家ID]" };

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.PlayersManagement))
        {
            response = "权限不足，需要 Players Management。";
            return false;
        }

        ChronoWardenPlugin? plugin = ChronoWardenPlugin.Instance;
        if (plugin is null)
        {
            response = "Chrono Warden 当前未启用。";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = $"用法：{this.DisplayCommandUsage()}";
            return false;
        }

        switch (arguments.At(0).ToLowerInvariant())
        {
            case "reload":
                return plugin.Reload(out response);
            case "refresh":
                plugin.Manager.ResetRound(true);
                response = "已清空角色运行状态，并按当前配置重新抽取本局角色。";
                return true;
            case "list":
                response = plugin.Manager.GetStatus();
                return true;
            case "give":
                if (!TryGetPlayer(arguments, out Player? giveTarget, out response))
                    return false;
                return plugin.Manager.Assign(giveTarget!, out response);
            case "remove":
                if (!TryGetPlayer(arguments, out Player? removeTarget, out response))
                    return false;
                return plugin.Manager.Remove(removeTarget!, out response);
            default:
                response = $"未知子命令。用法：{this.DisplayCommandUsage()}";
                return false;
        }
    }

    private static bool TryGetPlayer(ArraySegment<string> arguments, out Player? player, out string response)
    {
        player = null;
        if (arguments.Count < 2 || !int.TryParse(arguments.At(1), out int playerId))
        {
            response = "请提供有效的玩家 ID。";
            return false;
        }

        player = Player.Get(playerId);
        if (player is null)
        {
            response = $"找不到玩家 ID {playerId}。";
            return false;
        }

        response = string.Empty;
        return true;
    }
}
