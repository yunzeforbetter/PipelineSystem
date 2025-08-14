
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
    /// Pipeline系统使用示例类
    /// 展示了Pipeline系统的主要使用方式和实际应用场景
    /// </summary>
    public class PipelineSystemExample : MonoBehaviour
    {
        [Header("示例控制")]
        public bool runBasicExample = true;
        public bool runRegistryExample = true;
        public bool runGameInitExample = true;
        public bool runResourceLoadExample = true;
        public bool runLoginFlowExample = true;
        public bool runBattleSystemExample = true;
        public bool runParallelExample = true;
        public bool runErrorHandlingExample = true;

        private void Start()
        {
            RunSelectedExamples().Forget();
        }

        private async UniTaskVoid RunSelectedExamples()
        {
            Debug.Log("=== PipelineSystem 示例开始 ===");

            if (runBasicExample)
            {
                ExampleBasicPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runRegistryExample)
            {
                ExampleRegistryPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runGameInitExample)
            {
                ExampleGameInitPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runResourceLoadExample)
            {
                ExampleResourceLoadPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runLoginFlowExample)
            {
                ExampleLoginFlowPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runBattleSystemExample)
            {
                ExampleBattleSystemPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runParallelExample)
            {
                ExampleParallelTasksPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            if (runErrorHandlingExample)
            {
                ExampleErrorHandlingPipelineAsync().Forget();
                await UniTask.Delay(1000);
            }

            Debug.Log("=== 所有示例执行完成 ===");
        }

        #region 基础示例

        /// <summary>
        /// 基本的Pipeline使用示例
        /// </summary>
        private async UniTaskVoid ExampleBasicPipelineAsync()
        {
            Debug.Log("=== 开始基本Pipeline示例 ===");

            // 创建一个Pipeline构建器
            var builder = new PipelineBuilder("BasicExample");

            // 添加任务
            builder.AddAction(async context =>
            {
                Debug.Log("执行基本任务1 - 初始化");
                await UniTask.Delay(1000);
                return true;
            }, "基本任务1");

            builder.AddAction(async context =>
            {
                Debug.Log("执行基本任务2 - 配置");
                await UniTask.Delay(500);
                return true;
            }, "基本任务2");

            // 添加一个延迟任务
            builder.AddDelay(500);

            // 添加最后一个任务
            builder.AddAction(async context =>
            {
                Debug.Log("执行基本任务3 - 完成");
                await UniTask.CompletedTask;
                return true;
            }, "基本任务3");

            // 执行Pipeline
            bool result = await builder.ExecuteAsync();
            Debug.Log($"基本Pipeline执行结果: {result}");
        }

        /// <summary>
        /// 使用注册表的Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleRegistryPipelineAsync()
        {
            Debug.Log("=== 开始注册表Pipeline示例 ===");

            // 定义Pipeline键
            const string pipelineKey = "RegistryExample";

            // 创建一个简单的上下文数据对象
            var dataContext = new SimpleDataContext { Value = "初始数据", Counter = 0 };

            // 设置到注册表
            PipelineRegistry.Instance.SetContextObject(pipelineKey, dataContext);

            // 注册任务到管线，注意这些任务可以在不同的类中分散注册
            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var data = ((PipelineContext)context).GetContextObject<SimpleDataContext>();
                data.Counter++;
                Debug.Log($"任务1：数据值 = {data.Value}, 计数器 = {data.Counter}");

                // 修改数据
                data.Value = "任务1修改后的数据";
                await UniTask.Delay(500);
                return true;
            }, 0, "任务1");

            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var data = ((PipelineContext)context).GetContextObject<SimpleDataContext>();
                data.Counter++;
                Debug.Log($"任务2：数据值 = {data.Value}, 计数器 = {data.Counter}");

                // 再次修改数据
                data.Value = "任务2修改后的数据";
                await UniTask.Delay(300);
                return true;
            }, 1, "任务2");

            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var data = ((PipelineContext)context).GetContextObject<SimpleDataContext>();
                data.Counter++;
                Debug.Log($"任务3：最终数据值 = {data.Value}, 计数器 = {data.Counter}");
                await UniTask.Delay(200);
                return true;
            }, 2, "任务3");

            // 执行按优先级排序的Pipeline
            bool result = await PipelineRegistry.Instance.ExecutePriorityPipelineAsync(pipelineKey);
            Debug.Log($"注册表Pipeline执行结果: {result}");

            // 清理
            PipelineRegistry.Instance.Clear(pipelineKey);
        }

        #endregion

        #region 游戏初始化示例

        /// <summary>
        /// 游戏初始化Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleGameInitPipelineAsync()
        {
            Debug.Log("=== 开始游戏初始化Pipeline示例 ===");

            const string GAME_INIT_PIPELINE = "GameInitialization";

            // 创建初始化上下文
            var initContext = new GameInitContext();
            PipelineRegistry.Instance.SetContextObject(GAME_INIT_PIPELINE, initContext);

            // 模拟不同模块的初始化任务
            RegisterGameInitTasks(GAME_INIT_PIPELINE);

            // 执行初始化流程
            bool success = await PipelineRegistry.Instance.ExecutePriorityPipelineAsync(GAME_INIT_PIPELINE);

            if (success)
            {
                Debug.Log($"游戏初始化完成！耗时: {initContext.ElapsedTime:F2}秒");
                Debug.Log($"已初始化模块: {string.Join(", ", initContext.InitializedModules)}");
            }
            else
            {
                Debug.LogError("游戏初始化失败！");
            }

            // 清理
            PipelineRegistry.Instance.Clear(GAME_INIT_PIPELINE);
        }

        private void RegisterGameInitTasks(string pipelineKey)
        {
            // 配置系统初始化 (优先级最高)
            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var ctx = ((PipelineContext)context).GetContextObject<GameInitContext>();
                var startTime = Time.realtimeSinceStartup;

                Debug.Log("初始化配置系统...");
                await UniTask.Delay(800);

                ctx.InitializedModules.Add("配置系统");
                ctx.ElapsedTime += Time.realtimeSinceStartup - startTime;
                return true;
            }, 0, "配置系统初始化");

            // 资源系统初始化
            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var ctx = ((PipelineContext)context).GetContextObject<GameInitContext>();
                var startTime = Time.realtimeSinceStartup;

                Debug.Log("初始化资源系统...");
                await UniTask.Delay(1200);

                ctx.InitializedModules.Add("资源系统");
                ctx.ElapsedTime += Time.realtimeSinceStartup - startTime;
                return true;
            }, 1, "资源系统初始化");

            // UI系统初始化
            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var ctx = ((PipelineContext)context).GetContextObject<GameInitContext>();
                var startTime = Time.realtimeSinceStartup;

                Debug.Log("初始化UI系统...");
                await UniTask.Delay(600);

                ctx.InitializedModules.Add("UI系统");
                ctx.ElapsedTime += Time.realtimeSinceStartup - startTime;
                return true;
            }, 2, "UI系统初始化");

            // 网络系统初始化
            PipelineRegistry.Instance.RegisterActionWithPriority(pipelineKey, async context =>
            {
                var ctx = ((PipelineContext)context).GetContextObject<GameInitContext>();
                var startTime = Time.realtimeSinceStartup;

                Debug.Log("初始化网络系统...");
                await UniTask.Delay(1000);

                ctx.InitializedModules.Add("网络系统");
                ctx.ElapsedTime += Time.realtimeSinceStartup - startTime;
                return true;
            }, 3, "网络系统初始化");
        }

        #endregion

        #region 资源加载示例

        /// <summary>
        /// 资源加载Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleResourceLoadPipelineAsync()
        {
            Debug.Log("=== 开始资源加载Pipeline示例 ===");

            var resourceContext = new ResourceLoadContext();

            var builder = new PipelineBuilder("ResourceLoad")
                .SetContext(resourceContext)

                // 检查网络连接
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceLoadContext>();
                    Debug.Log("检查网络连接...");

                    await UniTask.Delay(500);
                    resCtx.IsNetworkAvailable = UnityEngine.Random.Range(0, 10) > 1; // 90%成功率

                    if (!resCtx.IsNetworkAvailable)
                    {
                        Debug.LogWarning("网络连接不可用，使用离线资源");
                    }

                    return true;
                }, "网络检查")

                // 加载基础资源
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceLoadContext>();
                    Debug.Log("加载基础资源...");

                    // 模拟加载过程
                    for (int i = 0; i < 5; i++)
                    {
                        await UniTask.Delay(200);
                        resCtx.LoadedResourceCount++;
                        Debug.Log($"已加载资源: {resCtx.LoadedResourceCount}/10");
                    }

                    return true;
                }, "基础资源加载")

                // 条件加载网络资源
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceLoadContext>();

                    if (resCtx.IsNetworkAvailable)
                    {
                        Debug.Log("加载网络资源...");
                        for (int i = 0; i < 5; i++)
                        {
                            await UniTask.Delay(300);
                            resCtx.LoadedResourceCount++;
                            Debug.Log($"已加载资源: {resCtx.LoadedResourceCount}/10");
                        }
                    }
                    else
                    {
                        Debug.Log("跳过网络资源加载");
                        resCtx.LoadedResourceCount = 10; // 直接设置为完成状态
                    }

                    return true;
                }, "网络资源加载")

                // 等待资源加载完成
                .AddWaitUntil(() => resourceContext.LoadedResourceCount >= 10, 10000)

                // 验证资源完整性
                .AddAction(async ctx =>
                {
                    var resCtx = ((PipelineContext)ctx).GetContextObject<ResourceLoadContext>();
                    Debug.Log("验证资源完整性...");

                    await UniTask.Delay(800);
                    resCtx.IsLoadComplete = resCtx.LoadedResourceCount >= 10;

                    if (resCtx.IsLoadComplete)
                    {
                        Debug.Log("所有资源加载完成且验证通过");
                    }

                    return resCtx.IsLoadComplete;
                }, "资源验证");

            bool result = await builder.ExecuteAsync();
            Debug.Log($"资源加载Pipeline结果: {result}");
        }

        #endregion

        #region 用户登录流程示例

        /// <summary>
        /// 用户登录流程Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleLoginFlowPipelineAsync()
        {
            Debug.Log("=== 开始用户登录流程Pipeline示例 ===");

            var loginContext = new LoginFlowContext
            {
                Username = "testuser",
                Password = "password123"
            };

            var builder = new PipelineBuilder("LoginFlow")
                .SetContext(loginContext)

                // 输入验证
                .AddAction(async ctx =>
                {
                    var loginCtx = ((PipelineContext)ctx).GetContextObject<LoginFlowContext>();
                    Debug.Log("验证用户输入...");

                    await UniTask.Delay(200);

                    bool isValid = !string.IsNullOrEmpty(loginCtx.Username) &&
                                   !string.IsNullOrEmpty(loginCtx.Password);

                    if (!isValid)
                    {
                        Debug.LogError("用户名或密码不能为空");
                    }

                    return isValid;
                }, "输入验证")

                // 网络认证
                .AddAction(async ctx =>
                {
                    var loginCtx = ((PipelineContext)ctx).GetContextObject<LoginFlowContext>();
                    Debug.Log($"正在认证用户: {loginCtx.Username}...");

                    // 模拟网络请求
                    await UniTask.Delay(2000);

                    // 90%成功率
                    bool authSuccess = UnityEngine.Random.Range(0, 10) > 1;

                    if (authSuccess)
                    {
                        loginCtx.Token = $"token_{Guid.NewGuid():N}";
                        loginCtx.UserId = UnityEngine.Random.Range(1000, 9999);
                        Debug.Log($"认证成功，Token: {loginCtx.Token[..20]}...");
                    }
                    else
                    {
                        Debug.LogError("认证失败：用户名或密码错误");
                    }

                    return authSuccess;
                }, "用户认证")

                // 获取用户档案
                .AddAction(async ctx =>
                {
                    var loginCtx = ((PipelineContext)ctx).GetContextObject<LoginFlowContext>();
                    Debug.Log("获取用户档案...");

                    await UniTask.Delay(1000);

                    loginCtx.UserProfile = new UserProfile
                    {
                        UserId = loginCtx.UserId,
                        DisplayName = $"玩家{loginCtx.UserId}",
                        Level = UnityEngine.Random.Range(1, 100),
                        Experience = UnityEngine.Random.Range(0, 10000)
                    };

                    Debug.Log($"用户档案加载完成：{loginCtx.UserProfile.DisplayName} (等级 {loginCtx.UserProfile.Level})");
                    return true;
                }, "用户档案加载")

                // 初始化游戏数据
                .AddAction(async ctx =>
                {
                    var loginCtx = ((PipelineContext)ctx).GetContextObject<LoginFlowContext>();
                    Debug.Log("初始化游戏数据...");

                    await UniTask.Delay(800);

                    loginCtx.GameData = new Dictionary<string, object>
                    {
                        ["lastLoginTime"] = DateTime.Now,
                        ["sessionId"] = Guid.NewGuid().ToString(),
                        ["isNewUser"] = loginCtx.UserProfile.Level == 1
                    };

                    Debug.Log("游戏数据初始化完成");
                    return true;
                }, "游戏数据初始化")

                // 登录完成
                .AddAction(async ctx =>
                {
                    var loginCtx = ((PipelineContext)ctx).GetContextObject<LoginFlowContext>();
                    Debug.Log($"登录成功！欢迎回来，{loginCtx.UserProfile.DisplayName}！");

                    loginCtx.IsLoggedIn = true;
                    await UniTask.CompletedTask;
                    return true;
                }, "登录完成");

            bool result = await builder.ExecuteAsync();
            Debug.Log($"登录流程Pipeline结果: {result}");
        }

        #endregion

        #region 战斗系统示例

        /// <summary>
        /// 战斗系统Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleBattleSystemPipelineAsync()
        {
            Debug.Log("=== 开始战斗系统Pipeline示例 ===");

            var battleContext = new BattleContext
            {
                BattleId = UnityEngine.Random.Range(1000, 9999),
                PlayerIds = new List<int> { 1, 2, 3, 4 },
                BattleType = BattleType.PvE
            };

            var builder = new PipelineBuilder("BattleSystem")
                .SetContext(battleContext)

                // 战斗准备
                .AddAction(async ctx =>
                {
                    var battleCtx = ((PipelineContext)ctx).GetContextObject<BattleContext>();
                    Debug.Log($"准备战斗 {battleCtx.BattleId} ({battleCtx.BattleType})");

                    await UniTask.Delay(1000);

                    battleCtx.Phase = BattlePhase.Preparation;
                    Debug.Log($"玩家列表: [{string.Join(", ", battleCtx.PlayerIds)}]");
                    return true;
                }, "战斗准备")

                // 加载战斗资源
                .AddAction(async ctx =>
                {
                    var battleCtx = ((PipelineContext)ctx).GetContextObject<BattleContext>();
                    Debug.Log("加载战斗场景和角色资源...");

                    // 模拟资源加载
                    for (int i = 1; i <= 5; i++)
                    {
                        await UniTask.Delay(300);
                        Debug.Log($"加载进度: {i * 20}%");
                    }

                    battleCtx.IsResourceLoaded = true;
                    Debug.Log("战斗资源加载完成");
                    return true;
                }, "资源加载")

                // 等待所有玩家准备就绪
                .AddAction(async ctx =>
                {
                    var battleCtx = ((PipelineContext)ctx).GetContextObject<BattleContext>();
                    Debug.Log("等待所有玩家准备就绪...");

                    // 模拟玩家准备过程
                    for (int i = 0; i < battleCtx.PlayerIds.Count; i++)
                    {
                        await UniTask.Delay(UnityEngine.Random.Range(800, 1500));
                        Debug.Log($"玩家 {battleCtx.PlayerIds[i]} 准备完成");
                    }

                    battleCtx.AllPlayersReady = true;
                    return true;
                }, "等待玩家准备")

                // 开始战斗
                .AddAction(async ctx =>
                {
                    var battleCtx = ((PipelineContext)ctx).GetContextObject<BattleContext>();
                    Debug.Log("战斗开始！");

                    battleCtx.Phase = BattlePhase.InProgress;

                    // 模拟战斗过程
                    for (int round = 1; round <= 5; round++)
                    {
                        Debug.Log($"第 {round} 回合");
                        await UniTask.Delay(1000);

                        // 模拟战斗伤害
                        int damage = UnityEngine.Random.Range(50, 200);
                        battleCtx.TotalDamage += damage;
                    }

                    // 随机战斗结果
                    battleCtx.IsVictory = UnityEngine.Random.Range(0, 2) == 0;
                    battleCtx.Phase = BattlePhase.Ended;

                    Debug.Log($"战斗结束！结果: {(battleCtx.IsVictory ? "胜利" : "失败")}");
                    Debug.Log($"总伤害: {battleCtx.TotalDamage}");

                    return true;
                }, "执行战斗")

                // 结算奖励
                .AddAction(async ctx =>
                {
                    var battleCtx = ((PipelineContext)ctx).GetContextObject<BattleContext>();

                    if (battleCtx.IsVictory)
                    {
                        Debug.Log("计算胜利奖励...");
                        await UniTask.Delay(800);

                        battleCtx.Rewards = new Dictionary<string, int>
                        {
                            ["经验值"] = UnityEngine.Random.Range(100, 500),
                            ["金币"] = UnityEngine.Random.Range(50, 200),
                            ["装备"] = UnityEngine.Random.Range(0, 3)
                        };

                        Debug.Log("奖励分发:");
                        foreach (var reward in battleCtx.Rewards)
                        {
                            Debug.Log($"  {reward.Key}: {reward.Value}");
                        }
                    }
                    else
                    {
                        Debug.Log("战斗失败，无奖励");
                        battleCtx.Rewards = new Dictionary<string, int>();
                    }

                    return true;
                }, "奖励结算");

            bool result = await builder.ExecuteAsync();
            Debug.Log($"战斗系统Pipeline结果: {result}");
        }

        #endregion

        #region 并行任务示例

        /// <summary>
        /// 并行任务Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleParallelTasksPipelineAsync()
        {
            Debug.Log("=== 开始并行任务Pipeline示例 ===");

            var builder = new PipelineBuilder("ParallelTasks");

            // 创建多个并行任务
            var task1 = new ActionTaskNode(async ctx =>
            {
                Debug.Log("并行任务1开始 - 数据处理");
                await UniTask.Delay(2000);
                Debug.Log("并行任务1完成");
                return true;
            }, "数据处理任务");

            var task2 = new ActionTaskNode(async ctx =>
            {
                Debug.Log("并行任务2开始 - 网络请求");
                await UniTask.Delay(1500);
                Debug.Log("并行任务2完成");
                return true;
            }, "网络请求任务");

            var task3 = new ActionTaskNode(async ctx =>
            {
                Debug.Log("并行任务3开始 - 文件操作");
                await UniTask.Delay(1000);
                Debug.Log("并行任务3完成");
                return true;
            }, "文件操作任务");

            var task4 = new ActionTaskNode(async ctx =>
            {
                Debug.Log("并行任务4开始 - 计算密集型操作");
                await UniTask.Delay(2500);
                Debug.Log("并行任务4完成");
                return true;
            }, "计算任务");

            // 添加前置任务
            builder.AddAction(async ctx =>
            {
                Debug.Log("准备并行任务...");
                await UniTask.Delay(500);
                return true;
            }, "并行任务准备");

            // 添加并行执行组
            builder.AddParallel(task1, task2, task3, task4);

            // 添加后置任务
            builder.AddAction(async ctx =>
            {
                Debug.Log("所有并行任务完成，开始汇总结果...");
                await UniTask.Delay(800);
                Debug.Log("结果汇总完成");
                return true;
            }, "结果汇总");

            var startTime = Time.realtimeSinceStartup;
            bool result = await builder.ExecuteAsync();
            var endTime = Time.realtimeSinceStartup;

            Debug.Log($"并行任务Pipeline结果: {result}");
            Debug.Log($"总耗时: {(endTime - startTime):F2}秒 (如果串行执行需要约7秒)");
        }

        #endregion

        #region 错误处理示例

        /// <summary>
        /// 错误处理和重试机制Pipeline示例
        /// </summary>
        private async UniTaskVoid ExampleErrorHandlingPipelineAsync()
        {
            Debug.Log("=== 开始错误处理Pipeline示例 ===");

            var builder = new PipelineBuilder("ErrorHandling");

            // 添加一个稳定的任务
            builder.AddAction(async ctx =>
            {
                Debug.Log("稳定任务执行");
                await UniTask.Delay(500);
                return true;
            }, "稳定任务");

            // 添加一个可能失败的任务（带重试机制）
            builder.AddAction(async ctx =>
            {
                const int maxRetries = 3;
                const string taskName = "不稳定的网络请求";

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        Debug.Log($"{taskName} - 第 {attempt} 次尝试");
                        await UniTask.Delay(800);

                        // 模拟60%的失败率
                        bool success = UnityEngine.Random.Range(0, 10) >= 6;

                        if (success)
                        {
                            Debug.Log($"{taskName} - 第 {attempt} 次尝试成功");
                            return true;
                        }
                        else if (attempt < maxRetries)
                        {
                            Debug.LogWarning($"{taskName} - 第 {attempt} 次尝试失败，将在 {attempt} 秒后重试");
                            await UniTask.Delay(attempt * 1000); // 指数退避
                        }
                        else
                        {
                            Debug.LogError($"{taskName} - 所有 {maxRetries} 次尝试都失败");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{taskName} - 第 {attempt} 次尝试异常: {ex.Message}");
                        if (attempt >= maxRetries)
                        {
                            return false;
                        }
                        await UniTask.Delay(attempt * 1000);
                    }
                }

                return false;
            }, "不稳定任务");

            // 添加一个错误恢复任务
            builder.AddAction(async ctx =>
            {
                Debug.Log("执行错误恢复逻辑");
                await UniTask.Delay(500);
                Debug.Log("系统已恢复正常状态");
                return true;
            }, "错误恢复");

            // 添加最终验证任务
            builder.AddAction(async ctx =>
            {
                Debug.Log("执行系统状态验证");
                await UniTask.Delay(300);

                bool isSystemHealthy = true; // 在实际应用中，这里会检查系统各项指标

                if (isSystemHealthy)
                {
                    Debug.Log("系统状态验证通过");
                }
                else
                {
                    Debug.LogError("系统状态验证失败");
                }

                return isSystemHealthy;
            }, "状态验证");

            bool result = await builder.ExecuteAsync();
            Debug.Log($"错误处理Pipeline结果: {result}");

            if (!result)
            {
                Debug.LogWarning("Pipeline执行失败，但这在错误处理示例中是正常的");
            }
        }

        #endregion

    }

    #region 上下文数据类定义

    /// <summary>
    /// 简单的数据上下文示例
    /// </summary>
    public class SimpleDataContext : IContextObject
    {
        public string Value { get; set; }
        public int Counter { get; set; }
    }

    /// <summary>
    /// 游戏初始化上下文
    /// </summary>
    public class GameInitContext : IContextObject
    {
        public List<string> InitializedModules { get; set; } = new List<string>();
        public float ElapsedTime { get; set; }
        public bool IsInitComplete => InitializedModules.Count >= 4;
    }

    /// <summary>
    /// 资源加载上下文
    /// </summary>
    public class ResourceLoadContext : IContextObject
    {
        public bool IsNetworkAvailable { get; set; }
        public int LoadedResourceCount { get; set; }
        public bool IsLoadComplete { get; set; }
        public List<string> FailedResources { get; set; } = new List<string>();
    }

    /// <summary>
    /// 登录流程上下文
    /// </summary>
    public class LoginFlowContext : IContextObject
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public int UserId { get; set; }
        public UserProfile UserProfile { get; set; }
        public Dictionary<string, object> GameData { get; set; }
        public bool IsLoggedIn { get; set; }
    }

    /// <summary>
    /// 用户档案
    /// </summary>
    public class UserProfile
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
    }

    /// <summary>
    /// 战斗上下文
    /// </summary>
    public class BattleContext : IContextObject
    {
        public int BattleId { get; set; }
        public List<int> PlayerIds { get; set; }
        public BattleType BattleType { get; set; }
        public BattlePhase Phase { get; set; }
        public bool IsResourceLoaded { get; set; }
        public bool AllPlayersReady { get; set; }
        public bool IsVictory { get; set; }
        public int TotalDamage { get; set; }
        public Dictionary<string, int> Rewards { get; set; }
    }

    /// <summary>
    /// 战斗类型
    /// </summary>
    public enum BattleType
    {
        PvE,    // 玩家对环境
        PvP,    // 玩家对玩家
        Boss,   // Boss战
        Arena   // 竞技场
    }

    /// <summary>
    /// 战斗阶段
    /// </summary>
    public enum BattlePhase
    {
        None,
        Preparation,    // 准备阶段
        InProgress,     // 进行中
        Ended          // 结束
    }
}
    
    #endregion

#endif
