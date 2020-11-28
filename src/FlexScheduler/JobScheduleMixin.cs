using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace FlexScheduler
{
    public static class JobScheduleMixin
    {
        /// <summary>
        /// Convert a job to an observable. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="job"></param>
        /// <param name="scheduler">A task scheduler, if not specified, try to use the one on the Job, otherwise, it falls back to the default NewThreadScheduler</param>
        /// <returns>an Observable of JobObservable</returns>
        /// <remarks>
        /// An observable is a sequence. This mixin allows us to convert a type of Job to an observable.
        /// The next state in the observable is only published when the next trigger time is hit.
        /// It allows us to schedule by interval or at a specific time. The timer does not tick every 1 second or
        /// 1 minute, it ticks by exact amount from current time to the next run time. This gives us much
        /// high flexibility and more gentle to the machine. Each state is published in to a separate thread.
        /// By default, if not specified, the scheduler will be TaskPool 
        /// </remarks>
        public static IObservable<JobObservable<T>> ToObservable<T>(this T job, IScheduler scheduler = null)
            where T : Job
        {
            switch (job.Schedule)
            {
                case IntervalJobSchedule intervalSchedule:
                    {
                        return job.ScheduleIntervalJob(intervalSchedule, scheduler ?? job.TaskScheduler);
                    }
                case FixedTimeJobSchedule fixedSchedule:
                    {
                        return job.ScheduleFixedTimeJob(fixedSchedule, scheduler ?? job.TaskScheduler);
                    }
                default:
                    {
                        throw new NotSupportedException($"{job.Schedule.GetType().FullName} is not supported");
                    }
            }
        }

        /// <summary>
        /// Converts a list of jobs to a sequence of observable 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jobs"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static IObservable<JobObservable<T>> ToObservable<T>(this IEnumerable<T> jobs,
            IScheduler scheduler = null) where T : Job
        {
            IObservable<JobObservable<T>>[] jobobservables = jobs.Select(a => a.ToObservable(scheduler)).ToArray();
            if (jobobservables.Any())
            {
                var first = jobobservables.First();
                for (int i = 1; i < jobobservables.Length; i++)
                {
                    first = first.Merge(jobobservables[i]);
                }

                return first;
            }

            return Observable.Empty<JobObservable<T>>();
        }

        private static long GetSecondsToNextRun(IEnumerable<DayTimeSlot> slots, IScheduler scheduler)
        {
            var arr = slots.OrderBy(a => a.DayOfWeek).ThenBy(a => a.TimeOfDay.Hour).ToArray();
            var now = scheduler.Now;
            long curr = now.Hour * 60 * 60 + now.Minute * 60 + now.Second + (int)now.DayOfWeek * 86400;
            long diff = -1;
            long min = int.MaxValue;
            // this loop basically is looking for the closest time to current time 
            foreach (DayTimeSlot expected in arr)
            {
                long secondOfExpected;
                if (expected.DayOfWeek != null)
                {
                    secondOfExpected = expected.TimeOfDay.Hour * 60 * 60 + expected.TimeOfDay.Minute * 60 +
                                       expected.TimeOfDay.Second + (int)expected.DayOfWeek.Value * 86400;
                }
                else
                {
                    secondOfExpected = expected.TimeOfDay.Hour * 60 * 60 + expected.TimeOfDay.Minute * 60 +
                                       expected.TimeOfDay.Second + (int)now.DayOfWeek * 86400;
                }

                if (secondOfExpected < min)
                {
                    min = secondOfExpected;
                }

                if (secondOfExpected >= curr)
                {
                    diff = secondOfExpected - curr;
                    break;
                }


            }

            if (diff > 0)
                return diff;

            const long max = 6 * 86400 + 86400; // the last second of each week;
            diff = max - curr + min;

            return diff;
        }

        private static IObservable<JobObservable<T>> ScheduleFixedTimeJob<T>(this T job,
                                                                             FixedTimeJobSchedule jobSchedule,
                                                                             IScheduler scheduler = null)
            where T : Job
        {
            IScheduler sc = scheduler ?? NewThreadScheduler.Default;

            (DateTimeOffset currentTime, int tickCount) initialState = (sc.Now, 1);
            long distanceToNextRun = GetSecondsToNextRun(jobSchedule.Slots, sc);

            var o = Observable.Generate(
                initialState,
                x => ShouldContinue(jobSchedule.ExitStrategy, x.tickCount, x.currentTime),
                x => (x.currentTime.AddSeconds(distanceToNextRun), x.tickCount + 1),
                x => x,
                x => // time selector, will tick again based on the returned timespan 
                    {
                        if (job.Schedule.TriggerAtStart && x.tickCount == 1)
                        {
                            return TimeSpan.Zero;
                        }

                        distanceToNextRun = GetSecondsToNextRun(jobSchedule.Slots, sc);
                        return TimeSpan.FromSeconds(distanceToNextRun);
                    },
                sc);

            return o
                .Delay(TimeSpan.FromSeconds(job.Schedule.AfterStartInSeconds), sc)
                .Select(
                state => new JobObservable<T>
                {
                    Job = job,
                    TriggerTime = sc.Now,
                    RunTimes = state.tickCount
                });
        }

        private static IObservable<JobObservable<T>> ScheduleIntervalJob<T>(this T job, IntervalJobSchedule jobSchedule, IScheduler scheduler = null)
            where T : Job
        {
            IScheduler sc = scheduler ??
                            NewThreadScheduler.Default;

            (DateTimeOffset currentTime, int tickCount) initialState = (sc.Now, 1);
            var intervalInSeconds = jobSchedule.IntervalInSeconds;
            var o = Observable.Generate(
                initialState,
                x => ShouldContinue(jobSchedule.ExitStrategy, x.tickCount, x.currentTime),
                x => (x.currentTime.AddSeconds(jobSchedule.IntervalInSeconds), x.tickCount + 1),
                x => x,
                x =>
                    {
                        if (job.Schedule.TriggerAtStart && x.tickCount == 1)
                        {
                            return TimeSpan.Zero;
                        }

                        return TimeSpan.FromSeconds(intervalInSeconds);
                    },
                sc);

            return o
                .Delay(TimeSpan.FromSeconds(job.Schedule.AfterStartInSeconds), sc)
                .Select(
                state => new JobObservable<T>
                {
                    Job = job,
                    TriggerTime = sc.Now,
                    RunTimes = state.tickCount
                });
        }

        private static bool ShouldContinue(ScheduleExitStrategy exitStrategy, int ticked, DateTimeOffset now)
        {
            if (exitStrategy == null)
                return true;

            if (exitStrategy.MaxRun.HasValue)
                return exitStrategy.MaxRun.Value >= ticked;

            // if we don't minus a small time, we may end up going to the next iteration 
            if (exitStrategy.TillTime.HasValue)
                return exitStrategy.TillTime.Value.AddSeconds(-1) >= now;

            return false;
        }
    }
}