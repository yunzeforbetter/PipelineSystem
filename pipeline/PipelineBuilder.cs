using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PipelineSystem
{
    /// <summary>
    /// Pipeline构建器
    /// 提供流式API创建任务链
    /// </summary>
    public class PipelineBuilder
    {
        private readonly List<IPipelineJob> _jobs = new List<IPipelineJob>();
        private readonly PipelineContext _context = new PipelineContext();
        private readonly string _pipelineId;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pipelineId">Pipeline唯一标识</param>
        public PipelineBuilder(string pipelineId)
        {
            _pipelineId = string.IsNullOrEmpty(pipelineId) ? Guid.NewGuid().ToString() : pipelineId;
        }

        /// <summary>
        /// 添加任务到Pipeline
        /// </summary>
        /// <param name="job">任务节点</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder AddJob(IPipelineJob job)
        {
            if (job != null)
            {
                _jobs.Add(job);
            }
            return this;
        }

        /// <summary>
        /// 添加动作任务到Pipeline
        /// </summary>
        /// <param name="action">异步任务委托</param>
        /// <param name="name">任务名称</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder AddAction(Func<IContextObject, UniTask<bool>> action, string name = null)
        {
            return AddJob(new ActionTaskNode(action, name));
        }

        /// <summary>
        /// 添加泛型任务到Pipeline
        /// </summary>
        /// <typeparam name="T">上下文对象类型</typeparam>
        /// <param name="action">处理特定上下文的异步任务</param>
        /// <param name="name">任务名称</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder AddTypedAction<T>(Func<T, UniTask<bool>> action, string name = null) where T : IContextObject
        {
            return AddJob(new GenericTaskNode<T>(action, name));
        }

        /// <summary>
        /// 设置上下文对象
        /// </summary>
        /// <param name="contextObject">上下文对象</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder SetContext(IContextObject contextObject)
        {
            _context.SetContextObject(contextObject);
            return this;
        }

        /// <summary>
        /// 设置或更新上下文对象
        /// </summary>
        /// <param name="contextObject">上下文对象</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder SetOrUpdateContext(IContextObject contextObject)
        {
            _context.SetOrUpdateContextObject(contextObject);
            return this;
        }

        /// <summary>
        /// 添加延迟任务
        /// </summary>
        /// <param name="milliseconds">延迟毫秒数</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder AddDelay(int milliseconds)
        {
            return AddAction(async context =>
            {
                await UniTask.Delay(milliseconds, cancellationToken: _context.CancellationToken);
                return true;
            }, $"延迟{milliseconds}毫秒");
        }

        /// <summary>
        /// 添加条件等待任务
        /// </summary>
        /// <param name="predicate">条件判断函数</param>
        /// <param name="timeoutMilliseconds">超时毫秒数</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder AddWaitUntil(Func<bool> predicate, int timeoutMilliseconds = 30000)
        {
            return AddAction(async context =>
            {
                try
                {
                   await UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, _context.CancellationToken);
                   return true;
                }
                catch (TimeoutException)
                {
                    Debug.LogError($"等待条件超时，超时时间: {timeoutMilliseconds}毫秒");
                    return false;
                }
            }, "等待条件满足");
        }

        /// <summary>
        /// 添加并行任务
        /// </summary>
        /// <param name="jobs">要并行执行的任务</param>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder AddParallel(params IPipelineJob[] jobs)
        {
            return AddAction(async context =>
            {
                if (jobs == null || jobs.Length == 0)
                {
                    return true;
                }

                var tasks = new UniTask<bool>[jobs.Length];
                for (int i = 0; i < jobs.Length; i++)
                {
                    tasks[i] = jobs[i].RunAsync(context);
                }

                bool[] results = await UniTask.WhenAll(tasks);
                return Array.TrueForAll(results, r => r);
            }, "并行任务组");
        }

        /// <summary>
        /// 执行Pipeline
        /// </summary>
        /// <returns>执行结果的异步任务</returns>
        public async UniTask<bool> ExecuteAsync()
        {
            if (_jobs.Count == 0)
            {
                Debug.LogWarning("Pipeline没有任何任务");
                return true;
            }

            try
            {
                // 创建根任务，依次执行所有任务
                var rootJob = new ActionTaskNode(async ctx =>
                {
                    //使用副本进行迭代，防止多个pipeline执行时因jobs变动导致报错
                    foreach (var job in _jobs.ToList())
                    {
                        bool result = await job.RunAsync(ctx);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    return true;
                }, "Pipeline根任务");

                // 执行Pipeline
                return await PipelineSystemManager.GetInstance().ExecutePipelineAsync(
                    _pipelineId,
                    rootJob,
                    _context,
                    _context.CancellationToken
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"执行Pipeline出错: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 获取Pipeline ID
        /// </summary>
        public string PipelineId => _pipelineId;

        /// <summary>
        /// 获取Pipeline上下文
        /// </summary>
        public PipelineContext Context => _context;
        
        /// <summary>
        /// 清除所有任务
        /// </summary>
        /// <returns>构建器实例，用于链式调用</returns>
        public PipelineBuilder ClearJobs()
        {
            _jobs.Clear();
            return this;
        }
    }
} 
