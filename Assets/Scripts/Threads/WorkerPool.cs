#define MAIN_THREAD_WORK

using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TerrainDemo.Threads
{
    public class WorkerPool
    {
        public WorkerPool()
        {
            //Get workers threads count (reserve 1 thread for main thread, Unity purposes etc)
            _threadsCount = Mathf.Max(1, SystemInfo.processorCount - 1);
            _pool = new WorkerBase[_threadsCount];
        }

        private void AddWorker([NotNull] WorkerBase worker)
        {
            if (worker == null) throw new ArgumentNullException("worker");

            if (!_workers.Contains(worker))
                _workers.Add(worker);
        }

        //From main thread
        private void StartWork(WorkerBase worker, WaitCallback workerLogic, WorkerBase.WorkerDataBase data)
        {
            var localWorksCount = Interlocked.Increment(ref _worksCount);
            if (localWorksCount > _maxWorks)
                _maxWorks = localWorksCount;
            _totalWorks++;

            int threadId;
            lock (_pool)
            {
                threadId = Array.FindIndex(_pool, w => w == null);
                if (threadId < 0)
                    throw new InvalidOperationException("There is no free thread in pool");
                _pool[threadId] = worker;
            }

            data.ThreadId = threadId;

#if MAIN_THREAD_WORK
            workerLogic(data);                                                      //Debug work from Main thread
#else
            ThreadPool.QueueUserWorkItem(workerLogic, data);
#endif
        }

        private void FinishWork(WorkerBase.WorkerDataBase data)
        {
            Interlocked.Decrement(ref _worksCount);

            lock (_pool)
            {
                if (_pool[data.ThreadId] == null)
                    Debug.LogError("Thread in pool is empty already");
                _pool[data.ThreadId] = null;
            }
        }

        /// <summary>
        /// Should be called regularly from Main thread to dispatch input/output
        /// </summary>
        public void Update()
        {
            //Dispatch result
            foreach (var workerBase in _workers)
            {
                workerBase.ProcessOutputQueue();
            }

            //Dispatch input
            while (_worksCount < _threadsCount)
            {
                var isInputQueueEmpty = true;
                foreach (var workerBase in _workers)
                {
                    isInputQueueEmpty &= !workerBase.ProcessInputQueue(this);

                    if (_worksCount >= _threadsCount)
                        return;
                }

                if (isInputQueueEmpty)
                    return;
            }
        }

#if UNITY_EDITOR
        internal void ShowInspectorGUI()
        {
            GUILayout.Label(string.Format("Workers {0}, max {1}, total {2}", _worksCount, _maxWorks, _totalWorks));
            EditorGUILayout.LabelField("Threads", _threadsCount.ToString());

            _inspectorGUIPoolToggle = EditorGUILayout.Foldout(_inspectorGUIPoolToggle, "Pool");

            if (_inspectorGUIPoolToggle)
            {
                for (int i = 0; i < _pool.Length; i++)
                {
                    // ReSharper disable InconsistentlySynchronizedField
                    var workerBase = _pool[i];
                    // ReSharper restore InconsistentlySynchronizedField
                    EditorGUILayout.LabelField(i.ToString(), workerBase != null ? workerBase.GetType().Name : "");
                }
            }
        }
#endif

        private readonly int _threadsCount;
        private int _worksCount;
        private int _maxWorks;
        private int _totalWorks;
        private readonly List<WorkerBase> _workers = new List<WorkerBase>();            //Registered workers
        private readonly WorkerBase[] _pool;            //Mostly debug purpose (see workers list in Unity inspector)

        private bool _inspectorGUIPoolToggle;

        public abstract class WorkerBase
        {
            protected readonly WorkerPool _pool;

            protected WorkerBase()
            {
                _pool = Object.FindObjectOfType<ThreadPoolHost>().Pool;         //Todo get rid of FindObjectOfType()
                _pool.AddWorker(this);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pool"></param>
            /// <returns>true - there are some more input items</returns>
            internal abstract bool ProcessInputQueue(WorkerPool pool);

            internal abstract void ProcessOutputQueue();

            public class WorkerDataBase
            {
                public int ThreadId;
            }
        }

        public abstract class WorkerBase<TIn, TOUt> : WorkerBase
        {
            /// <summary>
            /// From Main thread
            /// </summary>
            /// <param name="workData"></param>
            public void AddWork(TIn workData)
            {
                lock (_in)
                {
                    _in.Enqueue(workData);
                }
            }

            /// <summary>
            /// From Main thread
            /// </summary>
            public event Action<TOUt> Completed = delegate { };

            private readonly Queue<TIn> _in = new Queue<TIn>();
            private readonly Queue<TOUt> _out = new Queue<TOUt>();

            /// <summary>
            /// From main thread
            /// </summary>
            /// <param name="pool"></param>
            internal override bool ProcessInputQueue(WorkerPool pool)
            {
                if (_in.Count > 0)
                    lock (_in)
                        if (_in.Count > 0)
                        {
                            var workItem = _in.Dequeue();
                            pool.StartWork(this, WorkerThreadLogic, new WorkerData { Input = workItem });
                            return _in.Count > 0;
                        }

                return false;
            }

            /// <summary>
            /// From main thread
            /// </summary>
            internal override void ProcessOutputQueue()
            {
                TOUt[] result = null;

                if (_out.Count > 0)
                    lock (_out)
                        if (_out.Count > 0)
                        {
                            result = _out.ToArray();
                            _out.Clear();
                        }

                if (result != null)
                    foreach (var oUt in result)
                        Completed(oUt);
            }

            /// <summary>
            /// Transforms input to result
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            protected abstract TOUt WorkerLogic(TIn input);

            private void WorkerThreadLogic(object data)
            {
                //Debug.Log("Begin work " + workId);

                WorkerData input = (WorkerData)data;

                try
                {
                    var result = WorkerLogic(input.Input);

                    lock (_out)
                    {
                        _out.Enqueue(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat("{0} while processing {1}:\n {2}", ex.GetType().Name, input.Input, ex);
                }

                _pool.FinishWork(input);

                //Debug.Log("End work " + workId);
            }

            private class WorkerData : WorkerDataBase
            {
                public TIn Input;
            }
        }
    }
}
