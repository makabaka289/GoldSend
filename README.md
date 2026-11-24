# GoldSend · 金币往来

金币往来（GoldLend）是《杀戮尖塔 2》的联机 Mod。进入商店后，点击顶栏的金币图标，即可打开交易面板，给队友转账或向队友索要金币。

- 面板显示队友及其当前金币，可通过预设按钮或键盘修饰键调整金额。
- 队友可以同意或拒绝金币请求。余额不足时，可以转出当前持有的全部金币。
- 多条待处理请求按顺序显示。转账到账或请求处理后，游戏会显示通知。
- 支持简体中文和英文，可在设置中调整角色配色和玩家编号显示。

## 安装

在 Steam 创意工坊订阅 [金币往来](https://steamcommunity.com/sharedfiles/filedetails/?id=3799498581) 和 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)。联机时请确保队友使用相同的 Mod 版本。

项目清单要求游戏 `0.113.1` 及以上、RitsuLib `0.6.15` 及以上。构建默认使用 RitsuLib 的 `0.113.0` API 兼容目录。

## 从源码构建

在 Windows 上安装 .NET 9 SDK，用 PowerShell 在仓库根目录运行以下命令。将路径替换成实际目录。RitsuLib 目录内应有 `RitsuLib.References.props`。

```powershell
./build.ps1 -Configuration Release `
    -Sts2GameDir '<游戏安装目录>' `
    -RitsuLibRoot '<RitsuLib 创意工坊目录>'
```

也可以用环境变量 `STS2_GAME_DIR` 和 `RITSULIB_ROOT` 指定这两个目录。构建后可在 `.godot/mono/temp/bin/Release/` 找到 `GoldLend.dll` 和 `GoldLend.json`。

公开源码不包含到账音频。使用公开源码构建时，到账通知仍会显示，声音请保持为「关闭」。
