
using ELEX.Common.Utility;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Game.Common;

namespace PipelineSystem
{
    /// <summary>
    /// Pipeline系统管理器
    /// 用于管理和执行异步Pipeline流程
    /// </summary>
    public class PipelineSystemManager : MonoBehaviourSingle<PipelineSystemManager>
    {
        // 记录正在执行的Pipeline
        private readonly Dictionary<string, CancellationTokenSource> _runningPipelines = new Dictionary<string, CancellationTokenSource>();
        
        /// <summary>
        /// 异步执行Pipeline
        /// </summary>
        /// <param name="pipelineId">Pipeline唯一标识</param>
        /// <param name="rootJob">根任务</param>
        /// <param name="context">上下文对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public async UniTask<bool> ExecutePipelineAsync(string pipelineId, IPipelineJob rootJob, IContextObject context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pipelineId))
            {
                Log.PipelineSystem.Error("Pipeline ID不能为空");
                return false;
            }

            if (rootJob == null)
            {
                Log.PipelineSystem.Error("根任务不能为空");
                return false;
            }

            // 创建Pipeline取消源
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                // 注册到运行列表
                _runningPipelines[pipelineId] = cts;

                // 执行Pipeline
                Log.PipelineSystem.Debug($"开始执行Pipeline: {pipelineId}");
                bool result = await rootJob.RunAsync(context).AttachExternalCancellation(cts.Token);
                Log.PipelineSystem.Debug($"Pipeline执行完成: {pipelineId}, 结果: {result}");
                
                return result;
            }
            catch (System.OperationCanceledException)
            {
                Log.PipelineSystem.Debug($"Pipeline被取消: {pipelineId}");
                return false;
            }
            catch (System.Exception ex)
            {
                Log.PipelineSystem.Error($"Pipeline执行出错: {pipelineId}, 错误: {ex}");
                return false;
            }
            finally
            {
                // 清理资源
                _runningPipelines.Remove(pipelineId);
                cts.Dispose();
            }
        }
        
        /// <summary>
        /// 取消正在执行的Pipeline
        /// </summary>
        /// <param name="pipelineId">Pipeline唯一标识</param>
        /// <returns>是否成功取消</returns>
        public bool CancelPipeline(string pipelineId)
        {
            if (_runningPipelines.TryGetValue(pipelineId, out var cts))
            {
                cts.Cancel();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 取消所有正在执行的Pipeline
        /// </summary>
        public void CancelAllPipelines()
        {
            foreach (var cts in _runningPipelines.Values)
            {
                cts.Cancel();
            }
            
            _runningPipelines.Clear();
        }
        
        /// <summary>
        /// 检查Pipeline是否正在执行
        /// </summary>
        /// <param name="pipelineId">Pipeline唯一标识</param>
        /// <returns>是否正在执行</returns>
        public bool IsPipelineRunning(string pipelineId)
        {
            return _runningPipelines.ContainsKey(pipelineId);
        }
        
        /// <summary>
        /// 获取正在运行的Pipeline数量
        /// </summary>
        public int RunningPipelineCount => _runningPipelines.Count;
        
        /// <summary>
        /// 组件销毁时清理资源
        /// </summary>
        public void OnDestroy()
        {
            CancelAllPipelines();
        }
    }
}
