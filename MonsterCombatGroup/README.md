# MonsterHighlight

**当怪物进入玩家视野时自动高亮显示**，便于识别和跟踪。

> **模组类型**：**纯客户端**（仅影响本地显示，无需房主参与，无需网络同步）。

---

## 前置依赖

- **[BepInEx](https://github.com/BepInEx/BepInEx/releases)** – 模组加载框架（≥ 5.4.2100）
- **[MenuLib](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/)** – 菜单界面扩展（≥ 1.1.0）  
  *注：本模组本身不直接使用 MenuLib，但公共依赖库需要，因此必须安装。*
- **[REPO_Dinwlooc_Lib](../REPO_Dinwlooc_Lib)** – 公共依赖库（随本模组一起分发）

---

## 安装方法

1. 下载 `MonsterHighlight.zip` 并解压。  
2. 将 `MonsterHighlight.dll` 放入 `BepInEx/plugins` 目录（**每个玩家可单独安装，不影响他人**）。  
3. 确保 `REPO_Dinwlooc_Lib` 已安装在 `plugins` 目录。

---

## 使用方法

- 进入任意关卡（非主菜单），模组自动生效。  
- 当**本地玩家**视野中出现怪物时，该怪物身体会发出彩色光晕（材质自发光）。  
- 当怪物离开视野后，光晕消失（延迟约为视野检测间隔）。  
- 每个客户端独立检测自己的视野，互不干扰。

---

## 配置项

配置文件 `BepInEx/config/Dinwlooc.MonsterHighlight.cfg` 会在首次运行后生成，可用文本编辑器或 REPOConfig 调整。

- **Enabled**（布尔，默认 `true`）：是否启用该模组（总开关）。  
- **HighlightPreset**（枚举，默认 `Cyan`）：预设高亮颜色，可选值：`Cyan`、`Blue`、`Red`、`Green`、`Yellow`、`Orange`、`Pink`、`Purple`、`White`。  
- **EnableEmission**（布尔，默认 `true`）：是否启用材质自发光效果。  
- **EnableIndicator**（布尔，默认 `true`）：是否启用指示器（一个方框，罩住不可见的怪物，隔墙可见）。  
- **CheckIntervalMs**（整数，默认 `1000`，范围 50~5000）：视野检测间隔（毫秒），值越小响应越快，但性能消耗更高。  
- **IndicatorUpdateStep**（整数，默认 `5`，范围 1~60）：指示器更新步长（帧数），每 N 帧更新一次指示器位置。  
- **IndicatorSize**（浮点数，默认 `2.5`，范围 0.01~10）：指示器基础尺寸缩放。  
- **MinDistance**（整数，默认 `0`，范围 0~200）：指示器开始缩小的距离（米）。  
- **MaxDistance**（整数，默认 `50`，范围 1~200）：指示器达到最小尺寸的距离（米）。  
- **MinSizeRatio**（浮点数，默认 `0.02`，范围 0.01~1）：指示器最小尺寸比例。  
- **IndicatorAlpha**（浮点数，默认 `0.3`，范围 0~1）：指示器透明度。

---

## 工作原理

- 每个客户端独立检测本地玩家视野内的怪物（基于游戏内置的 `SemiFunc.PlayerVisionCheck`）。  
- 检测到怪物时，本地应用高亮效果（材质自发光）。  
- 不同客户端高亮结果独立，互不影响。  
- 检测间隔可调，默认 1 秒，兼顾性能与响应速度。

> **多人模式**：每个玩家只能看到自己视野内的高亮，无需网络同步，简单可靠。

> **可能问题**：当目睹怪物死亡，而后怪物复活时，他们的部分纹理可能呈现为高亮颜色的反色。

---

## 许可

本项目采用 **MIT 许可证**，详见 [LICENSE](LICENSE) 文件。