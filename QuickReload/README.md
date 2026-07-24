# QuickReload

**不保存进度，直接重新加载关卡、卡车或返回商店。**  
仅限房主或单人模式在非主菜单场景使用，点击 ESC 菜单中的“快速重载”或“返回商店”按钮即可。

---

## 前置依赖

- **[BepInEx](https://github.com/BepInEx/BepInEx/releases)** – 模组加载框架（≥ 5.4.2100）
- **[MenuLib](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/)** – 菜单界面扩展（≥ 1.1.0）  
  *注：公共依赖库已包含对 MenuLib 的依赖。*
- **[REPO_Dinwlooc_Lib](../REPO_Dinwlooc_Lib)** – 公共依赖库（随本模组一起分发）

---

## 安装方法

1. 下载 `QuickReload.zip` 并解压。  
2. 将 `QuickReload.dll` 放入 `BepInEx/plugins` 目录。  
3. 确保 `REPO_Dinwlooc_Lib` 已安装在 `plugins` 目录。

---

## 使用方法

- 在游戏中按 **ESC** 打开菜单（**不能**在主菜单界面）。  
- 在菜单中找到 **“快速重载”** 或 **“返回商店”** 按钮（仅当你是房主或在单人模式时可用）。  
- 点击后，**切换至目标场景**。  
- 多人模式下，房主点击后所有玩家将同步重载场景。

> **注意 1**：如果启用了随机化，客户端在加载过场动画内不会看到新场景的入场动画，而是播放最初场景的入场动画。

> **注意 2**：如果你在关卡中，此操作会**立即读取最近的存档**，恢复生命值与物品电量。它会保留在关卡内提取到的装饰品代币，但会把升级盒的使用记录重置到关卡开头。

> **注意 3**：若不在关卡中（如在商店或卡车），则**立即存档**。这不会影响在商店内丢失的生命值，但会保留购买和升级盒的使用记录。

---

## 配置项

配置文件 `BepInEx/config/Dinwlooc.QuickReload.cfg` 会在首次运行后生成，可用文本编辑器或 REPOConfig 调整。

- **Enabled**（布尔，默认 `true`）：是否启用该模组（总开关）。  
- **ReloadRandomScene**（布尔，默认 `false`）：启用时，在关卡/商店中重载会随机切换到同类型场景，可以用来刷关卡。  
- **ReloadButtonEnabled**（布尔，默认 `true`）：是否显示“快速重载”按钮。  
- **ReloadButtonPosX**（整数，默认 `176`）：按钮的 X 偏移。  
- **ReloadButtonPosY**（整数，默认 `125`）：按钮的 Y 偏移。  
- **ShopButtonEnabled**（布尔，默认 `true`）：是否显示“返回商店”按钮。  
- **ShopButtonPosX**（整数，默认 `176`）：按钮的 X 偏移。  
- **ShopButtonPosY**（整数，默认 `85`）：按钮的 Y 偏移。

---

## 关于存档备份文件

游戏会在每次保存时自动生成带时间戳和递增编号的备份文件。  
本模组会主动存档，因此备份文件数量可能增多。请定期清理 `%USERPROFILE%/AppData/LocalLow/REPO/saves/` 下的 `*_BACKUP*.es3` 文件（保留主存档即可）。

---

## 许可

本项目采用 **MIT 许可证**，详见 [LICENSE](LICENSE) 文件。