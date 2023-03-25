using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace TextStats.ParseLogic
{
    public class Utility
    {
        public static async Task<string> DownloadFile(string url)
        {
            var dirInfo = new DirectoryInfo("DownloadedFiles");
            if (dirInfo.Exists == false) dirInfo.Create();
            var targetPath = Path.Combine(dirInfo.FullName, Guid.NewGuid().ToString());

            using (var client = new HttpClient())
            {
                using (var s = client.GetStreamAsync(url))
                {
                    using (var fs = new FileStream(targetPath, FileMode.OpenOrCreate))
                    {
                        await s.Result.CopyToAsync(fs);
                    }
                }
            }

            return  targetPath;
        }

        public static async Task<string> GetFileCheckSum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var sb = new StringBuilder();
                    byte[] hashBytes = await md5.ComputeHashAsync(stream);
                    foreach (byte bt in hashBytes)
                    {
                        sb.Append(bt.ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
        }

        public static void Serialize<T>(T instance, string filePath)
        {
            var serializer = new XmlSerializer(instance!.GetType());

            using (var writer = System.Xml.XmlWriter.Create(filePath))
            {
                serializer.Serialize(writer, instance);
                writer.Flush();
            }
        }

        public static T DeSerialize<T>(string filePath)
        {
            if (File.Exists(filePath) == false) return default(T);

            var serializer = new XmlSerializer(typeof(T));

            var fi = new FileInfo(filePath);
            using (var stream = fi.OpenRead())
            {
                return (T)serializer.Deserialize(stream)!;
            }
        }
    }
}