using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace FlexScheduler
{
    /// <summary>
    /// An easy way to use the scheduler. Simply add jobs and then call Start.
    /// Once the scheduler starts, do not dispose the instance. However when
    /// the application exists, call Dispose to stop any running timer. 
    /// </summary>
    public class FlexScheduler : IDisposable
    {
        private static FlexScheduler _instance;
        private bool _disposed;
        private readonly ConcurrentDictionary<string, Job> _jobs = new ConcurrentDictionary<string, Job>();
        private bool _started;
        private IDisposable _observableDisposable;

        protected FlexScheduler()
        { }

        public static FlexScheduler Current
        {
            get
            {
                if (_instance == null)
                {
                    var temp = new FlexScheduler();
                    if (Interlocked.CompareExchange(ref _instance, temp, null) != null)
                    {
                        temp.Dispose();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Add a new job to the scheduler. 
        /// </summary>
        /// <param name="job">The job to add</param>
        /// <returns>the current instance of FlexScheduler</returns>
        public FlexScheduler AddJob(Job job)
        {
            if (_jobs.ContainsKey(job.Key) == false)
            {
                _jobs.TryAdd(job.Key, job);
            }

            return this;
        }

        /// <summary>
        /// Start all jobs defined in the scheduler.
        /// Once starts, calling again will not have any effect. 
        /// </summary>
        /// <returns>the current instance of FlexScheduler</returns>
        public FlexScheduler Start()
        {
            if (!_started)
            {
                _started = true;
                _observableDisposable = _jobs
                    .Values
                    .ToObservable()
                    .Subscribe(async a =>
                    {
                        if (a.Job is ExecutableJob executable)
                        {
                            executable.Execute(a.TriggerTime, a.RunTimes);
                        }
                        else if (a.Job is AsyncExecutableJob asycExecutable)
                        {
                            await asycExecutable.ExecuteAsync(a.TriggerTime, a.RunTimes);
                        }
                    });
            }

            return this;
        }

        /// <summary>
        /// Stops all jobs defined in the FlexScheduler 
        /// </summary>
        public void Dispose() => this.Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            _observableDisposable?.Dispose();
            _instance = null;
            _disposed = true;
            _started = false;
        }
    }
}