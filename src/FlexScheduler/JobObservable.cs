using System;

namespace FlexScheduler
{
    /// <summary>
    /// A job observable is a type returned from converting a list of job to Observable.
    /// When subscribing the observable, this is the next state (each item from observable) 
    /// </summary>
    /// <typeparam name="TJob"></typeparam>
    public class JobObservable<TJob> where TJob : Job
    {
        public TJob Job { get; set; }
        public DateTimeOffset TriggerTime { get; set; }
        public int RunTimes { get; set; }
    }
}