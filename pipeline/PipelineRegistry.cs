using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using UnityEngine;

namespace PipelineSystem
{
    /// <summary>
    /// Pipeline注册表
    /// 允许不同类通过键注册任务到同一条Pipeline上
    /// </summary>
    public class PipelineRegistry
    {
        private static readonly PipelineRegistry _instance = new PipelineRegistry();
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static PipelineRegistry Instance => _instance;
        
        // 管道名称到管道构建器的映射
        private readonly Dictionary<string, PipelineBuilder> _pipelines = new Dictionary<string, PipelineBuilder>();
        
        // 管道名称到任务优先级列表的映射
        private readonly Dictionary<string, SortedList<int, List<IPipelineJob>>> _priorityJobs = 
            new Dictionary<string, SortedList<int, List<IPipelineJob>>>();
            
        /// <summary>
        /// 私有构造函数，确保单例模式
        /// </summary>
        private PipelineRegistry() { }
        
        /// <summary>
        /// 获取指定键的Pipeline构建器
        /// 如果不存在则创建新的
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <returns>Pipeline构建器</returns>
        public PipelineBuilder GetPipeline(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Pipeline键不能为空");
                return null;
            }
            
            if (!_pipelines.TryGetValue(key, out var pipeline))
            {
                pipeline = new PipelineBuilder(key);
                _pipelines[key] = pipeline;
                
                // 同时创建优先级任务列表
                _priorityJobs[key] = new SortedList<int, List<IPipelineJob>>();
            }
            
            return pipeline;
        }
        
        /// <summary>
        /// 注册任务到指定的Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <param name="job">任务节点</param>
        /// <returns>是否成功注册</returns>
        public bool RegisterJob(string key, IPipelineJob job)
        {
            if (job == null || string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            GetPipeline(key).AddJob(job);
            return true;
        }
        
        /// <summary>
        /// 按优先级注册任务到Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <param name="job">任务节点</param>
        /// <param name="priority">优先级，数字越小优先级越高</param>
        /// <returns>是否成功注册</returns>
        public bool RegisterJobWithPriority(string key, IPipelineJob job, int priority = 0)
        {
            if (job == null || string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            if (!_priorityJobs.TryGetValue(key, out var priorityList))
            {
                GetPipeline(key); // 确保Pipeline和优先级列表已创建
                priorityList = _priorityJobs[key];
            }
            
            if (!priorityList.TryGetValue(priority, out var jobList))
            {
                jobList = new List<IPipelineJob>();
                priorityList[priority] = jobList;
            }
            
            jobList.Add(job);
            return true;
        }
        
        /// <summary>
        /// 注册动作任务到Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <param name="action">异步任务委托</param>
        /// <param name="name">任务名称</param>
        /// <returns>是否成功注册</returns>
        public bool RegisterAction(string key, Func<IContextObject, UniTask<bool>> action, string name = null)
        {
            if (action == null || string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            return RegisterJob(key, new ActionTaskNode(action, name));
        }
        
        /// <summary>
        /// 按优先级注册动作任务到Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <param name="action">异步任务委托</param>
        /// <param name="priority">优先级，数字越小优先级越高</param>
        /// <param name="name">任务名称</param>
        /// <returns>是否成功注册</returns>
        public bool RegisterActionWithPriority(string key, Func<IContextObject, UniTask<bool>> action, int priority = 0, string name = null)
        {
            if (action == null || string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            return RegisterJobWithPriority(key, new ActionTaskNode(action, name), priority);
        }
        
        /// <summary>
        /// 构建并获取按优先级排序的Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <returns>Pipeline构建器</returns>
        public PipelineBuilder BuildPriorityPipeline(string key)
        {
            if (!_priorityJobs.TryGetValue(key, out var priorityList))
            {
                Debug.LogWarning($"没有找到键为 {key} 的优先级任务");
                return null;
            }
            
            var builder = GetPipeline(key);
            
            // 在添加任务前先清除已有任务
            builder.ClearJobs();
            
            // 按优先级添加任务
            foreach (var kvp in priorityList)
            {
                foreach (var job in kvp.Value)
                {
                    builder.AddJob(job);
                }
            }
            
            return builder;
        }
        
        /// <summary>
        /// 异步执行指定键的Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <returns>执行结果的异步任务</returns>
        public async UniTask<bool> ExecuteAsync(string key)
        {
            var pipeline = GetPipeline(key);
            if (pipeline == null)
            {
                Debug.LogError($"未找到键为 {key} 的Pipeline");
                return false;
            }
            
            return await pipeline.ExecuteAsync();
        }
        
        /// <summary>
        /// 异步执行按优先级排序的Pipeline
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <returns>执行结果的异步任务</returns>
        public async UniTask<bool> ExecutePriorityPipelineAsync(string key)
        {
            // 先取消当前正在运行的Pipeline
            CancelPipeline(key); 
            // 构建按优先级排序的Pipeline
            var pipeline = BuildPriorityPipeline(key);
            if (pipeline == null)
            {
                Debug.LogError($"未找到键为 {key} 的Pipeline");
                return false;
            }
            
            return await pipeline.ExecuteAsync();
        }
        
        /// <summary>
        /// 设置上下文对象
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <param name="contextObject">上下文对象</param>
        /// <returns>是否成功设置</returns>
        public bool SetContextObject(string key, IContextObject contextObject)
        {
            if (contextObject == null || string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            var pipeline = GetPipeline(key);
            pipeline.SetContext(contextObject);
            return true;
        }
        
        /// <summary>
        /// 设置或更新上下文对象
        /// </summary>
        /// <param name="key">Pipeline键</param>
        /// <param name="contextObject">上下文对象</param>
        /// <returns>是否成功设置</returns>
        public bool SetOrUpdateContextObject(string key, IContextObject contextObject)
        {
            if (contextObject == null || string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            var pipeline = GetPipeline(key);
            pipeline.SetOrUpdateContext(contextObject);
            return true;
        }
        
        /// <summary>
        /// 取消指定Pipeline的执行
        /// </summary>
        /// <param name="key">Pipeline键</param>
        public void CancelPipeline(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            
            var pipeline = GetPipeline(key);
            pipeline.Context.Cancel();
            
            PipelineSystemManager.GetInstance().CancelPipeline(key);
        }

        /// <summary>
        /// 清理指定Pipeline及其任务
        /// </summary>
        /// <param name="key"></param>
        public void Clear(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_pipelines.TryGetValue(key, out var pipeline))
            {
                pipeline.Context.Cancel();
                PipelineSystemManager.GetInstance().CancelPipeline(key);
                _pipelines.Remove(key);
                _priorityJobs.Remove(key);
            }
        }

        /// <summary>
        /// 清除所有Pipeline
        /// </summary>
        public void ClearAll()
        {
            foreach (var pipeline in _pipelines.Values)
            {
                pipeline.Context.Cancel();
            }
            
            _pipelines.Clear();
            _priorityJobs.Clear();
            
            PipelineSystemManager.GetInstance().CancelAllPipelines();
        }
    }
} 
