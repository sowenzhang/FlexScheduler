using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using Newtonsoft.Json;

namespace FlexScheduler.Tests
{
    [TestClass]
    public class JobJsonReadingTest
    {
        [TestMethod]
        public void It_should_serialize_jobs_and_deserialize()
        {
            Job testJob = new Job()
            {
                Name="TestJob",
                Schedule = new IntervalJobSchedule()
                {
                    ExitStrategy = new ScheduleExitStrategy() { MaxRun = 10 },
                    IntervalInSeconds = 120,
                    TriggerAtStart = false
                },
                TaskScheduler = new EventLoopScheduler()
            };

            var json = JsonConvert.SerializeObject(testJob, JobSerializationSettings.Settings);
            var deserializedJob = JsonConvert.DeserializeObject<Job>(json, JobSerializationSettings.Settings);

            Assert.IsNotNull(deserializedJob);
            Assert.IsTrue(deserializedJob.Name == "TestJob");
            Assert.IsTrue(deserializedJob.Schedule.TriggerAtStart == false);
            Assert.IsTrue(deserializedJob.Schedule is IntervalJobSchedule);
            Assert.IsTrue(deserializedJob.Schedule.ExitStrategy.MaxRun.GetValueOrDefault() == 10);
        }

        [TestMethod]
        public void It_should_read_jobs_from_jsonfile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "FlexScheduler.Tests.config.jobs.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Assert.Fail($"File {resourceName} does not exist");
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var jobs = JsonConvert.DeserializeObject<IList<Job>>(json, JobSerializationSettings.Settings);
                    Assert.IsNotNull(jobs);
                    Assert.IsTrue(jobs.Count == 3);
                    Assert.IsTrue(jobs[0].Schedule is IntervalJobSchedule);
                    Assert.IsTrue(jobs[1].Schedule is IntervalJobSchedule);
                    Assert.IsTrue(jobs[2].Schedule is FixedTimeJobSchedule);
                    var schedule = (FixedTimeJobSchedule)jobs[2].Schedule;
                    var slots = schedule.Slots.ToArray();
                    Assert.IsTrue(slots.Length == 2);
                    Assert.IsTrue(slots[0].DayOfWeek.GetValueOrDefault() == DayOfWeek.Friday);
                    Assert.IsTrue(slots[1].DayOfWeek.GetValueOrDefault() == DayOfWeek.Monday);
                }
            }
        }
    }
}
