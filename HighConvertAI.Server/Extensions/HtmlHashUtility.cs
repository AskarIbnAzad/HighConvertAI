using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace HighConvertAI.Server.Extensions
{
    public static class HtmlHashUtility
    {
        private static readonly JsonSerializerOptions HashJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Computes a SHA-256 hash from an extracted object (e.g., PageScanResult).
        /// </summary>
        public static string ComputeObjectHash<T>(T obj)
        {
            if (obj == null)
                return string.Empty;

            // Serialize the object to a normalized JSON string
            string json = JsonSerializer.Serialize(obj, HashJsonOptions);

            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes);
        }
    }
}