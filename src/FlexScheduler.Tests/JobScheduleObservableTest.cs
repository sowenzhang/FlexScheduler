using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexScheduler.Tests
{
    [TestClass]
    public class JobScheduleObservableTest
    {
        // trigger one after 10 minutes, another one after 20 minutes 
        private static Job FixedTimeJobForTesting
        {
            get
            {
                var now = DateTimeOffset.Now;

                var job1 = new Job
                {
                    Name = "Fixed",
                    Schedule = new FixedTimeJobSchedule
                    {
                        Slots = new List<DayTimeSlot>
                        {
                            new DayTimeSlot(new TimePortion(now.AddMinutes(10).Hour, now.AddMinutes(10).Minute))
                        },
                        ExitStrategy = new ScheduleExitStrategy
                        {
                            MaxRun = 1
                        }
                    }
                };
                return job1;
            }
        }

        private static Job IntervalJobForTesting
        {
            get
            {
                var job1 = new Job
                {
                    Name = "Interval",
                    Schedule = new IntervalJobSchedule
                    {
                        IntervalInSeconds = 120,
                        ExitStrategy = new ScheduleExitStrategy
                        {
                            MaxRun = 1
                        }
                    }
                };
                return job1;
            }
        }

        [TestMethod]
        public void It_should_run_by_interval()
        {
            var job1 = IntervalJobForTesting;
            var testScheduler = new TestScheduler();
            var triggered = 0;
            var start = testScheduler.Now;
            var end = start;
            job1.ToObservable(testScheduler)
                .Subscribe(
                    a =>
                    {
                        triggered++;
                        end = testScheduler.Now;
                    });

            testScheduler.Start();
            Assert.IsTrue(triggered == 1, "Trigger should be 1, but it is actually " + triggered);
            testScheduler.Stop();
            var diff = end - start;
            // the time should have elapsed exactly 120 seconds, maybe +/- a few milliseconds 
            Assert.IsTrue(diff.TotalSeconds > 120 && diff.TotalSeconds < 121,
                "The time should have elapsed roughly 120 seconds");
        }

        [TestMethod]
        public void It_should_run_at_start()
        {
            var job1 = IntervalJobForTesting;
            // if we set this, then the job starts right away, and we will not elapse any "time" 
            job1.Schedule.TriggerAtStart = true;
            var testScheduler = new TestScheduler();
            var triggered = 0;
            var start = testScheduler.Now;
            var end = start;
            job1.ToObservable(testScheduler)
                .Subscribe(
                    a =>
                    {
                        triggered++;
                        end = testScheduler.Now;
                    });

            testScheduler.Start();
            Assert.IsTrue(triggered == 1, "Trigger should be 1, but it is actually " + triggered);
            testScheduler.Stop();
            var diff = end - start;
            // the time difference could have small variance in the level of ms 
            Assert.IsTrue(diff.TotalMilliseconds < 5.0,
                "The time should be less than 1 millisecond, but it's actually" + diff.TotalMinutes);
        }

        [TestMethod]
        public void It_shoud_run_at_start_then_interval()
        {
            var job1 = IntervalJobForTesting;
            job1.Schedule.TriggerAtStart = true;
            job1.Schedule.ExitStrategy.MaxRun = 2;
            var testScheduler = new TestScheduler();
            var triggered = 0;
            var start = testScheduler.Now;
            var end = start;
            job1.ToObservable(testScheduler)
                .Subscribe(
                    a =>
                    {
                        triggered++;
                        end = testScheduler.Now;
                    });

            testScheduler.Start();
            Assert.IsTrue(triggered == 2, "Trigger should be 2, but it is actually " + triggered);
            testScheduler.Stop();
            var diff = end - start;
            // the time should have elapsed exactly 120 seconds, maybe +/- a few milliseconds 
            Assert.IsTrue(diff.TotalSeconds > 120 && diff.TotalSeconds < 121,
                "The time should have elapsed roughly 120 seconds");
        }

        [TestMethod]
        public void It_should_stop_intervalJob_tillTime()
        {
            var testScheduler = new TestScheduler();
            var triggered = 0;
            var start = testScheduler.Now;

            var job1 = IntervalJobForTesting;
            job1.Schedule.ExitStrategy.MaxRun = null; // no max run now
            job1.Schedule.ExitStrategy.TillTime = start.AddSeconds(360); // this means we will run 3 times
            var end = start;
            job1.ToObservable(testScheduler)
                .Subscribe(
                    a =>
                    {
                        triggered++;
                        end = testScheduler.Now;
                    });

            testScheduler.Start();
            Assert.IsTrue(triggered == 3, "Trigger should be 3, but it is actually " + triggered);
            testScheduler.Stop();
            var diff = end - start;
            // the time should have elapsed exactly 120 seconds, maybe +/- a few milliseconds 
            Assert.IsTrue(diff.TotalSeconds > 360 && diff.TotalSeconds < 361,
                "The time should have elapsed roughly 360 seconds");
        }

        [TestMethod]
        public void It_should_run_at_fixedTime()
        {
            var job1 = FixedTimeJobForTesting;
            var testScheduler = new TestScheduler();
            var triggered = 0;

            // first, set a good time in the scheduler 
            testScheduler.AdvanceTo(DateTimeOffset.Now.Ticks);
            var start = testScheduler.Now;
            var end = start;

            job1.ToObservable(testScheduler)
                .Subscribe(
                    a =>
                    {
                        triggered++;
                        end = testScheduler.Now;
                    });

            testScheduler.AdvanceTo(start.AddMinutes(10).Ticks);

            var diff = end - start;
            Assert.IsTrue(triggered == 1, "Trigger should be 1, but it is actually " + triggered);
            Assert.IsTrue(diff.TotalMinutes >= 9 && diff.TotalMinutes <= 11,
                "The time should have elapsed roughly 10 minutes, but actually " + diff.TotalMinutes);
        }

        [TestMethod]
        public void It_should_chain_all_jobs()
        {
            var job1 = IntervalJobForTesting;
            var testScheduler = new TestScheduler();
            testScheduler.AdvanceTo(DateTimeOffset.Now.Ticks);
            var start = testScheduler.Now;
            var end = start;
            job1.Schedule.ExitStrategy.MaxRun = null;
            job1.Schedule.ExitStrategy.TillTime =
                start.AddMinutes(10); // job1 will trigger 1 per 2 mins, so this will trigger 5 times

            // this will still run once and after 10 minutes
            var job2 = FixedTimeJobForTesting;
            IList<Job> jobs = new List<Job> {job1, job2};
            var obs = jobs.Select(a => a.ToObservable(testScheduler)).ToArray();

            var job2Fired = false;
            var job1Triggered = 0;

            // this allows us combine multiple observable into one single stream 
            var combined = obs.Aggregate((a, b) => a.Merge(b));
            combined.Subscribe(a =>
                {
                    switch (a.Job.Name)
                    {
                        case "Fixed":
                            job2Fired = true;
                            break;
                        case "Interval":
                        {
                            job1Triggered++;
                            break;
                        }
                        default:
                        {
                            Assert.Fail("What?");
                            break;
                        }
                    }

                    end = a.TriggerTime;
                }
            );

            testScheduler.AdvanceTo(start.AddMinutes(2).Ticks);
            testScheduler.AdvanceTo(start.AddMinutes(4).Ticks);
            testScheduler.AdvanceTo(start.AddMinutes(6).Ticks);
            testScheduler.AdvanceTo(start.AddMinutes(8).Ticks);
            testScheduler.AdvanceTo(start.AddMinutes(11).Ticks);
            var diff = end - start;

            Assert.IsTrue(job1Triggered == 5, "Trigger should be 5, but it is actually " + job1Triggered);
            Assert.IsTrue(job2Fired, "Job2 should have been fired");
            Assert.IsTrue(diff.TotalMinutes >= 10 && diff.TotalMinutes <= 12.5,
                "The time should have elapsed roughly 10 minutes, but actually " + diff.TotalMinutes);
        }
    }
}