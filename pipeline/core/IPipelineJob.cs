using Cysharp.Threading.Tasks;

namespace PipelineSystem
{

    public interface IPipelineJob
    {
        /// <summary>
        /// 异步执行管线任务
        /// </summary>
        /// <param name="context">上下文对象</param>
        /// <returns>执行结果的异步任务</returns>
        UniTask<bool> RunAsync(IContextObject context);
    }

}
