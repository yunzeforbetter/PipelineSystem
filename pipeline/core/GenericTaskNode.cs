using Cysharp.Threading.Tasks;

using Game.Common;

using System;

namespace PipelineSystem
{
    /// <summary>
    /// 泛型任务节点
    /// 允许强类型访问特定的上下文对象
    /// </summary>
    /// <typeparam name="T">上下文对象类型，必须实现IContextObject接口</typeparam>
    public class GenericTaskNode<T> : AbsPipelineJob where T : IContextObject
    {
        private readonly Func<T, UniTask<bool>> _action;
        private readonly string _name;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="action">处理特定上下文对象的异步任务</param>
        /// <param name="name">任务名称，用于调试</param>
        public GenericTaskNode(Func<T, UniTask<bool>> action, string name = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _name = name ?? $"GenericTaskNode<{typeof(T).Name}>";
        }

        /// <summary>
        /// 执行任务，会从上下文中获取指定类型的对象
        /// </summary>
        public override async UniTask<bool> OnRunAsync(IContextObject context)
        {
            // 尝试将一般上下文转为特定类型
            if (context is PipelineContext pipelineContext)
            {
                try
                {
                    // 获取特定类型的上下文对象
                    T specificContext = pipelineContext.GetContextObject<T>();
                    return await _action(specificContext);
                }
                catch (Exception ex)
                {
                    Log.PipelineSystem.Error($"执行泛型任务失败: {_name}, 错误: {ex.Message}");
                    return false;
                }
            }
            else if (context is T specificContext)
            {
                return await _action(specificContext);
            }
            else
            {
                Log.PipelineSystem.Error($"泛型任务节点需要类型 {typeof(T).Name} 的上下文对象，但提供的是 {context?.GetType().Name ?? "null"}");
                return false;
            }
        }

        /// <summary>
        /// 返回任务的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"GenericTaskNode<{typeof(T).Name}>: {_name}";
        }
    }
} 
