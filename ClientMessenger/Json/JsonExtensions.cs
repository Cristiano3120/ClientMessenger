using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClientMessenger.Json
{
    internal static class JsonExtensions
    {
        #region GetExtensions
        /// <summary>
        /// Needs to be called on the <c>root</c> of the <see cref="JsonElement"/>.
        /// Converts the data that is sent as an byte to the <see cref="OpCode"/> equivalent.
        /// </summary>
        /// <param name="property">The <c>code</c> as an <see cref="JsonElement"/> property</param>
        /// <returns><c>Returns</c> the from the Server received OpCode</returns>
        public static OpCode GetOpCode(this JsonElement property)
            => (OpCode)property.GetProperty("opCode").GetByte();

        /// <summary>
        /// Needs to be called on the <c>root</c> of the <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="property">The <see cref="JsonElement"/></param>
        /// <returns></returns>
        public static RSAParameters GetPublicKey(this JsonElement property)
            => new()
            {
                Modulus = property.GetProperty("modulus").GetBytesFromBase64(),
                Exponent = property.GetProperty("exponent").GetBytesFromBase64(),
            };

        /// <summary>
        /// Needs to be called on a property with the type <c>string</c>. 
        /// </summary>
        /// <returns><c>Returns</c> the wanted date in the german time format</returns>
        public static DateOnly GetDateOnly(this JsonElement property)
            => DateOnly.Parse(property.GetString()!, new CultureInfo("de-DE"));

        /// <summary>
        /// Needs to be called on the <c>root</c> of the <see cref="JsonElement"/>.
        /// </summary>
        /// <returns><c>Returns</c> the NpgsqlException that the Server sent</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static NpgsqlExceptionInfos GetNpgsqlExceptionInfos(this JsonElement property)
            => JsonSerializer.Deserialize<NpgsqlExceptionInfos>(property.GetProperty("npgsqlExceptionInfos"))
            ?? throw new InvalidOperationException("Couldn´t get NpgsqlExceptionInfos");

        #endregion

        #region SetExtensions

        /// <summary>
        /// Updates the value of a specific property within the given <see cref="JsonElement"/>.
        /// </summary>
        /// <remarks>
        /// The input <paramref name="property"/> must represent a JSON object. 
        /// If it does not contain the specified property <paramref name="propertyName"/>, 
        /// it will be added with the given <paramref name="content"/>.
        /// </remarks>
        /// <returns>
        /// A new <see cref="JsonElement"/> with the updated property value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the JSON structure in <paramref name="property"/> is not an object.
        /// </exception>
        public static JsonElement SetString(this JsonElement property, JsonFile jsonFile, string propertyName , string content)
        {
            JsonObject jsonObject = JsonNode.Parse(property.GetRawText())!.AsObject();
            jsonObject[propertyName] = content;

            string jsonString = jsonObject.ToString();
            File.WriteAllText(GetPathToJsonFile(jsonFile), jsonString);

            return JsonDocument.Parse(jsonString).RootElement;
        }

        public static JsonElement SetBoolean(this JsonElement property, JsonFile jsonFile, string propertyName, bool enabled)
        {
            JsonObject jsonObject = JsonNode.Parse(property.GetRawText())!.AsObject();
            jsonObject[propertyName] = enabled;

            string jsonString = jsonObject.ToString();
            File.WriteAllText(GetPathToJsonFile(jsonFile), jsonString);

            return JsonDocument.Parse(jsonString).RootElement;
        }

        private static string GetPathToJsonFile(JsonFile jsonFile)
        {
            return jsonFile switch
            {
                JsonFile.Config => Client.GetDynamicPath(@"Settings/Settings.json"),
                _ => throw new NotSupportedException("The JsonFile has no entry")
            };
        }

        #endregion

        #region ReadJson
        
        public static JsonElement ReadJsonFile(JsonFile jsonFile)
        {
            string pathToJson = GetPathToJsonFile(jsonFile);    
            string jsonFileContent = File.ReadAllText(pathToJson);
            return JsonDocument.Parse(jsonFileContent).RootElement;
        }

        #endregion
    }
}
