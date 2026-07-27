# REPO_Dinwlooc_Lib

Dinwlooc 模组公共依赖库，为 Dinwlooc 系列模组提供统一的游戏桥接、事件总线、缓存中心、同步缓存、UI 辅助和配置基类，简化开发并增强模组间的互操作性。

---

## 前置依赖（必须安装）

- **BepInEx 5.x** – 插件加载框架。
- **MenuLib** – 提供菜单界面扩展（版本 ≥ 1.1.0）。  
  下载地址：https://thunderstore.io/c/repo/p/nickklmao/MenuLib/
- **REPOLib**（可选，但建议安装） – 提供自定义物品与升级系统支持（版本 ≥ 4.2.0）。  
  下载地址：https://thunderstore.io/c/repo/p/Zehs/REPOLib/  
  若未安装，升级相关桥接将回退至原生支持。

请将所有依赖库放在 `BepInEx/plugins` 目录下。

---

## 安装方法

1. 下载 `REPO_Dinwlooc_Lib.zip` 并解压。  
2. 将 `REPO_Dinwlooc_Lib` 文件夹整体放入 `BepInEx/plugins` 目录。  
3. 启动游戏，确保前置依赖已加载。本库在 REPOLib v4.2.0 和 MenuLib v1.1.0 环境下测试通过。

---

## 功能概览

本库本身不提供任何直接面向用户的游戏功能，而是为其他模组提供以下基础设施：

### 1. 原子化游戏桥接（命名空间 `Dinwlooc.Common.Bridge`）

将游戏原生 API 拆分为多个职责单一的接口，模组可按需引用：

- `IPlayerBridge`      – 玩家获取、血量读写、治愈  
- `IItemBridge`        – 手持物品电池操作  
- `IHealthPackBridge`  – 医疗包查找与消耗  
- `ITruckBridge`       – 卡车电量查询与消耗  
- `ISaveLoadBridge`    – 存档读写、场景重载、跳转商店  
- `IGameStateBridge`   – 游戏模式、场景类型、权限查询  
- `INetworkBridge`     – 字典数据同步  
- `IUpgradeBridge`     – 升级系统（支持原生升级，并在 REPOLib 存在时自动增强）  
- `IEnemyBridge`       – 怪物操作（获取列表、位置、高亮等）  
- `IEnergyBridge`      – 玩家体力控制（读写、原版恢复规则、下蹲额外恢复、总恢复速率）

通过 `BridgeLocator` 静态类获取所需桥接实例，无需关心底层实现：

    var player = BridgeLocator.Player;
    var state = BridgeLocator.GameState;
    var energy = BridgeLocator.Energy;
    var upgrade = BridgeLocator.Upgrade;

- 若 REPOLib 已加载，升级桥接会优先使用 `REPOLibItemUpgrade.UpgradeId` 映射自定义升级。  
- 若 REPOLib 未加载，升级桥接回退到原生组件类型映射（仅支持原版升级）。  
所有其他桥接接口均为纯原生实现，不依赖 REPOLib。

### 2. 事件总线与事件生成器（命名空间 `Dinwlooc.Common.Core` 和 `Events`）

轻量级发布-订阅机制，支持模组间松耦合通信。

    // 发布事件
    EventBus.Publish(new PlayerRevivedEvent(playerAvatar));
    // 订阅事件
    EventBus.Subscribe<PlayerRevivedEvent>(e => { /* 处理逻辑 */ });

预定义事件：
- `PlayerDiedEvent` – 玩家死亡时触发  
- `PlayerRevivedEvent` – 玩家复活时触发  
- `MonsterVisibilityChangedEvent` – 怪物可见性变化时触发  
- `PlayerLevelEnteredEvent` – **玩家进入关卡时触发（仅主机/单机）**  
- `PlayerJoinedEvent` – **玩家加入任意场景（包括大厅、商店）时触发（仅主机/单机）**

**事件生成器基类 `EventGeneratorBase<T>`** 提供按帧检测机制，模组需显式注册步长（帧数）以启用检测：

    // 注册每10帧检测一次
    PlayerLevelEnterEventGenerator.Instance.RegisterStep(10);
    // 取消注册
    PlayerLevelEnterEventGenerator.Instance.UnregisterStep(10);

多个模组可注册不同步长，生成器自动计算最大公约数作为实际检测间隔。

### 3. 缓存中心（`CacheManager`）

提供跨模组共享的缓存存储，避免重复构建和内存浪费。当一个模组已构建某种类型缓存（如远程配置、玩家状态等），其他模组可通过名称直接获取并复用，甚至协助更新。

    // 创建或获取缓存
    var cache = CacheManager.GetOrCreateCache<string, MyData>("MyMod_Cache",
        () => new MemoryCache<string, MyData>());
    // 读写数据
    cache.Set("key", data);
    if (cache.TryGet("key", out var result)) { /* 使用 */ }

内置 `MemoryCache<TKey, TValue>` 实现，支持过期时间。

### 4. 同步缓存（`SyncCache`，命名空间 `Dinwlooc.Common.Sync`）

基于 Photon RPC 的自动网络同步缓存，支持多种同步模式（房主权威、客户端快照、合并）。**采用懒加载**，仅在首次调用 `GetOrCreateSyncCache` 时初始化网络组件。

    // 创建房主权威同步缓存（房主写入，自动广播）
    var syncCache = CacheManager.GetOrCreateSyncCache<string, int>(
        "MySyncData",
        SyncMode.HostAuthority
    );
    syncCache.Set("score", 100);   // 房主自动同步给所有客户端
    syncCache.OnDataChanged += (key, value) => { /* 数据变更回调 */ };

**初始化时机警告**：由于同步缓存依赖 Photon 网络状态和游戏场景信息，**请勿在插件的 Awake 中调用 `GetOrCreateSyncCache`**，推荐在 `Start`、`CommonService.RegisterUpdate` 延迟一帧或关卡加载完成后（`LevelGenerator.Instance.Generated == true`）再调用。

### 5. 公共服务宿主（`CommonService`）

提供统一的 MonoBehaviour 生命周期回调注册，避免各模组自行挂载组件。

    CommonService.Instance.RegisterUpdate(dt => { /* 每帧执行 */ });
    CommonService.Instance.RegisterFixedUpdate(dt => { /* 固定时间步执行 */ });
    CommonService.Instance.RunCoroutine(MyCoroutine());

### 6. UI 辅助（`MenuHelper`）

封装 MenuLib 的按钮创建，自动读取配置中的位置和开关。

    MenuHelper.AddEscapeMenuButton(
        text: "我的按钮",
        onClick: () => { /* 逻辑 */ },
        enabledConfig: MyConfig.Enabled,
        posXConfig: MyConfig.PosX,
        posYConfig: MyConfig.PosY
    );

### 7. 配置基类（`ConfigBase<T>` / `MenuConfigBase<T>`）

简化模组配置管理，自动绑定开关和位置。
- `ConfigBase<T>` – 仅提供总开关 `Enabled`。  
- `MenuConfigBase<T>` – 继承自前者，额外提供 `PosX` 和 `PosY` 用于菜单按钮。

示例：

    public class MyConfig : MenuConfigBase<MyConfig>
    {
        public ConfigEntry<int> HealAmount { get; private set; }
        public override void Bind(ConfigFile config)
        {
            base.Bind(config);
            HealAmount = config.Bind("Healing", "Amount", 5, "恢复量");
        }
    }
    // 在模组 Awake 中初始化
    MyConfig.Instance.Initialize(Config);

---

## 面向模组开发者的使用指南

### 添加依赖
在你的模组项目文件中引用 `REPO_Dinwlooc_Lib.dll`，并在插件类上声明：

    [BepInDependency("Dinwlooc.Common")]

### 按需获取桥接

    using Dinwlooc.Common.Bridge;

    private IPlayerBridge _player = BridgeLocator.Player;
    private IGameStateBridge _state = BridgeLocator.GameState;
    private IEnergyBridge _energy = BridgeLocator.Energy;
    private IUpgradeBridge _upgrade = BridgeLocator.Upgrade; // 自动选择实现

### 使用事件

    using Dinwlooc.Common.Core;
    using Dinwlooc.Common.Events;

    // 订阅
    EventBus.Subscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
    // 取消订阅
    EventBus.Unsubscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);

    private void OnPlayerLevelEntered(PlayerLevelEnteredEvent ev) { /* 处理玩家进入关卡 */ }

### 注册帧回调

    private void Awake() => CommonService.Instance.RegisterUpdate(OnUpdate);
    private void OnUpdate(float deltaTime) { /* 每帧执行 */ }
    private void OnDestroy() => CommonService.Instance.UnregisterUpdate(OnUpdate);

### 添加 ESC 菜单按钮

    using Dinwlooc.Common.Helpers;

    MenuHelper.AddEscapeMenuButton(
        text: "快速重载",
        onClick: () => { /* 重载逻辑 */ },
        enabledConfig: QuickReloadConfig.Instance.Enabled,
        posXConfig: QuickReloadConfig.Instance.PosX,
        posYConfig: QuickReloadConfig.Instance.PosY
    );

### 使用同步缓存（示例）

    using Dinwlooc.Common.Caching;
    using Dinwlooc.Common.Sync;

    private ISyncCache<string, int> _syncCache;

    private void Start()  // 注意：不要在 Awake 中调用
    {
        _syncCache = CacheManager.GetOrCreateSyncCache<string, int>(
            "MyMod_Data",
            SyncMode.HostAuthority
        );
        _syncCache.OnDataChanged += (key, value) => { /* 处理远程更新 */ };
        if (SemiFunc.IsMasterClientOrSingleplayer())
            _syncCache.Set("key", 42);  // 房主写入，自动同步
    }

---

## 兼容性与注意事项

- 本库本身不修改游戏任何代码，仅提供 API 封装，对游戏“零侵入”。
- `IUpgradeBridge` 在 REPOLib 存在时将增强，不存在时回退原生。
- 许多桥接方法会检查主机权限（`IsMasterClientOrSingleplayer`），非主机调用会静默失败（如写入操作）。
- **同步缓存初始化**：请勿在 `Awake` 中调用，推荐在 `Start` 或延迟一帧后执行。
- 本库依赖 MenuLib（硬依赖），若未安装则无法加载。REPOLib 为软依赖。

---

## 许可

本项目采用 **MIT 许可证**，详见 LICENSE 文件。