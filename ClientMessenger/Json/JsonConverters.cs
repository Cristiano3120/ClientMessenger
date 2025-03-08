using System.Text.Json.Serialization;
using System.Text.Json;

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

                    if (root.TryGetProperty("user", out JsonElement userProperty))
                    {
                        root = userProperty;
                    }

                    return root.ValueKind == JsonValueKind.Null
                        ? null
                        : new User()
                        {
                            ProfilePicture = Converter.ToBitmapImage(root.GetProperty("profilePicture").GetBytesFromBase64()),
                            Username = root.GetProperty("username").GetString()!,
                            HashTag = root.GetProperty("hashTag").GetString()!,
                            Email = root.GetProperty("email").GetString()!,
                            Password = root.GetProperty("password").GetString()!,
                            Biography = root.GetProperty("biography").GetString()!,
                            Id = long.Parse(root.GetProperty("id").GetString()!),
                            Birthday = root.GetProperty("birthday").GetDateOnly(),
                            FaEnabled = bool.Parse(root.GetProperty("faEnabled").GetString()!),
                            Token = root.GetProperty("token").GetString()!
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

                    if (root.TryGetProperty("relationship", out JsonElement relationshipProperty))
                    {
                        root = relationshipProperty;
                    }

                    return root.ValueKind == JsonValueKind.Null
                        ? null
                        : new Relationship()
                        {
                            ProfilePicture = Converter.ToBitmapImage(root.GetProperty("profilePicture").GetBytesFromBase64()),
                            Username = root.GetProperty("username").GetString()!,
                            HashTag = root.GetProperty("hashTag").GetString()!,
                            Biography = root.GetProperty("biography").GetString()!,
                            Id = long.Parse(root.GetProperty("id").GetString()!),
                            RelationshipState = Enum.Parse<RelationshipState>(root.GetProperty("relationshipState").GetString()!),
                        };
                }
            }
        }
    }
}
