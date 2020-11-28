using System;
using System.Threading;


namespace FlexScheduler.SampleConsole
{
    class Program
    {
        private static readonly Random Rand = new Random();
        static void Main()
        {
            // uncomment the code here to compare the timer
            //var timer = new System.Timers.Timer(1000);
            //timer.Elapsed += (sender, eventArgs) =>
            //{
            //    JobActor(DateTimeOffset.Now, 0, "Job1");
            //};
            //timer.Enabled = true;
            //Console.ReadLine();
            //return;

            // uncomment here to compare the Threading.Timer 
            //var timer = new System.Threading.Timer(_=> {JobActor(DateTimeOffset.Now, 0, "Job1");}, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            //Console.ReadLine();
            //return;

            // uncomment if you want to test a large paralle jobs execution
            //new LargeJobsTester().Run();
            //return;
            
            // uncomment if you want to test a rotating jobs 
            //new RotateJobTester().StartJob();
            //return;

            Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss.fff} - You will see 2 jobs working with different intervals...");
            using (FlexScheduler.Current
                .AddJob(CreateExecutableJob("Job1", 1))
                .AddJob(CreateExecutableJob("Job2", 2))
                .Start())
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit");
                Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss.fff} - Start...");
                Console.ReadLine();
            }
        }

        private static Job CreateExecutableJob(string name, int intervalInSecond)
        {
            int delayExecution = Rand.Next(0, 5);

            var job = new ExecutableJob((d1, d2) => JobActor(d1, d2, name))
            {
                Name = name,
                Schedule = new IntervalJobSchedule
                {
                    ExitStrategy = new ScheduleExitStrategy { MaxRun = 5 },
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
            Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss.fff} - {jobName} ({runTime}) run at <{triggerTime:hh:mm:ss.fff}> in Thread <{Thread.CurrentThread.ManagedThreadId}>");
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}
