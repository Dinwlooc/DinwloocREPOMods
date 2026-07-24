# Dinwlooc REPO Mods 合集

本仓库汇集了一系列为游戏 **REPO** 开发的模组，均为个人闲暇时编写。  
所有模组均基于公共依赖库 **REPO_Dinwlooc_Lib** 构建，该库提供统一的游戏桥接、事件总线、UI 辅助和配置基类。

---

## 公共依赖库（REPO_Dinwlooc_Lib）

所有模组均依赖此库，它本身不提供面向玩家的功能，而是为模组开发者提供基础设施：

- **原子化游戏桥接** (`Dinwlooc.Common.Bridge`)  
  通过 `BridgeLocator` 获取玩家、物品、卡车、升级、网络等接口，自动适配 REPOLib（若存在）增强功能。

- **事件总线** (`Dinwlooc.Common.Core.EventBus`)  
  轻量级发布‑订阅机制，支持模组间松耦合通信（如玩家复活、场景重载等事件）。

- **公共服务宿主** (`CommonService`)  
  统一管理 MonoBehaviour 生命周期回调（Update、FixedUpdate、协程），避免各模组自行挂载组件。

- **UI 辅助** (`MenuHelper`)  
  封装 MenuLib 按钮创建，自动读取配置中的位置和开关。

- **配置基类** (`ConfigBase<T>` / `MenuConfigBase<T>`)  
  简化模组配置管理，自动绑定开关和位置。

该库对游戏本身“零侵入”，仅提供 API 封装，并自动处理主机权限检查。

---

### 通用安装步骤

1. 从本仓库 Releases 下载所需模组的压缩包（或克隆仓库自行编译）。  
2. 确保已安装以下 **必须的前置依赖**：
   - [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases)（模组加载框架）
   - [MenuLib](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/)（≥ 1.1.0，用于菜单按钮）
   - [REPO_Dinwlooc_Lib](./REPO_Dinwlooc_Lib)（即本公共库，包含在所有模组压缩包中）
3. 将对应的 `.dll` 文件或文件夹放入 `BepInEx/plugins` 目录。  
4. 启动游戏，模组自动加载。部分模组（如 UpgradeUninstaller）还需 **REPOLib**（可选，但建议安装）。

> 配置文件（`.cfg`）会在首次运行后自动生成于 `BepInEx/config/` 下，可用文本编辑器或 REPOConfig 修改。

---

## 兼容性与注意事项

- 所有模组均未修改游戏原生代码，仅通过 API 交互，兼容性良好。
- 多人游戏中，权限敏感的操作为房主限制（QuickReload、UpgradeUninstaller），非房主按钮不可见或点击无效。
- 存档备份文件会随操作增加，请定期清理 `%USERPROFILE%/AppData/LocalLow/REPO/saves/` 下的 `*_BACKUP*.es3` 文件，保留主存档即可。
- 公共库中的 `IUpgradeBridge` 会自动检测 REPOLib 是否存在，若不存在则回退至原生升级映射，因此即使不安装 REPOLib，升级相关功能（如 UpgradeUninstaller）仍会尝试工作（但可能无法识别自定义升级）。

---

## 开发与贡献

若您希望基于公共库开发自己的模组，请在项目中引用 `REPO_Dinwlooc_Lib.dll`，并添加 `[BepInDependency("Dinwlooc.Common")]` 特性。详细用法请参阅公共库的 README 或源代码注释。

欢迎提交 Issue 或 Pull Request，但请注意本项目为个人兴趣维护，响应可能较慢。

---

## 许可证

所有模组及公共库均采用 **MIT 许可证**，详见各子目录下的 LICENSE 文件。您可自由使用、修改、分发，但需保留版权声明。

---

**祝游戏愉快！**  
—— Dinwlooc