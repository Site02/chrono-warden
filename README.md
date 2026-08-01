# Chrono Warden（时序守望者）

一个基于 **LabAPI 1.1.7** 开发的 SCP: Secret Laboratory 高可玩性特殊角色插件。

时序守望者会从 D 级人员中随机产生，拥有独立的能量系统、三种主动技能、击杀成长和满级濒死回溯机制。插件支持通过 Remote Admin 管理面板热重载配置和刷新本局角色，无需重启服务器。

## 兼容信息

| 项目 | 版本 |
| --- | --- |
| 插件版本 | 1.0.0 |
| LabAPI | 1.1.7 |
| 目标框架 | .NET Framework 4.8 |
| 测试服务端 | SCP:SL Dedicated Server（本地服务端文件） |

> SCP:SL 或 LabAPI 更新后可能出现 API 不兼容。升级服务器前建议备份插件与配置。

## 角色玩法

### 基础属性

- 回合开始后，从 D 级人员中按配置概率随机抽取。
- 默认每局最多生成 1 名。
- 初始最大生命值为 135 HP，初始能量为 50。
- 能量会自动恢复，击杀敌人可额外获得能量。
- 每完成 2 次击杀提升一级，最高 3 级。
- 升级会提高最大生命、能量恢复效率和技能效果。

### 操作方式

1. 按 `Alt` 切换当前技能。
2. 也可在客户端控制台输入 `.cwcycle` 切换技能。
3. 丢出角色自带的硬币，施放当前技能。
4. 屏幕右侧 Hint 会显示等级、能量、击杀数和当前技能。

### 主动技能

#### 相位护盾

消耗能量获得临时 AHP。升级后消耗降低，护盾量提高，适合突围或吸收 SCP 的爆发伤害。

#### 时滞脉冲

释放范围脉冲：

- 治疗范围内的人类玩家。
- 伤害范围内的 SCP。
- 升级后治疗量和伤害提高。

#### 时间回溯

返回约 8 秒前的位置和生命值，可用于撤销错误走位、逃离追击或恢复战斗状态。

### 满级被动：拒绝死亡

达到 3 级并拥有 100 能量后，首次死亡会自动回到过去的状态。本局仅能触发一次，触发后清空能量。

## Remote Admin 管理命令

命令需要 `Players Management` 权限。

| 命令 | 作用 |
| --- | --- |
| `cw reload` | 热重载配置，不重启服务器 |
| `cw refresh` | 清空角色运行状态，并按当前配置重新抽取 |
| `cw give <玩家ID>` | 将指定 D 级人员设为时序守望者 |
| `cw remove <玩家ID>` | 移除指定玩家的特殊角色 |
| `cw list` | 查看当前角色、等级、能量、击杀和技能 |

完整命令名 `chronowarden` 与别名 `cw` 均可使用。

## 安装

1. 确保服务器已正确安装并启用 LabAPI 1.1.7。
2. 从 Gitee Release 下载最新的 `ChronoWarden-v1.0.0.zip`。
3. 解压并将 `ChronoWarden.dll` 放入 LabAPI 插件目录：
   - 全局插件：`%AppData%\SCP Secret Laboratory\LabAPI\plugins\global`
   - 单端口插件：`%AppData%\SCP Secret Laboratory\LabAPI\plugins\<服务器端口>`
4. 如果服务器环境缺少 `YamlDotNet.dll`，将发布包中的该文件放入 LabAPI 依赖目录；通常 LabAPI 已自带此依赖。
5. 启动服务器。首次加载后 LabAPI 会生成插件配置文件。
6. 在服务端日志中确认出现 `Chrono Warden v1.0.0 已启用`。

## 配置

配置文件支持以下主要选项：

- 插件启用状态、每局角色上限和生成概率。
- 角色最大生命值和能量恢复速度。
- 击杀奖励、升级所需击杀数。
- 三种技能的消耗、强度、范围、回溯时间与冷却。
- 满级濒死回溯开关。
- 出生广播时间与调试日志开关。

修改配置后，在 Remote Admin 执行 `cw reload` 即可立即应用。执行 `cw refresh` 可按新配置重新抽取本局角色。

## 从源码构建

需要安装 .NET SDK，并准备 SCP:SL Dedicated Server 的托管程序集。

```powershell
$env:SL_REFERENCES = "C:\Program Files (x86)\Steam\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed"
$env:UNITY_REFERENCES = $env:SL_REFERENCES
dotnet restore .\ChronoWarden\ChronoWarden.csproj
dotnet build .\ChronoWarden\ChronoWarden.csproj -c Release
```

编译结果位于：

```text
ChronoWarden\bin\Release\net48\ChronoWarden.dll
```

## 常见问题

### 丢硬币没有施放技能

确认玩家确实是时序守望者、丢出的是角色硬币、能量足够且技能不在冷却中。

### `cw` 命令提示权限不足

为管理员角色添加 `Players Management` 权限。

### 修改配置后没有重新抽取角色

`cw reload` 只重新读取配置并更新现有角色数值；需要重新抽取时再执行 `cw refresh`。

### 更新服务器后插件无法加载

检查服务器 LabAPI 版本是否仍为 1.1.7 兼容版本，并查看服务端启动日志中的程序集或 API 错误。

## 源码结构

- `ChronoWardenPlugin.cs`：插件入口、生命周期和热重载。
- `WardenManager.cs`：角色生成、能量循环、技能与成长逻辑。
- `WardenState.cs`：玩家运行状态和时间快照。
- `Config.cs`：可配置参数。
- `Commands/`：RA 管理命令和玩家技能切换命令。

