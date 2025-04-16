using System.Text.Json.Serialization;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace ClientMessenger.Json
{
    internal static class JsonConverters
    {
        public class Base64ByteArrayJsonConverter : JsonConverter<BitmapImage>
        {
            public override BitmapImage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => BitmapImageConverter.ToBitmapImage(reader.GetBytesFromBase64());

            public override void Write(Utf8JsonWriter writer, BitmapImage value, JsonSerializerOptions options)
                => writer.WriteStringValue(Convert.ToBase64String(BitmapImageConverter.ToByteArray(value)));
        }
    }
}
