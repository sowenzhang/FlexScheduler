using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlexScheduler
{
    public class JobScheduleJsonConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);

            var type = (item["type"] ?? throw new InvalidOperationException("JSON does not contain schedule type, may be invalid")).Value<string>();

            if (type == nameof(IntervalJobSchedule))
            {
                return item.ToObject<IntervalJobSchedule>();
            }

            if (type == nameof(FixedTimeJobSchedule))
            {
                return item.ToObject<FixedTimeJobSchedule>();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject o = JObject.FromObject(value);
            if (value is IntervalJobSchedule)
            {
                o.AddFirst(new JProperty("type", new JValue(nameof(IntervalJobSchedule))));
            }
            else if (value is FixedTimeJobSchedule)
            {
                o.AddFirst(new JProperty("type", new JValue(nameof(FixedTimeJobSchedule))));
            }

            o.WriteTo(writer);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(JobSchedule).IsAssignableFrom(objectType);
        }
    }

}