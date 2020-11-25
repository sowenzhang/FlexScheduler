using System;
using System.Threading.Tasks;

namespace FlexScheduler
{
    /// <summary>
    /// The basic Job with no action to do. This is the template of a job, which can be translated to a scheduling item. 
    /// </summary>
    public class Job
    {
        public JobSchedule Schedule { get; set; }
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