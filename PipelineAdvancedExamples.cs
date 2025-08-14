#if UNITY_EDITOR
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PipelineSystem
{
    /// <summary>
    /// Pipeline系统高级用法示例
    /// 展示复杂场景和最佳实践
    /// </summary>
    public class PipelineAdvancedExamples : MonoBehaviour
    {
        [Header("高级示例控制")]
        public bool runConditionalPipelineExample = true;
        public bool runDynamicPipelineExample = true;
        public bool runCustomTaskNodeExample = true;
        public bool runResourceManagerExample = true;
        public bool runUIFlowExample = true;
        
        private void Start()
        {
            RunAdvancedExamples().Forget();
        }

        private async UniTaskVoid RunAdvancedExamples()
        {
            Debug.Log("=== PipelineSystem 高级示例开始 ===");
            
            if (runConditionalPipelineExample)
            {
                ExampleConditionalPipelineAsync().Forget();
                await UniTask.Delay(2000);
            }
            
            if (runDynamicPipelineExample)
            {
                ExampleDynamicPipelineAsync().Forget();
                await UniTask.Delay(2000);
            }
            
            if (runCustomTaskNodeExample)
            {
                ExampleCustomTaskNodeAsync().Forget();
                await UniTask.Delay(2000);
            }
            
            if (runResourceManagerExample)
            {
                ExampleResourceManagerAsync().Forget();
                await UniTask.Delay(2000);
            }
            
            if (runUIFlowExample)
            {
                ExampleUIFlowAsync().Forget();
                await UniTask.Delay(2000);
            }
            
            Debug.Log("=== 所有高级示例执行完成 ===");
        }
        
        #region 条件管道示例
        
        /// <summary>
        /// 条件执行管道示例 - 根据运行时条件决定执行路径
        /// </summary>
        private async UniTaskVoid ExampleConditionalPipelineAsync()
        {
            Debug.Log("=== 开始条件执行管道示例 ===");
            
            var context = new ConditionalContext
            {
                UserLevel = UnityEngine.Random.Range(1, 100),
                IsVipUser = UnityEngine.Random.Range(0, 2) == 1,
                HasCompletedTutorial = UnityEngine.Random.Range(0, 2) == 1
            };
            
            var builder = new PipelineBuilder("ConditionalPipeline")
                .SetContext(context)
                
                // 用户状态检查
                .AddAction(async ctx =>
                {
                    var condCtx = ((PipelineContext)ctx).GetContextObject<ConditionalContext>();
                    Debug.Log($"用户状态: 等级{condCtx.UserLevel}, VIP:{condCtx.IsVipUser}, 教程:{condCtx.HasCompletedTutorial}");
                    await UniTask.Delay(300);
                    return true;
                }, "用户状态检查")
                
                // 条件分支 - 新手引导
                .AddAction(async ctx =>
                {
                    var condCtx = ((PipelineContext)ctx).GetContextObject<ConditionalContext>();
                    
                    if (!condCtx.HasCompletedTutorial)
                    {
                        Debug.Log("执行新手引导流程...");
                        await UniTask.Delay(1500);
                        
                        condCtx.ExecutedTasks.Add("新手引导");
                        condCtx.HasCompletedTutorial = true;
                    }
                    else
                    {
                        Debug.Log("跳过新手引导");
                    }
                    
                    return true;
                }, "新手引导检查")
                
                // 条件分支 - VIP特权
                .AddAction(async ctx =>
                {
                    var condCtx = ((PipelineContext)ctx).GetContextObject<ConditionalContext>();
                    
                    if (condCtx.IsVipUser)
                    {
                        Debug.Log("激活VIP特权...");
                        await UniTask.Delay(800);
                        
                        condCtx.ExecutedTasks.Add("VIP特权激活");
                        condCtx.VipRewards = new List<string> { "双倍经验", "专属皮肤", "优先匹配" };
                        
                        Debug.Log($"VIP奖励: {string.Join(", ", condCtx.VipRewards)}");
                    }
                    else
                    {
                        Debug.Log("普通用户，跳过VIP特权");
                    }
                    
                    return true;
                }, "VIP特权处理")
                
                // 条件分支 - 高级用户内容
                .AddAction(async ctx =>
                {
                    var condCtx = ((PipelineContext)ctx).GetContextObject<ConditionalContext>();
                    
                    if (condCtx.UserLevel >= 30)
                    {
                        Debug.Log("解锁高级内容...");
                        await UniTask.Delay(1000);
                        
                        condCtx.ExecutedTasks.Add("高级内容解锁");
                        condCtx.UnlockedFeatures.AddRange(new[] { "高级副本", "公会系统", "竞技场" });
                        
                        Debug.Log($"解锁功能: {string.Join(", ", condCtx.UnlockedFeatures)}");
                    }
                    else
                    {
                        Debug.Log($"等级{condCtx.UserLevel}不足，需要等级30+才能解锁高级内容");
                    }
                    
                    return true;
                }, "高级内容检查")
                
                // 最终报告
                .AddAction(async ctx =>
                {
                    var condCtx = ((PipelineContext)ctx).GetContextObject<ConditionalContext>();
                    
                    Debug.Log("=== 条件管道执行报告 ===");
                    Debug.Log($"执行的任务: {string.Join(", ", condCtx.ExecutedTasks)}");
                    Debug.Log($"解锁的功能: {string.Join(", ", condCtx.UnlockedFeatures)}");
                    
                    if (condCtx.VipRewards?.Count > 0)
                    {
                        Debug.Log($"VIP奖励: {string.Join(", ", condCtx.VipRewards)}");
                    }
                    
                    await UniTask.Delay(500);
                    return true;
                }, "执行报告");
            
            bool result = await builder.ExecuteAsync();
            Debug.Log($"条件管道执行结果: {result}");
        }
        
        #endregion
        
        #region 动态管道示例
        
        /// <summary>
        /// 动态构建管道示例 - 运行时根据配置动态添加任务
        /// </summary>
        private async UniTaskVoid ExampleDynamicPipelineAsync()
        {
            Debug.Log("=== 开始动态构建管道示例 ===");
            
            // 模拟从配置文件或服务器获取的任务配置
            var taskConfigs = new List<TaskConfig>
            {
                new TaskConfig { Id = 1, Name = "数据验证", Duration = 800, Priority = 1, IsEnabled = true },
                new TaskConfig { Id = 2, Name = "资源预加载", Duration = 1500, Priority = 2, IsEnabled = true },
                new TaskConfig { Id = 3, Name = "网络连接测试", Duration = 1200, Priority = 3, IsEnabled = UnityEngine.Random.Range(0, 2) == 1 },
                new TaskConfig { Id = 4, Name = "用户偏好加载", Duration = 600, Priority = 4, IsEnabled = true },
                new TaskConfig { Id = 5, Name = "A/B测试初始化", Duration = 400, Priority = 5, IsEnabled = UnityEngine.Random.Range(0, 2) == 1 },
                new TaskConfig { Id = 6, Name = "分析数据上报", Duration = 300, Priority = 6, IsEnabled = true }
            };
            
            var context = new DynamicContext 
            { 
                TaskConfigs = taskConfigs,
                StartTime = DateTime.Now
            };
            
            var builder = new PipelineBuilder("DynamicPipeline")
                .SetContext(context);
            
            // 动态添加任务
            var enabledTasks = taskConfigs
                .Where(config => config.IsEnabled)
                .OrderBy(config => config.Priority)
                .ToList();
            
            Debug.Log($"将动态添加 {enabledTasks.Count} 个任务:");
            foreach (var task in enabledTasks)
            {
                Debug.Log($"  - {task.Name} (优先级: {task.Priority}, 耗时: {task.Duration}ms)");
            }
            
            // 根据配置动态添加任务
            foreach (var taskConfig in enabledTasks)
            {
                builder.AddAction(CreateDynamicTask(taskConfig), taskConfig.Name);
            }
            
            // 添加完成报告任务
            builder.AddAction(async ctx =>
            {
                var dynCtx = ((PipelineContext)ctx).GetContextObject<DynamicContext>();
                var endTime = DateTime.Now;
                var totalTime = (endTime - dynCtx.StartTime).TotalMilliseconds;
                
                Debug.Log("=== 动态管道执行报告 ===");
                Debug.Log($"总耗时: {totalTime:F0}ms");
                Debug.Log($"成功执行任务: {dynCtx.CompletedTasks.Count}");
                Debug.Log($"任务详情: {string.Join(", ", dynCtx.CompletedTasks.Select(t => t.Name))}");
                
                return true;
            }, "执行报告");
            
            bool result = await builder.ExecuteAsync();
            Debug.Log($"动态管道执行结果: {result}");
        }
        
        private Func<IContextObject, UniTask<bool>> CreateDynamicTask(TaskConfig config)
        {
            return async ctx =>
            {
                var dynCtx = ((PipelineContext)ctx).GetContextObject<DynamicContext>();
                
                Debug.Log($"执行动态任务: {config.Name}");
                await UniTask.Delay(config.Duration);
                
                // 模拟任务可能失败
                bool success = UnityEngine.Random.Range(0, 10) > 0; // 90%成功率
                
                if (success)
                {
                    dynCtx.CompletedTasks.Add(config);
                    Debug.Log($"任务 {config.Name} 完成");
                }
                else
                {
                    Debug.LogWarning($"任务 {config.Name} 失败");
                }
                
                return success;
            };
        }
        
        #endregion
        
        #region 自定义任务节点示例
        
        /// <summary>
        /// 自定义任务节点示例 - 展示如何创建复杂的自定义任务
        /// </summary>
        private async UniTaskVoid ExampleCustomTaskNodeAsync()
        {
            Debug.Log("=== 开始自定义任务节点示例 ===");
            
            var context = new CustomTaskContext();
            
            var builder = new PipelineBuilder("CustomTaskPipeline")
                .SetContext(context)
                .AddJob(new ProgressiveDownloadTask("关卡数据", 50))
                .AddJob(new ConditionalRetryTask("不稳定网络请求", 3))
                .AddJob(new BatchProcessingTask("数据处理", new[] { "file1.dat", "file2.dat", "file3.dat" }))
                .AddJob(new TimedTask("定时同步", TimeSpan.FromSeconds(2)))
                .AddAction(async ctx =>
                {
                    var customCtx = ((PipelineContext)ctx).GetContextObject<CustomTaskContext>();
                    Debug.Log("=== 自定义任务执行报告 ===");
                    Debug.Log($"下载进度: {customCtx.DownloadProgress}%");
                    Debug.Log($"重试次数: {customCtx.RetryCount}");
                    Debug.Log($"处理的文件数: {customCtx.ProcessedFiles.Count}");
                    Debug.Log($"同步状态: {customCtx.SyncStatus}");
                    return true;
                }, "执行报告");
            
            bool result = await builder.ExecuteAsync();
            Debug.Log($"自定义任务管道执行结果: {result}");
        }
        
        #endregion
        
        #region 资源管理器示例
        
        /// <summary>
        /// 资源管理器示例 - 展示复杂的资源加载和管理流程
        /// </summary>
        private async UniTaskVoid ExampleResourceManagerAsync()
        {
            Debug.Log("=== 开始资源管理器示例 ===");
            
            var resourceContext = new ResourceManagerContext();
            
            // 模拟需要加载的资源列表
            var resources = new List<ResourceInfo>
            {
                new ResourceInfo { Id = "ui_textures", Type = ResourceType.Texture, Size = 5.2f, Priority = ResourcePriority.High },
                new ResourceInfo { Id = "character_models", Type = ResourceType.Model, Size = 12.8f, Priority = ResourcePriority.Medium },
                new ResourceInfo { Id = "background_audio", Type = ResourceType.Audio, Size = 3.6f, Priority = ResourcePriority.Low },
                new ResourceInfo { Id = "effect_shaders", Type = ResourceType.Shader, Size = 2.1f, Priority = ResourcePriority.High },
                new ResourceInfo { Id = "config_data", Type = ResourceType.Data, Size = 0.5f, Priority = ResourcePriority.Critical }
            };
            
            resourceContext.ResourcesToLoad = resources;
            
            var builder = new PipelineBuilder("ResourceManager")
                .SetContext(resourceContext)
                
                // 资源优先级排序
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceManagerContext>();
                    
                    // 按优先级排序
                    resCtx.ResourcesToLoad = resCtx.ResourcesToLoad
                        .OrderBy(r => (int)r.Priority)
                        .ToList();
                    
                    Debug.Log("资源加载优先级排序:");
                    foreach (var resource in resCtx.ResourcesToLoad)
                    {
                        Debug.Log($"  {resource.Id} - {resource.Priority} ({resource.Size:F1}MB)");
                    }
                    
                    return true;
                }, "优先级排序")
                
                // 检查存储空间
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceManagerContext>();
                    
                    float totalSize = resCtx.ResourcesToLoad.Sum(r => r.Size);
                    float availableSpace = 50.0f; // 模拟50MB可用空间
                    
                    Debug.Log($"需要空间: {totalSize:F1}MB, 可用空间: {availableSpace:F1}MB");
                    
                    if (totalSize > availableSpace)
                    {
                        Debug.LogWarning("空间不足，将只加载关键资源");
                        resCtx.ResourcesToLoad = resCtx.ResourcesToLoad
                            .Where(r => r.Priority <= ResourcePriority.High)
                            .ToList();
                    }
                    
                    return true;
                }, "存储检查")
                
                // 并行加载资源组
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceManagerContext>();
                    
                    // 按优先级分组
                    var groups = resCtx.ResourcesToLoad
                        .GroupBy(r => r.Priority)
                        .OrderBy(g => (int)g.Key);
                    
                    foreach (var group in groups)
                    {
                        Debug.Log($"加载 {group.Key} 优先级资源组...");
                        
                        // 并行加载同优先级资源
                        var loadTasks = group.Select(resource => LoadResourceAsync(resource, resCtx));
                        await UniTask.WhenAll(loadTasks);
                        
                        Debug.Log($"{group.Key} 优先级资源组加载完成");
                        await UniTask.Delay(500); // 短暂延迟，模拟分组加载
                    }
                    
                    return true;
                }, "资源加载")
                
                // 资源完整性验证
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceManagerContext>();
                    
                    Debug.Log("验证资源完整性...");
                    await UniTask.Delay(800);
                    
                    int corruptedCount = 0;
                    foreach (var resource in resCtx.LoadedResources)
                    {
                        // 模拟5%的资源损坏率
                        if (UnityEngine.Random.Range(0, 20) == 0)
                        {
                            resource.IsCorrupted = true;
                            corruptedCount++;
                            Debug.LogWarning($"资源 {resource.Id} 损坏");
                        }
                    }
                    
                    Debug.Log($"验证完成，损坏资源: {corruptedCount}");
                    return corruptedCount == 0 || corruptedCount <= resCtx.LoadedResources.Count * 0.1f; // 允许10%的损坏率
                }, "完整性验证")
                
                // 生成资源报告
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceManagerContext>();
                    
                    Debug.Log("=== 资源管理报告 ===");
                    Debug.Log($"计划加载: {resCtx.ResourcesToLoad.Count} 个资源");
                    Debug.Log($"成功加载: {resCtx.LoadedResources.Count} 个资源");
                    Debug.Log($"总大小: {resCtx.LoadedResources.Sum(r => r.Size):F1}MB");
                    
                    var byType = resCtx.LoadedResources.GroupBy(r => r.Type);
                    foreach (var group in byType)
                    {
                        Debug.Log($"  {group.Key}: {group.Count()} 个 ({group.Sum(r => r.Size):F1}MB)");
                    }
                    
                    return true;
                }, "生成报告");
            
            bool result = await builder.ExecuteAsync();
            Debug.Log($"资源管理器执行结果: {result}");
        }
        
        private async UniTask LoadResourceAsync(ResourceInfo resource, ResourceManagerContext context)
        {
            Debug.Log($"  加载资源: {resource.Id}");
            
            // 模拟加载时间（基于资源大小）
            int loadTime = Mathf.RoundToInt(resource.Size * 200); // 每MB需要200ms
            await UniTask.Delay(loadTime);
            
            resource.IsLoaded = true;
            context.LoadedResources.Add(resource);
            
            Debug.Log($"  ✓ {resource.Id} 加载完成 ({resource.Size:F1}MB)");
        }
        
        #endregion
        
        #region UI流程示例
        
        /// <summary>
        /// UI流程示例 - 展示复杂的用户界面交互流程
        /// </summary>
        private async UniTaskVoid ExampleUIFlowAsync()
        {
            Debug.Log("=== 开始UI流程示例 ===");
            
            var uiContext = new UIFlowContext();
            
            var builder = new PipelineBuilder("UIFlow")
                .SetContext(uiContext)
                
                // 显示加载界面
                .AddAction(async ctx =>
                {
                    var uiCtx = ((PipelineContext)ctx).GetContextObject<UIFlowContext>();
                    
                    Debug.Log("显示加载界面");
                    uiCtx.CurrentScreen = "LoadingScreen";
                    uiCtx.LoadingProgress = 0;
                    
                    return true;
                }, "显示加载界面")
                
                // 模拟加载进度
                .AddAction(async ctx =>
                {
                    var uiCtx = ((PipelineContext)ctx).GetContextObject<UIFlowContext>();
                    
                    for (int i = 1; i <= 10; i++)
                    {
                        await UniTask.Delay(300);
                        uiCtx.LoadingProgress = i * 10;
                        Debug.Log($"加载进度: {uiCtx.LoadingProgress}%");
                    }
                    
                    return true;
                }, "更新加载进度")
                
                // 用户登录流程
                .AddAction(async ctx =>
                {
                    var uiCtx = ((PipelineContext)ctx).GetContextObject<UIFlowContext>();
                    
                    Debug.Log("切换到登录界面");
                    uiCtx.CurrentScreen = "LoginScreen";
                    
                    // 模拟用户输入等待
                    Debug.Log("等待用户输入...");
                    await UniTask.Delay(2000);
                    
                    // 模拟登录验证
                    uiCtx.IsUserLoggedIn = UnityEngine.Random.Range(0, 10) > 2; // 80%成功率
                    
                    if (uiCtx.IsUserLoggedIn)
                    {
                        uiCtx.UserId = UnityEngine.Random.Range(1000, 9999);
                        Debug.Log($"登录成功，用户ID: {uiCtx.UserId}");
                    }
                    else
                    {
                        Debug.Log("登录失败");
                    }
                    
                    return uiCtx.IsUserLoggedIn;
                }, "用户登录")
                
                // 主界面初始化
                .AddAction(async ctx =>
                {
                    var uiCtx = ((PipelineContext)ctx).GetContextObject<UIFlowContext>();
                    
                    Debug.Log("初始化主界面");
                    uiCtx.CurrentScreen = "MainScreen";
                    
                    // 模拟UI元素加载
                    var uiElements = new[] { "导航栏", "用户头像", "菜单按钮", "通知中心", "快捷操作" };
                    
                    foreach (var element in uiElements)
                    {
                        await UniTask.Delay(200);
                        uiCtx.LoadedUIElements.Add(element);
                        Debug.Log($"  加载UI元素: {element}");
                    }
                    
                    return true;
                }, "主界面初始化")
                
                // 数据同步和个性化
                .AddAction(async ctx =>
                {
                    var uiCtx = ((PipelineContext)ctx).GetContextObject<UIFlowContext>();
                    
                    Debug.Log("同步用户数据和个性化设置");
                    
                    // 并行执行多个数据同步任务
                    var syncTasks = new[]
                    {
                        SyncUserPreferencesAsync(uiCtx),
                        SyncUserDataAsync(uiCtx),
                        SyncNotificationsAsync(uiCtx),
                        SyncFriendsListAsync(uiCtx)
                    };
                    
                    await UniTask.WhenAll(syncTasks);
                    
                    return true;
                }, "数据同步")
                
                // 显示欢迎信息
                .AddAction(async ctx =>
                {
                    var uiCtx = ((PipelineContext)ctx).GetContextObject<UIFlowContext>();
                    
                    Debug.Log($"欢迎回来，用户 {uiCtx.UserId}！");
                    Debug.Log($"当前界面: {uiCtx.CurrentScreen}");
                    Debug.Log($"已加载UI元素: {uiCtx.LoadedUIElements.Count} 个");
                    Debug.Log($"通知数量: {uiCtx.NotificationCount}");
                    Debug.Log($"好友在线: {uiCtx.OnlineFriendsCount} 人");
                    
                    await UniTask.Delay(1000);
                    return true;
                }, "显示欢迎信息");
            
            bool result = await builder.ExecuteAsync();
            Debug.Log($"UI流程执行结果: {result}");
        }
        
        private async UniTask SyncUserPreferencesAsync(UIFlowContext context)
        {
            Debug.Log("  同步用户偏好设置...");
            await UniTask.Delay(800);
            context.UserPreferences = new Dictionary<string, object>
            {
                ["theme"] = "dark",
                ["language"] = "zh-CN",
                ["notifications"] = true
            };
            Debug.Log("  ✓ 用户偏好设置同步完成");
        }
        
        private async UniTask SyncUserDataAsync(UIFlowContext context)
        {
            Debug.Log("  同步用户数据...");
            await UniTask.Delay(1200);
            // 模拟用户数据同步
            Debug.Log("  ✓ 用户数据同步完成");
        }
        
        private async UniTask SyncNotificationsAsync(UIFlowContext context)
        {
            Debug.Log("  同步通知消息...");
            await UniTask.Delay(600);
            context.NotificationCount = UnityEngine.Random.Range(0, 20);
            Debug.Log($"  ✓ 通知消息同步完成 ({context.NotificationCount} 条)");
        }
        
        private async UniTask SyncFriendsListAsync(UIFlowContext context)
        {
            Debug.Log("  同步好友列表...");
            await UniTask.Delay(900);
            context.OnlineFriendsCount = UnityEngine.Random.Range(0, 50);
            Debug.Log($"  ✓ 好友列表同步完成 ({context.OnlineFriendsCount} 人在线)");
        }
        
        #endregion
        
    }
    
    #region 高级上下文类定义
    
    /// <summary>
    /// 条件执行上下文
    /// </summary>
    public class ConditionalContext : IContextObject
    {
        public int UserLevel { get; set; }
        public bool IsVipUser { get; set; }
        public bool HasCompletedTutorial { get; set; }
        public List<string> ExecutedTasks { get; set; } = new List<string>();
        public List<string> UnlockedFeatures { get; set; } = new List<string>();
        public List<string> VipRewards { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// 动态管道上下文
    /// </summary>
    public class DynamicContext : IContextObject
    {
        public List<TaskConfig> TaskConfigs { get; set; } = new List<TaskConfig>();
        public List<TaskConfig> CompletedTasks { get; set; } = new List<TaskConfig>();
        public DateTime StartTime { get; set; }
    }
    
    /// <summary>
    /// 任务配置
    /// </summary>
    public class TaskConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
    }
    
    /// <summary>
    /// 自定义任务上下文
    /// </summary>
    public class CustomTaskContext : IContextObject
    {
        public int DownloadProgress { get; set; }
        public int RetryCount { get; set; }
        public List<string> ProcessedFiles { get; set; } = new List<string>();
        public string SyncStatus { get; set; } = "未开始";
    }
    
    /// <summary>
    /// 资源管理器上下文
    /// </summary>
    public class ResourceManagerContext : IContextObject
    {
        public List<ResourceInfo> ResourcesToLoad { get; set; } = new List<ResourceInfo>();
        public List<ResourceInfo> LoadedResources { get; set; } = new List<ResourceInfo>();
    }
    
    /// <summary>
    /// 资源信息
    /// </summary>
    public class ResourceInfo
    {
        public string Id { get; set; }
        public ResourceType Type { get; set; }
        public float Size { get; set; } // MB
        public ResourcePriority Priority { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsCorrupted { get; set; }
    }
    
    /// <summary>
    /// 资源类型
    /// </summary>
    public enum ResourceType
    {
        Texture,
        Model,
        Audio,
        Shader,
        Data
    }
    
    /// <summary>
    /// 资源优先级
    /// </summary>
    public enum ResourcePriority
    {
        Critical = 0,
        High = 1,
        Medium = 2,
        Low = 3
    }
    
    /// <summary>
    /// UI流程上下文
    /// </summary>
    public class UIFlowContext : IContextObject
    {
        public string CurrentScreen { get; set; }
        public int LoadingProgress { get; set; }
        public bool IsUserLoggedIn { get; set; }
        public int UserId { get; set; }
        public List<string> LoadedUIElements { get; set; } = new List<string>();
        public Dictionary<string, object> UserPreferences { get; set; }
        public int NotificationCount { get; set; }
        public int OnlineFriendsCount { get; set; }
    }
    
    #endregion
    
    #region 自定义任务节点
    
    /// <summary>
    /// 渐进式下载任务 - 显示下载进度
    /// </summary>
    public class ProgressiveDownloadTask : IPipelineJob
    {
        private readonly string _taskName;
        private readonly int _totalSteps;
        
        public ProgressiveDownloadTask(string taskName, int totalSteps)
        {
            _taskName = taskName;
            _totalSteps = totalSteps;
        }
        
        public async UniTask<bool> RunAsync(IContextObject context)
        {
            var customCtx = ((PipelineContext)context).GetContextObject<CustomTaskContext>();
            
            Debug.Log($"开始渐进式下载: {_taskName}");
            
            for (int i = 1; i <= _totalSteps; i++)
            {
                await UniTask.Delay(100);
                customCtx.DownloadProgress = (int)((float)i / _totalSteps * 100);
                
                if (i % 10 == 0 || i == _totalSteps)
                {
                    Debug.Log($"下载进度: {customCtx.DownloadProgress}%");
                }
            }
            
            Debug.Log($"渐进式下载完成: {_taskName}");
            return true;
        }
    }
    
    /// <summary>
    /// 条件重试任务 - 失败时自动重试
    /// </summary>
    public class ConditionalRetryTask : IPipelineJob
    {
        private readonly string _taskName;
        private readonly int _maxRetries;
        
        public ConditionalRetryTask(string taskName, int maxRetries)
        {
            _taskName = taskName;
            _maxRetries = maxRetries;
        }
        
        public async UniTask<bool> RunAsync(IContextObject context)
        {
            var customCtx = ((PipelineContext)context).GetContextObject<CustomTaskContext>();
            
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                Debug.Log($"尝试执行 {_taskName} (第 {attempt} 次)");
                await UniTask.Delay(800);
                
                customCtx.RetryCount = attempt;
                
                // 模拟70%成功率
                bool success = UnityEngine.Random.Range(0, 10) >= 3;
                
                if (success)
                {
                    Debug.Log($"{_taskName} 执行成功");
                    return true;
                }
                else if (attempt < _maxRetries)
                {
                    Debug.LogWarning($"{_taskName} 执行失败，将重试");
                    await UniTask.Delay(1000 * attempt); // 递增延迟
                }
            }
            
            Debug.LogError($"{_taskName} 在 {_maxRetries} 次尝试后仍然失败");
            return false;
        }
    }
    
    /// <summary>
    /// 批处理任务 - 批量处理多个项目
    /// </summary>
    public class BatchProcessingTask : IPipelineJob
    {
        private readonly string _taskName;
        private readonly string[] _items;
        
        public BatchProcessingTask(string taskName, string[] items)
        {
            _taskName = taskName;
            _items = items;
        }
        
        public async UniTask<bool> RunAsync(IContextObject context)
        {
            var customCtx = ((PipelineContext)context).GetContextObject<CustomTaskContext>();
            
            Debug.Log($"开始批处理: {_taskName} ({_items.Length} 个项目)");
            
            foreach (var item in _items)
            {
                Debug.Log($"  处理项目: {item}");
                await UniTask.Delay(500);
                
                customCtx.ProcessedFiles.Add(item);
                
                // 模拟处理可能失败
                if (UnityEngine.Random.Range(0, 10) == 0) // 10%失败率
                {
                    Debug.LogWarning($"  处理 {item} 失败，跳过");
                    continue;
                }
                
                Debug.Log($"  ✓ {item} 处理完成");
            }
            
            Debug.Log($"批处理完成: {customCtx.ProcessedFiles.Count}/{_items.Length} 个项目成功");
            return customCtx.ProcessedFiles.Count > 0; // 至少有一个成功就算成功
        }
    }
    
    /// <summary>
    /// 定时任务 - 在指定时间后执行
    /// </summary>
    public class TimedTask : IPipelineJob
    {
        private readonly string _taskName;
        private readonly TimeSpan _delay;
        
        public TimedTask(string taskName, TimeSpan delay)
        {
            _taskName = taskName;
            _delay = delay;
        }
        
        public async UniTask<bool> RunAsync(IContextObject context)
        {
            var customCtx = ((PipelineContext)context).GetContextObject<CustomTaskContext>();
            
            Debug.Log($"开始定时任务: {_taskName} (延迟 {_delay.TotalSeconds} 秒)");
            customCtx.SyncStatus = "进行中";
            
            await UniTask.Delay(_delay);
            
            // 模拟定时同步操作
            Debug.Log($"执行定时操作: {_taskName}");
            customCtx.SyncStatus = "已完成";
            
            Debug.Log($"定时任务完成: {_taskName}");
            return true;
        }
    }
    
    #endregion
    
}

#endif
