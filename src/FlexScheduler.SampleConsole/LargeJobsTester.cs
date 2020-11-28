using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FlexScheduler.SampleConsole
{
    public class LargeJobsTester
    {
        // DO NOT set this number too big, as the Observable.Generate will cause StackOverflowException, see this page for details: 
        // https://stackoverflow.com/questions/13462713/why-does-observable-generate-throw-system-stackoverflowexception
        // in a real use case, maybe have multiple separate collections instead of having one single
        private const int TotalJobs = 500;
        private const int MaxRunTimes = 1;
        readonly ConcurrentBag<string> _outputs = new ConcurrentBag<string>();
        public void Run()
        {
            Stopwatch watch = new Stopwatch();
            IList<ExecutableJob> jobs = new List<ExecutableJob>();

            for (var a = 0; a < TotalJobs; a++)
            {
                jobs.Add(CreateJob(a));
            }

            watch.Start();

            IDisposable disposable = null;

            Console.WriteLine("Check your debug window to all the output");
            IObservable<JobObservable<ExecutableJob>> obs = jobs.ToObservable();
            while (_outputs.Count < TotalJobs * MaxRunTimes)
            {
                disposable = obs
                    .Subscribe(onNext: a => a.Job.Execute(a.TriggerTime, a.RunTimes));
            }

            disposable?.Dispose();
            watch.Stop();
            Console.WriteLine($"Total time for {TotalJobs} Jobs, max run {MaxRunTimes} times is: {watch.ElapsedMilliseconds}ms");
        }

        private ExecutableJob CreateJob(in int i)
        {
            string name = $"Job{i}";
            return new ExecutableJob((d1, d2) => JobActor(d1, d2, name))
            {
                Name = name,
                Schedule = new IntervalJobSchedule
                {
                    ExitStrategy = new ScheduleExitStrategy { MaxRun = 1 },
                    AfterStartInSeconds = 0,
                    IntervalInSeconds = 1,
                    TriggerAtStart = true
                }
            };
        }

        private void JobActor(DateTimeOffset triggerTime, int runTime, string jobName)
        {
            string output =
                $"{DateTime.UtcNow:hh:mm:ss.fff} - {jobName} ({runTime}) run at <{triggerTime:hh:mm:ss.fff}> in Thread <{Thread.CurrentThread.ManagedThreadId}>";
            Debug.WriteLine(output);
            _outputs.Add(output);
        }
    }
}