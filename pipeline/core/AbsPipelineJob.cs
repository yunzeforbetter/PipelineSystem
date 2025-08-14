using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace PipelineSystem
{
    /// <summary>
    /// 管线任务抽象基类
    /// </summary>
    public abstract class AbsPipelineJob : IPipelineJob
    {
        /// <summary>
        /// 前置子任务列表
        /// </summary>
        protected readonly List<IPipelineJob> PreSubTasks = new List<IPipelineJob>();

        /// <summary>
        /// 后置子任务列表
        /// </summary>
        protected readonly List<IPipelineJob> PostSubTasks = new List<IPipelineJob>();

        /// <summary>
        /// 异步执行任务
        /// </summary>
        /// <param name="context">上下文对象</param>
        /// <returns>执行结果的异步任务</returns>
        public async UniTask<bool> RunAsync(IContextObject context)
        {
            // 执行前置任务
            foreach (var task in PreSubTasks)
            {
                bool result = await task.RunAsync(context);
                if (!result)
                {
                    return false;
                }
            }

            // 执行主任务
            bool success = await OnRunAsync(context);
            if (!success)
            {
                return false;
            }

            // 执行后置任务
            foreach (var task in PostSubTasks)
            {
                bool result = await task.RunAsync(context);
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 子类实现的异步执行方法
        /// </summary>
        /// <param name="context">上下文对象</param>
        /// <returns>执行结果的异步任务</returns>
        public abstract UniTask<bool> OnRunAsync(IContextObject context);
    }
}
