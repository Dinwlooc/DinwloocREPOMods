# UpgradeUninstaller

**将全体玩家的升级（包括生命、体力等）统一卸载，并返还等量升级箱。**  
仅限房主在卡车（运输途中）使用，点击 ESC 菜单中的“卸载升级”按钮即可。

---

## 前置依赖（必须安装）

- **[REPOLib](https://github.com/ZehsTeam/REPOLib)** – 提供自定义升级支持（版本 ≥ 4.2.0）  
  下载地址：[https://thunderstore.io/c/repo/p/Zehs/REPOLib/](https://thunderstore.io/c/repo/p/Zehs/REPOLib/)
- **[MenuLib](https://github.com/IsThatTheRealNick/MenuLib)** – 提供菜单界面扩展（版本 ≥ 1.1.0）  
  下载地址：[https://thunderstore.io/c/repo/p/nickklmao/MenuLib/](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/)
请将这两个库放在 `BepInEx/plugins` 目录下。

---

## 安装方法

1. 下载 `UpgradeUninstaller.zip` 并解压。
2. 将 `UpgradeUninstaller` 文件夹整体放入 `BepInEx/plugins` 目录。
3. 启动游戏，确保前置依赖已加载。本模组在 **REPOLib v4.2.0** 和 **MenuLib v1.1.0** 环境下测试通过。

---

## 使用方法

- 在游戏中按 **ESC** 打开菜单。
- 在菜单中找到 **“卸载升级”** 按钮（仅当你在卡车中且为房主时可用）。
- 点击后，**所有玩家的所有升级** 将被清零，并按照以下规则返还升级箱：
  - **生命升级**：每级扣除 20 HP，最多保留 1 HP，返还等数量生命升级箱。
  - **其他升级**：全部扣除，返还等数量对应升级箱。
- 完成后游戏会自动重载当前场景，使变更生效。

> **注意**：此操作对当前不在游戏中的玩家（离线玩家）的升级同样生效，只要其steamID是有效值。因此可以此操作取回离线玩家使用过的升级。

---

## 兼容性

- 支持原版大多数升级，以及通过 **REPOLib** 注册的自定义升级。
- 多人联机时需房主执行，会同步影响所有玩家。
- 只在可以生成升级箱时移除等级。

---

## 关于存档备份文件

游戏会在每次保存时自动生成带时间戳和递增编号的备份文件（例如 `REPO_SAVE_2026_07_05_21_23_12_BACKUP1.es3`）。  
生成升级盒时，本模组会立即存档，使用升级盒时，游戏会自动存档。因此，使用该模组通常会让备份文件数量高于预期。
这些备份会随着游玩时间不断累积，你可以定期手动删除 `%USERPROFILE%/AppData/LocalLow/REPO/saves/` 下的所有 `*_BACKUP*.es3` 文件（请勿删除不带 `_BACKUP` 的主存档）。  
本模组**不会**自动删除任何备份文件。

---

## 许可

本项目采用 **MIT 许可证**，详见 [LICENSE](LICENSE) 文件。