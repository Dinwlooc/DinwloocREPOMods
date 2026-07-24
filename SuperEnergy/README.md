# SuperEnergy

**为游戏提供全面的能量管理功能：自动充电、玩家自愈、死亡复活、体力加速。**  
所有功能均可独立开关，并支持多种能量来源（免费或消耗卡车电量/医疗包）。

---

## 前置依赖

- **[BepInEx](https://github.com/BepInEx/BepInEx/releases)** – 模组加载框架（≥ 5.4.2100）
- **[MenuLib](https://thunderstore.io/c/repo/p/nickklmao/MenuLib/)** – 菜单界面扩展（≥ 1.1.0）  
  *注：公共依赖库已包含对 MenuLib 的依赖。*
- **[REPO_Dinwlooc_Lib](../REPO_Dinwlooc_Lib)** – 公共依赖库（随本模组一起分发）

---

## 安装方法

1. 下载 `SuperEnergy.zip` 并解压。  
2. 将 `SuperEnergy.dll` 放入 `BepInEx/plugins` 目录。  
3. 确保 `REPO_Dinwlooc_Lib` 已安装在 `plugins` 目录。

---

## 功能说明
- 所有功能都只在关卡场景中生效。
### ① 手持物品自动充电
- **效果**：玩家手持带有电池的物品（如手电筒、武器）时，自动为其充电。
- **来源选项**：
  - **Free**：免费充电，无任何消耗。
  - **Truck**：消耗卡车电量。
- **可配置参数**：充电间隔（秒）、每次充电量（百分比）。

### ② 玩家自愈
- **效果**：玩家生命值低于最大生命时，自动恢复生命。
- **来源选项**：
  - **Free**：免费恢复，无任何消耗。
  - **HealthPack**：消耗附近的医疗包，总恢复量与医疗包剩余量相关。
	- > **注意**：该功能未经充分测试。它会不断减少医疗包内置的治疗量，并在治疗量为零时，复现其“被使用”时的一系列原版效果。
- **可配置参数**：恢复间隔（秒）、每次恢复量（HP）。

### ③ 死亡头部复活
- **效果**：玩家死亡后，激活自己的死亡头部（Spectated）时，将累积时间。达到设定值后自动复活。
- **可配置参数**：复活所需时间（秒），设为 **0** 则立即复活（无延迟）。

### ④ 体力加速恢复
- **效果**：体力值（Energy）恢复速度加倍（仅对本地玩家有效）。
- **可配置参数**：恢复倍率（1 ~ 10 倍）。
- > **注意**：该能力会让玩家的体力无条件持续恢复，不会因冲刺而暂停回复。

---

## 配置项

配置文件 `BepInEx/config/Dinwlooc.SuperEnergy.cfg` 会在首次运行后生成，可用文本编辑器或 REPOConfig 调整。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| **Enabled** | bool | true | 模组总开关 |
| **ItemCharging / Enable** | bool | true | 启用物品自动充电 |
| **ItemCharging / Source** | enum | Free | 充电来源（Free / Truck） |
| **ItemCharging / Interval** | int (1-60) | 2 | 充电检测间隔（秒） |
| **ItemCharging / Amount** | int (1-100) | 5 | 每次充电量（%） |
| **PlayerHeal / Enable** | bool | true | 启用玩家自愈 |
| **PlayerHeal / Source** | enum | Free | 自愈来源（Free / HealthPack） |
| **PlayerHeal / Interval** | int (1-60) | 2 | 自愈检测间隔（秒） |
| **PlayerHeal / Amount** | int (1-100) | 5 | 每次恢复量（HP） |
| **DeathHeadRevive / Enable** | bool | true | 启用死亡复活 |
| **DeathHeadRevive / RequiredTime** | int (0-300) | 30 | 复活所需时间（秒），0=立即复活 |
| **StaminaBoost / Enable** | bool | true | 启用体力加速恢复 |
| **StaminaBoost / Multiplier** | int (1-10) | 2 | 恢复倍率 |

---

## 使用提示

- 所有功能默认启用，您可以根据喜好单独关闭任意功能。
- **Truck 模式**下请确保卡车有足够电量，否则充电会因电量不足而跳过。
- 体力加速恢复仅影响**本地玩家**，多人模式下各自独立。

---

## 兼容性

- 所有功能均通过游戏原生 API 或公共库桥接实现，不修改游戏核心代码。
- 多人模式下，充电和自愈操作由房主执行并同步。

---

## 许可

本项目采用 **MIT 许可证**，详见 [LICENSE](LICENSE) 文件。