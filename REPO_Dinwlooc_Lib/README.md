REPO_Dinwlooc_Lib
=================

Dinwlooc 模组公共依赖库
为 Dinwlooc 系列模组提供统一的游戏桥接、事件总线、UI 辅助和配置基类，简化开发并增强模组间的互操作性。


前置依赖（必须安装）
--------------------
* BepInEx 5.x – 插件加载框架。
* REPOLib – 提供自定义物品与升级系统支持（版本 ≥ 4.2.0）。
  下载地址：https://thunderstore.io/c/repo/p/Zehs/REPOLib/
* MenuLib – 提供菜单界面扩展（版本 ≥ 1.1.0）。
  下载地址：https://thunderstore.io/c/repo/p/nickklmao/MenuLib/

请将所有依赖库放在 BepInEx/plugins 目录下。


安装方法
--------
1. 下载 REPO_Dinwlooc_Lib.zip 并解压。
2. 将 REPO_Dinwlooc_Lib 文件夹整体放入 BepInEx/plugins 目录。
3. 启动游戏，确保前置依赖已加载。本库在 REPOLib v4.2.0 和 MenuLib v1.1.0 环境下测试通过。


功能概览
--------
本库本身不提供任何直接面向用户的游戏功能，而是为其他模组提供以下基础设施：

1. 原子化游戏桥接（命名空间 Dinwlooc.Common.Bridge）
   将游戏原生 API 拆分为多个职责单一的接口，模组可按需引用：

   - IPlayerBridge      – 玩家获取、血量读写、治愈
   - IItemBridge        – 手持物品电池操作
   - IHealthPackBridge  – 医疗包查找与消耗
   - ITruckBridge       – 卡车电量查询与消耗
   - ISaveLoadBridge    – 存档读写、场景重载、跳转商店
   - IGameStateBridge   – 游戏模式、场景类型、权限查询
   - INetworkBridge     – 字典数据同步
   - IUpgradeBridge     – 升级系统（支持原生升级，并在 REPOLib 存在时自动增强）

   通过 BridgeLocator 静态类获取所需桥接实例，无需关心底层实现：
   var player = BridgeLocator.Player;
   var state = BridgeLocator.GameState;
   var upgrade = BridgeLocator.Upgrade; // 自动检测并选择最佳实现

   - 若 REPOLib 已加载，升级桥接会优先使用 REPOLibItemUpgrade.UpgradeId 映射自定义升级。
   - 若 REPOLib 未加载，升级桥接回退到原生组件类型映射（仅支持原版升级）。
   所有其他桥接接口（玩家、物品、卡车等）均为纯原生实现，不依赖 REPOLib。

2. 事件总线（命名空间 Dinwlooc.Common.Core.EventBus）
   轻量级发布-订阅机制，支持模组间松耦合通信。
   发布事件：EventBus.Publish(new PlayerRevivedEvent(playerAvatar));
   订阅事件：EventBus.Subscribe<PlayerRevivedEvent>(e => { /* 处理逻辑 */ });

   预定义事件：
   - PlayerRevivedEvent      – 玩家复活时触发
   - SceneReloadedEvent      – 场景重载完成时触发
   - UpgradeUninstalledEvent – 升级卸载完成时触发

3. 公共服务宿主（CommonService）
   提供统一的 MonoBehaviour 生命周期回调注册，避免各模组自行挂载组件。
   CommonService.Instance.RegisterUpdate(dt => { /* 每帧执行 */ });
   CommonService.Instance.RegisterFixedUpdate(dt => { /* 固定时间步执行 */ });
   CommonService.Instance.RunCoroutine(MyCoroutine());

4. UI 辅助（MenuHelper）
   封装 MenuLib 的按钮创建，自动读取配置中的位置和开关。
   MenuHelper.AddEscapeMenuButton(
       text: "我的按钮",
       onClick: () => { /* 逻辑 */ },
       enabledConfig: MyConfig.Enabled,
       posXConfig: MyConfig.PosX,
       posYConfig: MyConfig.PosY
   );

5. 配置基类（ModConfig<T>）
   简化模组配置管理，自动绑定开关和位置。
   public class MyConfig : ModConfig<MyConfig>
   {
       public ConfigEntry<int> HealAmount { get; private set; }
       protected override void Bind(ConfigFile config)
       {
           HealAmount = config.Bind("Healing", "Amount", 5, "恢复量");
       }
   }
   在模组 Awake 中初始化：
   MyConfig.Instance.Initialize(Config);


面向模组开发者的使用指南
------------------------
添加依赖：
在你的模组项目文件中引用 REPO_Dinwlooc_Lib.dll，并在插件类上声明：
[BepInDependency("Dinwlooc.Common")]

按需获取桥接：
using Dinwlooc.Common.Bridge;

// 所有接口均通过 BridgeLocator 获取，无需关心是 Native 还是 REPOLib 增强版
private IPlayerBridge _player = BridgeLocator.Player;
private IGameStateBridge _state = BridgeLocator.GameState;
private IUpgradeBridge _upgrade = BridgeLocator.Upgrade; // 自动选择实现

// 升级桥接即使 REPOLib 未加载也不会返回 null，始终可用。
// 若 REPOLib 未加载，自定义升级将无法识别，但原生升级仍可正常工作。

使用事件：
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
// 订阅
EventBus.Subscribe<PlayerRevivedEvent>(OnPlayerRevived);
// 取消订阅
EventBus.Unsubscribe<PlayerRevivedEvent>(OnPlayerRevived);
private void OnPlayerRevived(PlayerRevivedEvent ev) { /* 处理玩家复活 */ }

注册帧回调：
private void Awake()
{
    CommonService.Instance.RegisterUpdate(OnUpdate);
}
private void OnUpdate(float deltaTime) { /* 每帧执行 */ }
private void OnDestroy()
{
    CommonService.Instance.UnregisterUpdate(OnUpdate);
}

添加 ESC 菜单按钮：
using Dinwlooc.Common.Helpers;
MenuHelper.AddEscapeMenuButton(
    text: "快速重载",
    onClick: () => { /* 重载逻辑 */ },
    enabledConfig: QuickReloadConfig.Instance.Enabled,
    posXConfig: QuickReloadConfig.Instance.PosX,
    posYConfig: QuickReloadConfig.Instance.PosY
);


兼容性与注意事项
----------------
* 本库本身不修改游戏任何代码，仅提供 API 封装，对游戏“零侵入”。
* IUpgradeBridge 在 REPOLib 存在时将增强。
* 所有桥接方法均会检查主机权限（IsMasterClientOrSingleplayer），非主机调用会静默失败（如写入操作）。
* 本库依赖 MenuLib，若未安装则 MenuHelper 会抛出异常，请确保在调用前检查依赖。


许可
----
本项目采用 MIT 许可证，详见 LICENSE 文件。