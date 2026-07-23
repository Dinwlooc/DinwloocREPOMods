# MonsterHighlight

**当怪物进入玩家视野时自动高亮显示**，便于识别和跟踪。

> **模组类型**：**纯客户端**（仅影响本地显示，无需房主参与，无需安装同步）。

---

## 前置依赖

- **[BepInEx](https://github.com/BepInEx/BepInEx/releases)** – 模组加载框架（版本 ≥ 5.4.2100）

---

## 安装方法

1. 下载 `MonsterHighlight.zip` 并解压。
2. 将 `MonsterHighlight.dll` 放入 `BepInEx/plugins` 目录（**每个玩家可单独安装，不影响他人**）。

---

## 配置项

模组配置文件位于 `BepInEx/config/Dinwlooc.MonsterHighlight.cfg`，可用文本编辑器或 **[REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/)** 调整。

- **EnableMod**（布尔，默认 `true`）：  
  是否启用怪物高亮模组。设为 `false` 可完全禁用，无需卸载插件。

- **HighlightPreset**（枚举，默认 `Cyan`）：  
  预设高亮颜色，可选值：`Cyan`、`Blue`、`Red`、`Green`、`Yellow`、`Orange`、`Pink`、`Purple`、`White`。

- **CheckIntervalMs**（整数，默认 `1000`，范围 50~5000）：  
  视野检测间隔（毫秒）。值越小反应越快，但性能消耗也更高。

- **EnableLight**（布尔，默认 `true`）：  
  是否在怪物身上添加点光源辅助高亮。关闭后仅使用材质自发光。

---

## 工作原理

- **每个客户端独立检测本地玩家视野内的怪物**（基于游戏内置的 `SemiFunc.PlayerVisionCheck`）。
- 检测到怪物时，**本地**应用高亮效果（材质自发光 + 可选点光源）。
- 不同客户端高亮结果独立，互不影响。
- 检测间隔可调，默认 1 秒，兼顾性能与响应速度。

> **多人模式**：每个玩家只能看到自己视野内的高亮，无需网络同步，简单可靠。

---

## 许可

本项目采用 **MIT 许可证**，详见 [LICENSE](LICENSE) 文件。