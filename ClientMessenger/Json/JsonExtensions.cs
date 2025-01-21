using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace ClientMessenger.Json
{
    internal static class JsonExtensions
    {
        /// <summary>
        /// Needs to be called on the <c>code</c> property of the <see cref="JsonElement"/>.
        /// Converts the data that is sent as an byte to the <see cref="OpCode"/> equivalent.
        /// </summary>
        /// <param name="property">The <c>code</c> as an <see cref="JsonElement"/> property</param>
        /// <returns><c>Returns</c> the from the Server received OpCode</returns>
        public static OpCode GetOpCode(this JsonElement property)
            => (OpCode)property.GetByte();

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
        /// Needs to be called on the <c>error</c> property of the <see cref="JsonElement"/>.
        /// </summary>
        /// <returns><c>Returns</c> the NpgsqlException that the Server sent</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static NpgsqlExceptionInfos GetNpgsqlExceptionInfos(this JsonElement property)
            => JsonSerializer.Deserialize<NpgsqlExceptionInfos>(property.GetProperty("npgsqlException"))
            ?? throw new InvalidOperationException("Couldn´t get NpgsqlExceptionInfos");
    }
}
