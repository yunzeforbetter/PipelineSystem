# PipelineSystem - 异步流程管线系统

![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-1.0.0-orange)

一个基于Unity和UniTask的高性能异步流程管线系统，用于构建和执行复杂的异步任务流程。

## ✨ 核心特性

- 🚀 **异步优先**: 基于UniTask构建，完全异步执行
- 🔄 **流式API**: 提供优雅的链式调用语法
- 📦 **上下文共享**: 强类型上下文对象在任务间传递数据
- ⏰ **优先级调度**: 支持任务优先级排序执行
- 🎯 **并行执行**: 支持任务并行处理
- 🛡️ **异常安全**: 完善的错误处理和取消机制
- 🔧 **注册表模式**: 分布式任务注册，支持模块化开发
- ⚡ **高性能**: 基于内存池和对象复用的高效实现

## 📋 系统要求

- Unity 2021.3+
- UniTask 2.0+
- .NET Standard 2.1+

## 🏗️ 架构概览

```
PipelineSystem/
├── pipeline/
│   ├── core/
│   │   ├── IPipelineJob.cs          # 任务接口定义
│   │   ├── AbsPipelineJob.cs        # 抽象任务基类
│   │   ├── ActionTaskNode.cs        # Action任务节点
│   │   ├── GenericTaskNode.cs       # 泛型任务节点
│   │   ├── IContextObject.cs        # 上下文对象接口
│   │   └── PipelineSystemManager.cs # 管线系统管理器
│   ├── PipelineBuilder.cs           # 流式构建器
│   ├── PipelineContext.cs           # 管线上下文
│   └── PipelineRegistry.cs          # 管线注册表
├── extend/
│   └── FunctionUnlockPipeline.cs    # 功能解锁管线示例
└── PipelineSystemExample.cs         # 使用示例
```

## 🚀 快速开始

### 基础使用

```csharp
// 创建简单的Pipeline
var builder = new PipelineBuilder("MyPipeline");

// 添加任务
builder.AddAction(async context =>
{
    Debug.Log("任务1执行");
    await UniTask.Delay(1000);
    return true; // 返回true表示成功
}, "任务1");

builder.AddAction(async context =>
{
    Debug.Log("任务2执行");
    await UniTask.Delay(500);
    return true;
}, "任务2");

// 执行Pipeline
bool result = await builder.ExecuteAsync();
```

### 使用上下文对象

```csharp
// 定义上下文数据
public class GameDataContext : IContextObject
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public float Score { get; set; }
}

// 创建并设置上下文
var context = new GameDataContext 
{ 
    PlayerId = 1001, 
    PlayerName = "Player1",
    Score = 0f
};

var builder = new PipelineBuilder("GameFlow")
    .SetContext(context)
    .AddAction(async ctx =>
    {
        var gameData = ((PipelineContext)ctx).GetContextObject<GameDataContext>();
        Debug.Log($"玩家 {gameData.PlayerName} 开始游戏");
        gameData.Score += 100;
        return true;
    }, "开始游戏")
    .AddAction(async ctx =>
    {
        var gameData = ((PipelineContext)ctx).GetContextObject<GameDataContext>();
        Debug.Log($"当前分数: {gameData.Score}");
        return true;
    }, "显示分数");

await builder.ExecuteAsync();
```

## 🎯 高级用法

### 1. 注册表模式

适用于模块化开发，多个类可以向同一个Pipeline注册任务：

```csharp
const string GAME_INIT_PIPELINE = "GameInitialization";

// 在不同的类中注册任务
// UIModule.cs
PipelineRegistry.Instance.RegisterActionWithPriority(GAME_INIT_PIPELINE, async context =>
{
    Debug.Log("初始化UI系统");
    await InitializeUI();
    return true;
}, 10, "UI初始化");

// AudioModule.cs  
PipelineRegistry.Instance.RegisterActionWithPriority(GAME_INIT_PIPELINE, async context =>
{
    Debug.Log("初始化音频系统");
    await InitializeAudio();
    return true;
}, 20, "音频初始化");

// GameManager.cs
PipelineRegistry.Instance.RegisterActionWithPriority(GAME_INIT_PIPELINE, async context =>
{
    Debug.Log("初始化游戏逻辑");
    await InitializeGameLogic();
    return true;
}, 0, "游戏逻辑初始化");

// 按优先级执行所有注册的任务
await PipelineRegistry.Instance.ExecutePriorityPipelineAsync(GAME_INIT_PIPELINE);
```

### 2. 并行任务处理

```csharp
var builder = new PipelineBuilder("ParallelExample");

// 创建并行任务
var task1 = new ActionTaskNode(async ctx => 
{
    Debug.Log("并行任务1开始");
    await UniTask.Delay(2000);
    Debug.Log("并行任务1完成");
    return true;
}, "并行任务1");

var task2 = new ActionTaskNode(async ctx => 
{
    Debug.Log("并行任务2开始");
    await UniTask.Delay(1000);
    Debug.Log("并行任务2完成");
    return true;
}, "并行任务2");

// 添加并行执行
builder.AddParallel(task1, task2);

await builder.ExecuteAsync();
```

### 3. 条件等待和错误处理

```csharp
var builder = new PipelineBuilder("ConditionalPipeline");

bool isResourceLoaded = false;

builder
    .AddAction(async ctx =>
    {
        Debug.Log("开始加载资源");
        // 模拟资源加载
        UniTask.Delay(3000).ContinueWith(() => isResourceLoaded = true);
        return true;
    }, "启动加载")
    .AddWaitUntil(() => isResourceLoaded, 10000) // 等待资源加载完成，最多10秒
    .AddAction(async ctx =>
    {
        if (!isResourceLoaded)
        {
            Debug.LogError("资源加载超时");
            return false;
        }
        Debug.Log("资源加载完成，继续执行");
        return true;
    }, "检查资源状态");

await builder.ExecuteAsync();
```

### 4. 复杂游戏流程示例

```csharp
// 游戏战斗流程
public class BattleFlowContext : IContextObject
{
    public int BattleId { get; set; }
    public List<int> PlayerIds { get; set; } = new List<int>();
    public BattleResult Result { get; set; }
}

var battleFlow = new PipelineBuilder("BattleFlow")
    .SetContext(new BattleFlowContext { BattleId = 1001 })
    
    // 战斗准备阶段
    .AddAction(async ctx =>
    {
        var battle = ((PipelineContext)ctx).GetContextObject<BattleFlowContext>();
        Debug.Log($"准备战斗 {battle.BattleId}");
        
        // 初始化战斗数据
        battle.PlayerIds.AddRange(new[] { 1, 2, 3 });
        await UniTask.Delay(1000);
        return true;
    }, "战斗准备")
    
    // 加载战斗资源
    .AddAction(async ctx =>
    {
        Debug.Log("加载战斗场景和角色");
        await LoadBattleAssets();
        return true;
    }, "资源加载")
    
    // 等待所有玩家准备就绪
    .AddWaitUntil(() => AllPlayersReady(), 30000)
    
    // 开始战斗
    .AddAction(async ctx =>
    {
        Debug.Log("战斗开始！");
        var battle = ((PipelineContext)ctx).GetContextObject<BattleFlowContext>();
        
        // 模拟战斗过程
        await SimulateBattle(battle);
        return battle.Result != null;
    }, "执行战斗")
    
    // 结算奖励
    .AddAction(async ctx =>
    {
        var battle = ((PipelineContext)ctx).GetContextObject<BattleFlowContext>();
        Debug.Log($"战斗结束，结果: {battle.Result}");
        
        await DistributeRewards(battle.Result);
        return true;
    }, "奖励结算");

bool success = await battleFlow.ExecuteAsync();
```

## 📚 API 文档

### PipelineBuilder

| 方法 | 描述 | 参数 | 返回值 |
|------|------|------|--------|
| `AddJob(IPipelineJob)` | 添加任务节点 | job: 任务实例 | PipelineBuilder |
| `AddAction(Func<IContextObject, UniTask<bool>>, string)` | 添加Action任务 | action: 异步委托, name: 任务名 | PipelineBuilder |
| `AddTypedAction<T>(Func<T, UniTask<bool>>, string)` | 添加泛型Action任务 | action: 类型化委托, name: 任务名 | PipelineBuilder |
| `AddDelay(int)` | 添加延迟任务 | milliseconds: 延迟毫秒数 | PipelineBuilder |
| `AddWaitUntil(Func<bool>, int)` | 添加条件等待 | predicate: 条件函数, timeout: 超时时间 | PipelineBuilder |
| `AddParallel(params IPipelineJob[])` | 添加并行任务组 | jobs: 并行执行的任务数组 | PipelineBuilder |
| `SetContext(IContextObject)` | 设置上下文对象 | contextObject: 上下文实例 | PipelineBuilder |
| `ExecuteAsync()` | 执行Pipeline | - | UniTask<bool> |

### PipelineRegistry

| 方法 | 描述 | 参数 | 返回值 |
|------|------|------|--------|
| `RegisterAction(string, Func<IContextObject, UniTask<bool>>, string)` | 注册Action任务 | key: Pipeline键, action: 异步委托, name: 任务名 | bool |
| `RegisterActionWithPriority(string, Func<IContextObject, UniTask<bool>>, int, string)` | 按优先级注册任务 | key: Pipeline键, action: 异步委托, priority: 优先级, name: 任务名 | bool |
| `ExecuteAsync(string)` | 执行指定Pipeline | key: Pipeline键 | UniTask<bool> |
| `ExecutePriorityPipelineAsync(string)` | 按优先级执行Pipeline | key: Pipeline键 | UniTask<bool> |
| `SetContextObject(string, IContextObject)` | 设置上下文对象 | key: Pipeline键, contextObject: 上下文实例 | bool |
| `CancelPipeline(string)` | 取消Pipeline执行 | key: Pipeline键 | void |

### PipelineContext

| 方法 | 描述 | 参数 | 返回值 |
|------|------|------|--------|
| `SetContextObject(IContextObject)` | 设置上下文对象 | contextObject: 上下文实例 | void |
| `GetContextObject<T>()` | 获取上下文对象 | - | T |
| `TryGetContextObject<T>(out T)` | 尝试获取上下文对象 | contextObject: 输出参数 | bool |
| `RemoveContextObject<T>()` | 移除上下文对象 | - | bool |
| `Cancel()` | 取消所有异步操作 | - | void |

## 🔧 最佳实践

### 1. 上下文对象设计

```csharp
// ✅ 好的做法：专用的上下文类
public class LoginFlowContext : IContextObject
{
    public string Username { get; set; }
    public string Token { get; set; }
    public UserProfile Profile { get; set; }
    public bool IsNewUser { get; set; }
}

// ❌ 避免：使用通用字典
public class GenericContext : IContextObject
{
    public Dictionary<string, object> Data { get; set; }
}
```

### 2. 错误处理

```csharp
// ✅ 好的做法：明确的错误处理
builder.AddAction(async ctx =>
{
    try
    {
        await SomeRiskyOperation();
        return true;
    }
    catch (NetworkException ex)
    {
        Debug.LogError($"网络错误: {ex.Message}");
        return false; // 明确返回false表示失败
    }
    catch (Exception ex)
    {
        Debug.LogError($"未知错误: {ex}");
        return false;
    }
}, "网络操作");
```

### 3. 任务命名规范

```csharp
// ✅ 好的做法：清晰的任务命名
builder
    .AddAction(async ctx => { /* ... */ }, "验证用户凭据")
    .AddAction(async ctx => { /* ... */ }, "加载用户配置")
    .AddAction(async ctx => { /* ... */ }, "初始化用户界面")
    .AddAction(async ctx => { /* ... */ }, "发送登录事件");
```

### 4. 资源管理

```csharp
// ✅ 好的做法：正确的资源清理
public class ResourceContext : IContextObject, IDisposable
{
    public Texture2D LoadedTexture { get; set; }
    public AudioClip LoadedAudio { get; set; }
    
    public void Dispose()
    {
        if (LoadedTexture != null)
            Object.Destroy(LoadedTexture);
        if (LoadedAudio != null)
            Object.Destroy(LoadedAudio);
    }
}
```

## 🐛 常见问题

### Q: Pipeline执行失败时如何调试？

A: 系统提供了完整的日志记录，可以通过以下方式查看：

```csharp
// 启用详细日志
Log.PipelineSystem.Debug("Pipeline执行状态");

// 检查任务返回值
bool result = await builder.ExecuteAsync();
if (!result)
{
    Debug.LogError("Pipeline执行失败，请检查各任务的返回值");
}
```

### Q: 如何处理长时间运行的任务？

A: 使用取消令牌和超时控制：

```csharp
builder.AddAction(async ctx =>
{
    var pipelineCtx = (PipelineContext)ctx;
    try
    {
        await LongRunningTask().AttachExternalCancellation(pipelineCtx.CancellationToken);
        return true;
    }
    catch (OperationCanceledException)
    {
        Debug.Log("任务被取消");
        return false;
    }
}, "长时间任务");
```

### Q: 如何实现任务重试机制？

A: 可以在任务内部实现重试逻辑：

```csharp
builder.AddAction(async ctx =>
{
    const int maxRetries = 3;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await UnstableOperation();
            return true; // 成功则返回
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            Debug.LogWarning($"操作失败，重试 {i + 1}/{maxRetries}: {ex.Message}");
            await UniTask.Delay(1000 * (i + 1)); // 指数退避
        }
    }
    return false; // 所有重试都失败
}, "不稳定操作");
```

## 📄 更新日志

### v1.0.0 (2024-01-15)
- 🎉 初始版本发布
- ✨ 基础Pipeline构建和执行功能
- ✨ 注册表模式支持
- ✨ 上下文对象系统
- ✨ 并行任务支持
- ✨ 优先级调度
- ✨ 完整的异常处理和取消机制

## 🤝 贡献指南

1. Fork 本项目
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 📝 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 👨‍💻 作者

**Your Name** - *初始创建者* - [您的GitHub](https://github.com/yourusername)

---

⭐ 如果这个项目对您有帮助，请给它一个星标！

📧 有问题或建议？欢迎提交 [Issues](https://github.com/yourusername/PipelineSystem/issues)！
