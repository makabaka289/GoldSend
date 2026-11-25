# GoldSend · 金币往来

金币往来（GoldLend）是《杀戮尖塔 2》的联机 Mod。进入商店后，点击顶栏的金币图标，即可打开交易面板，给队友转账或向队友索要金币。

- 面板显示队友及其当前金币，可通过预设按钮或键盘修饰键调整金额。
- 队友可以同意或拒绝金币请求。余额不足时，可以转出当前持有的全部金币。
- 多条待处理请求按顺序显示。转账到账或请求处理后，游戏会显示通知。
- 支持简体中文和英文，可在设置中调整角色配色和玩家编号显示。

## 安装

在 Steam 创意工坊订阅 [金币往来](https://steamcommunity.com/sharedfiles/filedetails/?id=3799498581) 和 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)。联机时请确保队友使用相同的 Mod 版本。

GoldLend `0.2.9` 面向游戏 `0.114.0`，需要 RitsuLib `0.6.16` 及以上版本。构建默认使用 RitsuLib 的 `0.114.0` 兼容目录。使用游戏 `0.113.1` 时请用 GoldLend `0.2.8`。

## 从源码构建

在 Windows 上安装 .NET 9 SDK，用 PowerShell 在仓库根目录运行以下命令。将路径替换成实际目录。RitsuLib 变体包目录内应有 `RitsuLib.References.props` 和 `compat/0.114.0/`。

```powershell
./build.ps1 -Configuration Release `
    -Sts2GameDir '<游戏安装目录>' `
    -RitsuLibRoot '<RitsuLib 变体包目录>'
```

也可以用环境变量 `STS2_GAME_DIR` 和 `RITSULIB_ROOT` 指定这两个目录。构建后可在 `.godot/mono/temp/bin/Release/` 找到 `GoldLend.dll` 和 `GoldLend.json`。

公开源码不包含到账音频。使用公开源码构建时，到账通知仍会显示，声音请保持为「关闭」。
