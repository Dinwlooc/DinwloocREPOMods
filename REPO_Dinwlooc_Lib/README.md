# REPO_Dinwlooc_Lib

Dinwlooc 模组公共依赖库，为 Dinwlooc 系列模组提供统一的游戏桥接、事件总线、缓存中心、同步缓存、UI 辅助、配置基类、翻译管理和网络行为基类，简化开发并增强模组间的互操作性。

---

## 前置依赖

- **BepInEx 5.x** – 插件加载框架。
- **MenuLib** – 提供菜单界面扩展（版本 ≥ 1.1.0）。下载地址：https://thunderstore.io/c/repo/p/nickklmao/MenuLib/
- **REPOLib** – 提供自定义物品与升级系统支持（版本 ≥ 4.2.0）。下载地址：https://thunderstore.io/c/repo/p/Zehs/REPOLib/

请将所有依赖库放在 `BepInEx/plugins` 目录下。

---

## 安装方法

1. 下载 `REPO_Dinwlooc_Lib.zip` 并解压。
2. 将 `REPO_Dinwlooc_Lib` 文件夹整体放入 `BepInEx/plugins` 目录。
3. 启动游戏，确保前置依赖已加载。本库在 REPOLib v4.2.0 和 MenuLib v1.1.0 环境下测试通过。

---

## 功能概览

本库为其他模组提供基础设施，本身不提供直接面向用户的功能。

### 1. 原子化游戏桥接

将游戏原生 API 拆分为多个单一职责的接口，通过 `BridgeLocator` 获取。例如：

    var player = BridgeLocator.Player;       // IPlayerBridge
    var enemy = BridgeLocator.Enemy;         // IEnemyBridge
    var energy = BridgeLocator.Energy;       // IEnergyBridge

所有桥接均已自动注册。

### 2. 事件总线与事件生成器

提供发布‑订阅机制，事件生成器检测状态变化并发布事件。

    EventBus.Subscribe<PlayerRevivedEvent>(e => { /* 处理 */ });
    EventBus.Publish(new PlayerRevivedEvent(player));

- 订阅 `PlayerDiedEvent`、`PlayerRevivedEvent`、`PlayerJoinedEvent`、`MonsterVisibilityChangedEvent` 时，对应生成器自动启用（默认步长 60 帧）。
- 怪物事件（`EnemySpawnedEvent` 等）需手动调用 `EnemyEventGenerator.Instance.RegisterStep(10)` 启用检测。
- `SceneChangedEvent` 由 `SceneEventGenerator` 持续监听，无法关闭。
- 生成器不依赖网络，单机模式同样工作。

### 3. 缓存中心（`CacheManager`）

提供本地内存缓存（`MemoryCache<TKey,TValue>`），支持过期时间，可跨模组共享。

    var cache = CacheManager.GetOrCreateCache<string, MyData>("MyCache", () => new MemoryCache<string, MyData>());
    cache.Set("key", data);

### 4. 同步缓存（`SyncCache`）

基于 Photon RPC 的网络同步缓存，支持 `HostAuthority`、`ClientSnapshot`、`Merge` 三种模式。

    var sync = CacheManager.GetOrCreateSyncCache<string, int>("Score", SyncMode.HostAuthority);
    sync.Set("player1", 100);  // 仅房主可写入

**重要限制**：在游戏启动早期（`Awake`、`Start`、`OnJoinedRoom`）调用会破坏多人连接，必须延迟到 `PlayerJoinedEvent` 或关卡加载后初始化。

### 5. 公共服务宿主（`CommonService`）

提供统一的 `MonoBehaviour` 生命周期回调注册。

    CommonService.Instance.RegisterUpdate(dt => { /* 每帧执行 */ });

### 6. UI 辅助（`MenuHelper`）

封装 MenuLib 的按钮创建，自动读取配置中的位置和开关。

    MenuHelper.AddEscapeMenuButton("我的按钮", () => { }, MyConfig.Enabled, MyConfig.PosX, MyConfig.PosY);

### 7. 配置基类（`ConfigBase<T>` / `MenuConfigBase<T>`）

简化配置管理，自动绑定 `Enabled`（及 `PosX`/`PosY`）。继承 `MenuConfigBase<T>` 获得所有配置项。

    public class MyConfig : MenuConfigBase<MyConfig> {
        public override void Bind(ConfigFile config) { base.Bind(config); }
    }
    MyConfig.Instance.Initialize(Config);

### 8. 翻译管理（`TranslationManager`）

为模组生成统一格式的翻译文件，存放于 `BepInEx/Config/Translation/{语言}/Dinwlooc_Translation/{模组ID}.txt`，兼容 XUnity.AutoTranslator。

    var dict = new Dictionary<string, string> { { "Hello", "你好" } };
    TranslationManager.RegisterTranslations("MyMod", "zh", 1, dict);

### 9. 网络行为基类（`NetworkBehaviour`）

继承自 `MonoBehaviour`，自动订阅 `SycnReadyEvent` 和 `LeftRoomEvent`，提供 `OnSyncReady()` / `OnLeftRoom()` 虚方法，便于在同步器就绪时执行逻辑。暂时不能感知到网络的初始化。

    public class MyNetworkBehaviour : NetworkBehaviour {
        protected override void OnSyncReady() { /* 网络就绪 */ }
        protected override void OnLeftRoom() { /* 离开房间 */ }
    }

---

## 面向模组开发者的使用指南

### 添加依赖

在项目文件中引用 `REPO_Dinwlooc_Lib.dll`，并在插件类上声明 `[BepInDependency("Dinwlooc.Common")]`。

### 获取桥接

    var player = BridgeLocator.Player;
    var state = BridgeLocator.GameState;

### 订阅事件

    EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);

### 注册帧回调

    CommonService.Instance.RegisterUpdate(OnUpdate);
    CommonService.Instance.UnregisterUpdate(OnUpdate);

### 添加 ESC 菜单按钮

    MenuHelper.AddEscapeMenuButton("重载", () => { }, MyConfig.Enabled, MyConfig.PosX, MyConfig.PosY);

### 使用同步缓存（安全初始化）

    private ISyncCache<string, int> _cache;
    private void Awake() => EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
    private void OnPlayerJoined(PlayerJoinedEvent _) {
        if (_cache != null) return;
        _cache = CacheManager.GetOrCreateSyncCache<string, int>("Data", SyncMode.HostAuthority);
        if (SemiFunc.IsMasterClientOrSingleplayer()) _cache.Set("key", 42);
    }

### 注册翻译

    var dict = new Dictionary<string, string> { { "Start", "开始" } };
    TranslationManager.RegisterTranslations("MyMod", "zh", 1, dict);

### 使用网络行为基类

继承 `NetworkBehaviour` 并重写对应方法可以自动使用回调预设。该模式可能推广，暂不深入介绍。

---

## 注意事项

- 本库对游戏“零侵入”。
- `IUpgradeBridge` 在 REPOLib 存在时增强，否则回退原生。
- 写入类桥接（如 `IEnemyModifierBridge`、`ITruckBridge.ConsumeTruckCharge`）需主机权限。
- **同步缓存初始化禁止在 `Awake`/`Start`/`OnJoinedRoom` 中调用**，必须延迟到 `PlayerJoinedEvent` 或关卡加载后。
- `SceneEventGenerator` 一旦创建即持续工作，无法关闭。
- 怪物事件生成器需要手动 `RegisterStep` 才会工作。
- 事件生成器不依赖网络，单机模式同样可用。
- 依赖 MenuLib（硬依赖），REPOLib 为软依赖。

---

## 接口与事件清单

### 桥接接口（通过 `BridgeLocator` 获取）

- `IPlayerBridge` – 玩家操作
- `IGameStateBridge` – 游戏状态查询
- `IItemBridge` – 物品电池操作
- `IHealthPackBridge` – 医疗包操作
- `ITruckBridge` – 卡车电量
- `ISaveLoadBridge` – 存档与场景
- `INetworkBridge` – 字典同步
- `IUpgradeBridge` – 升级系统
- `IEnemyBridge` – 怪物查询
- `IEnemyModifierBridge` – 怪物修改（主机）
- `IEnergyBridge` – 体力控制
- `IMenuBridge` – ESC 菜单
- `ISlideBridge` – 滑铲状态
- `IMovementOverrideBridge` – 速度与移动覆盖
- `IAppearanceOverrideBridge` – 外观覆盖
- `IDeathHeadOverrideBridge` – 死亡头部覆盖
- `IGrabberOverrideBridge` – 抓取器覆盖
- `ITumbleOverrideBridge` – 翻滚覆盖

### 预定义事件（订阅时自动启用对应生成器）

- `SceneChangedEvent`（始终激活）
- `PlayerDiedEvent` / `PlayerRevivedEvent`
- `PlayerJoinedEvent`
- `MonsterVisibilityChangedEvent`

### 需手动启用生成器的事件（调用 `EnemyEventGenerator.RegisterStep`）

- `EnemySpawnedEvent`
- `EnemyDiedEvent`
- `EnemyHurtEvent`
- `EnemyVisionEvent`
- `EnemyInvestigateEvent`
- `EnemyGrabbedEvent`
- `EnemyDespawnEvent`

### 网络事件（同步缓存内部使用）

- `SyncReadyEvent` / `LeftRoomEvent`
- `CustomRequestEvent` / `CustomResponseEvent`

### 配置基类

- `ConfigBase<T>` – 提供 `Enabled`
- `MenuConfigBase<T>` – 继承前者，增加 `PosX`、`PosY`

### 其他工具

- `TranslationManager` – 翻译注册
- `MenuHelper` – 按钮辅助
- `NetworkBehaviour` – 网络行为基类

---

## 许可

本项目采用 **MIT 许可证**，详见 LICENSE 文件。