// ------------------------------------------------------
// Copyright (C) Microsoft. All rights reserved.
// ------------------------------------------------------

using System.Collections.Generic;
using System;

namespace FlexScheduler.SampleConsole
{
    public class RotateJobTester
    {
        private static Job WalkJob
        {
            get
            {
                var job = new Job
                {
                    Name = "Walk",
                    Schedule = new IntervalJobSchedule
                    {
                        IntervalInSeconds = 2,
                        ExitStrategy = new ScheduleExitStrategy
                        {
                            MaxRun = 5
                        }
                    }
                };
                return job;
            }
        }

        private static Job DanceJob
        {
            get
            {
                var job = new Job
                {
                    Name = "Dance",
                    Schedule = new IntervalJobSchedule
                    {
                        IntervalInSeconds = 2,
                        ExitStrategy = new ScheduleExitStrategy
                        {
                            MaxRun = 5
                        }
                    }
                };
                return job;
            }
        }

        private readonly Queue<Job> _activities = new Queue<Job>();

        /// <summary>
        /// these jobs will not end, it will keep running. Walk once every 2 seconds for 5 times, and then Dance once every 2 seconds for 5 times 
        /// </summary>
        public RotateJobTester()
        {
            _activities.Enqueue(WalkJob);
            _activities.Enqueue(DanceJob);
        }

        public void StartJob() {
            var recentJob = _activities.Dequeue();
            recentJob
                .ToObservable()
                .Subscribe(
                    onNext: item => ExecuteJob(item.Job),
                    onCompleted: () => ResumeJob(recentJob)
                );
        }

        private void ResumeJob(Job recentJob)
        {
            _activities.Enqueue(recentJob);
            // start the job again to 
            StartJob();
        }

        private void ExecuteJob(Job job)
        {
            switch (job)
            {
                case { } walk when walk.Name == "Walk":
                {
                    Console.WriteLine("NPC is Walking from X to Y");
                    break;
                }
                case { } dance when dance.Name == "Dance":
                {
                    Console.WriteLine("NPC is dancing");
                    break;
                }
                default:
                {
                    throw new NotSupportedException("Not supported activity");
                }
            }
        }
    }
}