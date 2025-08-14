using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PipelineSystem
{
    /// <summary>
    /// Pipeline上下文
    /// 用于在Pipeline执行过程中共享数据
    /// </summary>
    public class PipelineContext : IContextObject
    {
        // 使用线程安全的字典存储上下文对象
        private readonly ConcurrentDictionary<System.Type, IContextObject> _contextObjects = new ConcurrentDictionary<System.Type, IContextObject>();
        
        // 存储异步操作的取消令牌源
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public PipelineContext()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// 清空所有上下文对象
        /// </summary>
        public void ClearAllContext()
        {
            _contextObjects.Clear();
        }
        
        /// <summary>
        /// 设置上下文对象
        /// </summary>
        public void SetContextObject(IContextObject contextObject)
        {
            if (contextObject == null)
                throw new System.ArgumentNullException("contextObject");

            var type = contextObject.GetType();
            if (!_contextObjects.TryAdd(type, contextObject))
            {
                throw new System.Exception($"Context object {type} is already existed.");
            }
        }
        
        /// <summary>
        /// 尝试设置上下文对象，如果已存在则更新
        /// </summary>
        public void SetOrUpdateContextObject(IContextObject contextObject)
        {
            if (contextObject == null)
                throw new System.ArgumentNullException("contextObject");

            var type = contextObject.GetType();
            _contextObjects[type] = contextObject;
        }
        
        /// <summary>
        /// 获取上下文对象
        /// </summary>
        public T GetContextObject<T>() where T : IContextObject
        {
            var type = typeof(T);
            if (_contextObjects.TryGetValue(type, out IContextObject contextObject))
            {
                return (T)contextObject;
            }
            else
            {
                return default;
            }
        }
        
        /// <summary>
        /// 尝试获取上下文对象
        /// </summary>
        public bool TryGetContextObject<T>(out T contextObject) where T : IContextObject
        {
            var type = typeof(T);
            if (_contextObjects.TryGetValue(type, out IContextObject obj))
            {
                contextObject = (T)obj;
                return true;
            }
            
            contextObject = default;
            return false;
        }
        
        /// <summary>
        /// 移除上下文对象
        /// </summary>
        public bool RemoveContextObject<T>() where T : IContextObject
        {
            var type = typeof(T);
            return _contextObjects.TryRemove(type, out _);
        }
        
        /// <summary>
        /// 获取取消令牌
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        
        /// <summary>
        /// 取消所有异步操作
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
        
        /// <summary>
        /// 重置取消令牌
        /// </summary>
        public void ResetCancellation()
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// 等待直到条件满足
        /// </summary>
        public async UniTask WaitUntil(System.Func<bool> predicate)
        {
            await UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, _cancellationTokenSource.Token);
        }
        
        /// <summary>
        /// 延迟指定时间
        /// </summary>
        public async UniTask Delay(int milliseconds)
        {
            await UniTask.Delay(milliseconds, cancellationToken: _cancellationTokenSource.Token);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _contextObjects.Clear();
        }
    }
}

