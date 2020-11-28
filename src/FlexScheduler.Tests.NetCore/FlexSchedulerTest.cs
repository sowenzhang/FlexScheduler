using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexScheduler.Tests.NetCore
{
    [TestClass]
    public class FlexSchedulerTest
    {
        [TestMethod]
        public void It_should_run_executable_jobs()
        {
            string jobName = "TestJob1";
            // cleanup 
            ConcurrentDictionary<string, int> punchCards = new ConcurrentDictionary<string, int>();
            punchCards.TryAdd(jobName, 0);

            TestScheduler testScheduler = new TestScheduler();

            ExecutableJob job1 = new ExecutableJob((triggerTime, runTimes) =>
            {
                punchCards[jobName] = runTimes;
            })
            {
                TaskScheduler = testScheduler,
                Name = jobName, 
                Schedule = new IntervalJobSchedule()
                {
                    ExitStrategy = new ScheduleExitStrategy() {MaxRun = 5},
                    IntervalInSeconds = 1,
                    TriggerAtStart = true
                }
            };

            using (FlexScheduler.Current.AddJob(job1).Start())
            {
                testScheduler.Start();
                Assert.IsTrue(punchCards[jobName] == 5, $"Expected to have run <{jobName}> 5 times, but only get {punchCards[jobName]}");
            }
        }

        [TestMethod]
        public void It_should_run_async_executable_jobs()
        {
            string jobName = "TestJob2";
            // cleanup 
            ConcurrentDictionary<string, int> punchCards = new ConcurrentDictionary<string, int>();
            punchCards.TryAdd(jobName, 0);

            TestScheduler testScheduler = new TestScheduler();

            AsyncExecutableJob job1 = new AsyncExecutableJob((triggerTime, runTimes) =>
            {
                punchCards[jobName] = runTimes;
                return Task.CompletedTask;
            })
            {
                TaskScheduler = testScheduler,
                Name = jobName, 
                Schedule = new IntervalJobSchedule()
                {
                    ExitStrategy = new ScheduleExitStrategy {MaxRun = 5},
                    IntervalInSeconds = 1,
                    TriggerAtStart = true
                }
            };

            using (FlexScheduler.Current.AddJob(job1).Start())
            {
                testScheduler.Start();
                Assert.IsTrue(punchCards[jobName] == 5, $"Expected to have run <{jobName}> 5 times, but only get {punchCards[jobName]}");
            }
        }

    }
}
