using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlexScheduler
{
    /// <summary>
    /// The basic Job with no action to do. This is the template of a job, which can be translated to a scheduling item. 
    /// </summary>
    public class Job
    {
        public JobSchedule Schedule { get; set; }

        /// <summary>
        /// The name of the job
        /// </summary>
        public virtual string Name { get; set; }

        public virtual string Key
        {
            get
            {
                if (Schedule != null)
                {
                    return $"{Schedule.GetType().Name}_{Name}";
                }

                return Name;
            }
        }

        /// <summary>
        /// specifies a task scheduler if needed, by default, it's NewThreadScheduler. 
        /// </summary>
        /// <remarks>
        /// This cannot be serialized and deserialized, however, we can use an Enum if we really want to carry this in configuration.
        /// </remarks>
        [JsonIgnore]
        public IScheduler TaskScheduler { get; set; }
    }

    /// <summary>
    /// An executable job, which contains an action to execute. 
    /// </summary>
    /// <remarks>
    /// This is useful if using FlexScheduler to manage all jobs. 
    /// </remarks>
    public class ExecutableJob : Job
    {
        public ExecutableJob(Action<DateTimeOffset, int> execute)
        {
            Execute = execute;
        }

        /// <summary>
        /// The action to execute. The 1st argument is the triggering time, and 2nd argument is the run times of the job. 
        /// </summary>
        public Action<DateTimeOffset, int> Execute { get; }
    }

    /// <summary>
    /// An executable job, which contains an action to execute. 
    /// </summary>
    /// <remarks>
    /// This is useful if using FlexScheduler to manage all jobs.
    /// Generally, there is no need to have an async job, as each job is in a separate thread already when running in the scheduler 
    /// </remarks>
    public class AsyncExecutableJob : Job
    {
        public AsyncExecutableJob(Func<DateTimeOffset, int, Task> execute)
        {
            ExecuteAsync = execute;
        }

        /// <summary>
        /// The action to execute. The 1st argument is the triggering time, and 2nd argument is the run times of the job. 
        /// </summary>
        public Func<DateTimeOffset, int, Task> ExecuteAsync { get; }
    }
}