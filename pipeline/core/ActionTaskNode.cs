using Cysharp.Threading.Tasks;
using System;

namespace PipelineSystem
{
    /// <summary>
    /// 基于委托的异步任务节点
    /// 允许使用lambda表达式创建简单任务
    /// </summary>
    public class ActionTaskNode : AbsPipelineJob
    {
        private readonly Func<IContextObject, UniTask<bool>> _action;
        private readonly string _name;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="action">异步任务委托</param>
        /// <param name="name">任务名称，用于调试</param>
        public ActionTaskNode(Func<IContextObject, UniTask<bool>> action, string name = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _name = name ?? "ActionTaskNode";
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public override async UniTask<bool> OnRunAsync(IContextObject context)
        {
            return await _action(context);
        }

        /// <summary>
        /// 返回任务的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"ActionTaskNode: {_name}";
        }
    }
} 
