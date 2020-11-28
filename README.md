# FlexScheduler
A lightweight but flexible and data model driven scheduling library for .NET. 

# Features 

- Run on interval, e.g. oncce a few seconds, a few minutes, etc. 
- Run on fixed time, including
  - once a specific day, e.g. every Monday 12:00 AM 
  - once a specific time, e.g. every afternoon 4:00 PM 
- Exit strategy: stops the job either it has run N times or up to a specific datetime. 
- Delay execution or run job immediately 
- Chain all different jobs together and centerialize the management
- Fully testable 

# Comparison 

`FlexScheduler` is based on [Reactive Extension](https://github.com/dotnet/reactive). 
The motivation is tranforming a set of job configurations to an observable sequence that can be subscribed to. 

## Windows Built-in Timers

First, `FlexScheduler` is NOT a timer, so there is no concept of Ticked event handler. All windows built-in timers, 
including `System.Timers.Timer` and `System.Threading.Timer`, or `System.Windows.Forms.Timer` and `System.Windows.Threading.DispatcherTimer`, 
all require to specify an event handler, which representing an action to execute when the _time_ comes. 
Each action needs A timer to invoke. Hence, if we to have N actions to execute on scheduled, 
we will need N timers. 

Second, timers run on interval only and cannot run at a specific time only. Moreover, there is no way to exit, 
unless the timer is set to disabled. 

Finally, most of those timers run on a thread through threadpool, there is no easy way to change it. Some of them are application-depedent. 
In constrast, **FlexScheduler** can be configured to run on TaskPool, NewThread, or your own thread scheduler, and it is a .NET standard 
assembly, so you should be able to use it in any platform with .NET. 

## Other scheduler libraries 

There are some other great scheduler libraries, such as 

- [FluentScheduler](https://github.com/fluentscheduler/FluentScheduler)
- [HarshedWheelTimer](https://github.com/wangjia184/HashedWheelTimer)

All of them are created for different purpose. However, one major difference between them and `FlexScheduler` is: 
they have something suervising the jobs. In another word, it can be seen, including all built-in timers, we are
thinking from top to bottom. It means, there is really a concept of scheduler, manager, or timer from the top. It 
is used to execute the tasks assigned. 

Note: there is nothing wrong of this thinking approach. It's just different. 

# Data Model driven scheduling 

`FlexScheduler`, which does not have a class acts as a scheduler, is in fact trying to focus on data only. 

The intention is thinking about how a job is modeled, and then through the library, the data model is transformed to 
a notifierable event on scheduled. 

With this thinking, we can focus on data model with out worrying about the concept of scheduler, manager, or timer, which 
offers 3 benefits: 

- **much less bootstrapping**: no need to worry about initialization of the top-level class

- **transformmable data model**: can easily store, update, and extend the job model

- **independent task**: each job is independent from the execution, i.e. is not tied with an event handler  

Following is a code snippet showing how a job is defined: 

```c#
private static Job IntervalJobForTesting
{
    get
    {
        var job = new Job
        {
            Name = "Interval Job",
            Schedule = new IntervalJobSchedule
            {
                IntervalInSeconds = 10,
                ExitStrategy = new ScheduleExitStrategy
                {
                    MaxRun = 5
                }
            }
        };
        return job;
    }
}
```

The above job runs once every 10 seconds, and it it set to run at most 5 times. 

To execute this job, simply cast it as an `Observable` and subscribe it. 

```c#
IntervalJobForTesting
    .ToObservable()
    .Subscribe(
        onNext: item=> {
            // each item is a JobObservable type
        }, 
        onComplete: {
            // when the job is invoked 5 times, this will be called 
        }
    );
```

It is also possible to have multiple jobs in one collection to be handled together. For example 

```C#
IList<Job> jobs = new List<Job>();
jobs.Add(job1);
jobs.Add(job2);
jobs
    .ToObservable()
    .Subscribe(
        onNext: item=> {
            // each item is a JobObservable type, which contains the reference of the Job 
        }, 
        onCompleted: {
            // when the job is invoked 5 times, this will be called 
        }
    );    
```

As you see, there is really no direct reference of what needs to be done in the above two examples. However, 
if you like, there are couple classes that inherit from the base `Job` class, which contain an Action. For example: 

```c#
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
```

Now you have an `ExecutableJob`, which can be added to the class `FlexScheduler`, as shown below: 

```c#
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
```

`FlexScheduler` class is created only for convenience purpose. It is not really meant to use in all scenearios, 
because its functionality is very limited. It does not have a way to know if all jobs are completed so you can 
safely dispose everything. 

A more **recommended** way of using this library is defining a `Job` and then convert it to an `Observable`, finally 
to subscribe it, so you can have better control of what to do and when to finish. 

# Use Cases 

This section covers how `FlexScheduler` can be useful in some use cases. As mentioned earlier, the recommended 
usage is focusing on the `Job` data model. You should have your own business logic that composites one or more 
jobs, as each `Jobs` should be seen as part of your business logic. 

## Caching 

In a service-side application, a piece of data are usually refreshed once a day at 11 PM. You have a function call 
to retrieve the new updated data, for example, 

```c#
cached = repository.GetDataForToday(); 
```

However, occasionally you need to manually amend today's data in the middle of the day. Therefore, you set a job 
to refresh the cache once an hour, just in case a manual update happens. Because it is an in-memory caching, there 
is no service call to explicitly evinct cached value. After all, it may be overkill to your application. 

But there is a problem. When the normal data refreshing happens, it will need 5 to 10 minutes to finish updating. 
Therefore, if the scheduled task is refreshing every hour, then it is possible to miss the desired data. To resolve 
this, we can have 2 Jobs, one to run every hour, another to run explicitly at 11:15 PM, when we know it has finished 
the regulard refreshing. 

```c#
IList<Job> jobs = new List<Job>();
jobs.Add(intervalJob); // once an hour
jobs.Add(fixedtimeJob); // every day 11:15 PM
jobs
    .ToObservable()
    .Subscribe(
        _ => {            
            cached = repository.GetDataForToday(); 
        }
    );
```

## Games 

Imagine you have a game with some classes responsible for NPC activities. An activity could be walking straightline, 
running circle, examining surrounding, or collecting items (yes, I feel sad about being an NPC too). Suppose such 
a class is called `NPCActivityLogic`, which can contain one or more `Job`, each represents 
an activity that will happen. Here is what you can do: 

```c#
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

// now in your initialization 
Queue<Job> activities = new Queue<Job>();
activites.Enqueue(WalkJob);
activites.Enqueue(DanceJob);

private void StartJob() {
    var recentJob = activities.Dequeue();
    recentJob
        .ToObservable()
        .Subscribe(
            onNext: item => ExecuteJob(item.Job),
            onCompleted: ResumeJob(item.Job)
        );
}
#

By doing this, you can have a never-ending sequence of 2 jobs, but each job is invoked only 5 times. 

You may argue this can be equivilent to having a while loop. However, unlike a while loop, this is non-blocking sequence, 
which can be executed in separate threads and can be extended with more activities. The same "schedule" (or sequence of
jobs) can be shared by another "actor", which could represent different activities. 

# Future Improvements and Known Issues 

Hopefully by now you have understood why this library is created and what use cases it is best for. Here are some
known issues and future improvements. 

## Known Issues 

Do not put more than a few hundreds of `Job` in one collection and then call `ToObservable`. This appears to cause 
a "StackOverflowException". I do not see a valid reason that anyone wants to have so many jobs chaining together. 
So just don't do it. 

## Future Improvements 

- Need to support explicit time to start a Job 

For any other feature, please submit a feature request in Issues. Thanks. 
