using System;
using System.Collections.Generic;

namespace FlexScheduler
{
    /// <summary>
    /// The base JobSchedule that can be used in the scheduler 
    /// </summary>
    public abstract class JobSchedule
    {
        /// <summary>
        /// if set true, it will fire immediately.
        /// Keep it in mind, the TriggerAtStart will be counted as run times.
        /// Meaning, if the ExitStrategy defines MaxRun as 1, the run at start is counted as 1. 
        /// </summary>
        public bool TriggerAtStart { get; set; }

        /// <summary>
        /// defines the time to start the observable. This value is only used in the first item. 
        /// </summary>
        public int AfterStartInSeconds { get; set; }

        /// <summary>
        /// define the exit strategy, referred to when the observable to stop. If null, it will be forever. 
        /// </summary>
        public ScheduleExitStrategy ExitStrategy { get; set;}
    }

    /// <summary>
    /// this schedule is triggered based on Interval 
    /// </summary>
    public class IntervalJobSchedule : JobSchedule
    {
        public int IntervalInSeconds { get; set; }
    }

    /// <summary>
    /// this schedule is triggered based on a fixed time slot (of each week) 
    /// </summary>
    public class FixedTimeJobSchedule : JobSchedule
    {
        public IEnumerable<DayTimeSlot> Slots { get; set; }
    }

    /// <summary>
    /// Defines a time, which contains hour, minute and second
    /// </summary>
    public class TimePortion
    {
        public TimePortion(int hour, int minute, int second = 0)
        {
            this.Hour = hour;
            this.Minute = minute;
            this.Second = second;
            
            if (Hour > 24 || Hour < 0) throw new ArgumentOutOfRangeException(nameof(hour), "hour must be between 0 and 24");
            if (Minute> 60 || Minute<0) throw new ArgumentOutOfRangeException(nameof(minute), "minute must be between 0 and 60");
            if (Second>60 || Second<0) throw new ArgumentOutOfRangeException(nameof(second), "second must be between 0 and 60");
        }

        /// <summary>
        /// 
        /// </summary>
        public int Hour { get; }
        /// <summary>
        /// 
        /// </summary>
        public int Minute { get; }
        /// <summary>
        /// 
        /// </summary>
        public int Second {get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DayTimeSlot
    {
        public DayTimeSlot(TimePortion timeOfDay, DayOfWeek? dayOfWeek = null)
        {
            DayOfWeek = dayOfWeek;
            TimeOfDay = timeOfDay;
        }

        /// <summary>
        /// if not specified, the time of day is used as daily 
        /// </summary>
        public DayOfWeek? DayOfWeek { get;  }

        /// <summary>
        /// 
        /// </summary>
        public TimePortion TimeOfDay { get;  }
    }

    /// <summary>
    /// A strategy defined if the job should exit 
    /// </summary>
    public class ScheduleExitStrategy {
        /// <summary>
        /// maximum run of the job observable. If null, it will check TillTime 
        /// </summary>
        public int? MaxRun { get; set; }

        /// <summary>
        /// stop till the specified time. If null, it will be forever 
        /// </summary>
        public DateTimeOffset? TillTime {get;set;}
    }
}