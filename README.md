# Dinwlooc REPO Mods 合集

本仓库汇集了一系列为游戏 **REPO** 开发的模组，均为个人闲暇时编写。  
所有模组均基于公共依赖库 **REPO_Dinwlooc_Lib** 构建，该库提供统一的游戏桥接、事件总线、缓存管理、同步管理、UI 辅助和配置基类。

---

## 公共依赖库（REPO_Dinwlooc_Lib）

所有模组均依赖此库，它本身不提供面向玩家的功能，而是为模组开发者提供基础设施。

详见其内部的README.md

---

### 通用安装步骤

1. 从本仓库 Releases 下载所需模组的压缩包。  
2. 需要安装以下 **必须的前置依赖**：
   - [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases)（模组加载框架）
   - [MenuLib](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/)（≥ 1.1.0，用于菜单按钮）
	-[REPOLib]( https://thunderstore.io/c/repo/p/Zehs/REPOLib/ )
   - [REPO_Dinwlooc_Lib](./REPO_Dinwlooc_Lib)
3. 将对应的 `.dll` 文件或文件夹放入 `BepInEx/plugins` 目录。  

> 配置文件（`.cfg`）会在首次运行后自动生成于 `BepInEx/config/` 下，可用文本编辑器或 REPOConfig 修改。

---

## 兼容性与注意事项

- 所有模组均不通过补丁来修改游戏原生代码，仅通过 API 交互，兼容性良好。
- 多人游戏中，权限敏感的操作为房主限制。
- 大量采用懒启动设计，理论上只在模块实际运行时出现冲突。

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