using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection.PortableExecutable;

namespace ClientMessenger.Json
{
    internal static class JsonConverters
    {
        public class UserConverter : JsonConverter<User>
        {
            public override User? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    JsonElement root = doc.RootElement;

                    root = root.TryGetProperty("user", out JsonElement property)
                        ? property 
                        : doc.RootElement.GetProperty("User");

                    return root.ValueKind == JsonValueKind.Null
                        ? null
                        : new User()
                        {
                            ProfilePicture = Converter.ToBitmapImage(root.GetProperty("ProfilePicture").GetBytesFromBase64()),
                            Username = root.GetProperty("Username").GetString()!,
                            HashTag = root.GetProperty("HashTag").GetString()!,
                            Email = root.GetProperty("Email").GetString()!,
                            Password = root.GetProperty("Password").GetString()!,
                            Biography = root.GetProperty("Biography").GetString()!,
                            Id = long.Parse(root.GetProperty("Id").GetString()!),
                            Birthday = root.GetProperty("Birthday").GetDateOnly(),
                            FaEnabled = bool.Parse(root.GetProperty("FaEnabled").GetString()!),
                            Token = root.GetProperty("Token").GetString()!
                        };
                }
            }

            /// <summary>
            /// Enumerates over each property of the <see cref="User"/> obj and writes its name with the value(as a <c>string</c>).
            /// </summary>
            public override void Write(Utf8JsonWriter writer, User value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach ((string name, string value) item in value)
                {
                    writer.WritePropertyName(item.name);
                    writer.WriteStringValue(item.value);
                }

                writer.WriteEndObject();
            }
        }

        public class RelationshipConverter : JsonConverter<Relationship>
        {
            public override void Write(Utf8JsonWriter writer, Relationship value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach ((string name, string value) item in value)
                {
                    writer.WritePropertyName(item.name);
                    writer.WriteStringValue(item.value);
                }

                writer.WriteEndObject();
            }

            public override Relationship? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    JsonElement root = doc.RootElement;

                    root = root.TryGetProperty("relationship", out JsonElement property)
                        ? property
                        : doc.RootElement.GetProperty("Relationship");

                    return root.ValueKind == JsonValueKind.Null
                        ? null
                        : new Relationship()
                        {
                            ProfilePicture = Converter.ToBitmapImage(root.GetProperty("ProfilePicture").GetBytesFromBase64()),
                            Username = root.GetProperty("Username").GetString()!,
                            HashTag = root.GetProperty("HashTag").GetString()!,
                            Biography = root.GetProperty("Biography").GetString()!,
                            Id = long.Parse(root.GetProperty("Id").GetString()!),
                        };
                }
            }
        }
    }
}
