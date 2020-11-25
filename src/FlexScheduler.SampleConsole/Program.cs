using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FlexScheduler.SampleConsole
{
    class Program
    {
        private static Random rand = new Random();
        static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss.fff} - You will see 2 jobs working with different intervals...");

            var scheduler = FlexScheduler.Current
                .AddJob(CreateExecutableJob("Job1", 1))
                .AddJob(CreateExecutableJob("Job2", 2));

            Console.WriteLine();

            scheduler.Start();

            Console.ReadLine();
        }

        private static Job CreateExecutableJob(string name, int intervalInSecond)
        {
            int delayExecution = rand.Next(0, 5);

            var job = new ExecutableJob((d1, d2) => JobActor(d1, d2, name))
            {
                Name = name,
                Schedule = new IntervalJobSchedule
                {
                    ExitStrategy = new ScheduleExitStrategy {MaxRun = 5},
                    AfterStartInSeconds = delayExecution,
                    IntervalInSeconds = intervalInSecond,
                    TriggerAtStart = true
                }
            };

            Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss.fff} - {name} will run with interval <{intervalInSecond}> second but delay execute after <{delayExecution}> seconds ");
            return job;
        }

        private static void JobActor(DateTimeOffset triggerTime, int runTime, string jobName)
        {
            if (jobName == "Job2")
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
            }
            Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss.fff} - {jobName} ({runTime}) run at <{triggerTime:hh:mm:ss.fff}>");
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}
