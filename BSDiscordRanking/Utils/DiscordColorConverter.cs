using System;
using Discord;
using Newtonsoft.Json;

namespace BSDiscordRanking.Utils
{
    public class DiscordColorConverter : JsonConverter
    {
        public struct toto
        {
            public uint RawValue;
        }
        
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(Color))
            {
                return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // ReSharper disable once PossibleInvalidOperationException
            return new Color(rawValue: serializer.Deserialize<toto>(reader).RawValue);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Color v = (Color)value;

            writer.WriteStartObject();
            writer.WritePropertyName("R");
            writer.WriteValue(v.R);
            writer.WritePropertyName("G");
            writer.WriteValue(v.G);
            writer.WritePropertyName("B");
            writer.WriteValue(v.B);
            writer.WritePropertyName("RawValue");
            writer.WriteValue(v.RawValue);
            writer.WriteEndObject();
        }
    }
}