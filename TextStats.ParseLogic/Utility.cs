using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace TextStats.ParseLogic
{
    public class Utility
    {
      

        public static string GetBaseFolderPath()
        {
            var dirInfo = new DirectoryInfo("TextStats");
            if (dirInfo.Exists == false) dirInfo.Create();
            return dirInfo.FullName;
        }
             
        public static async Task<string> DownloadFile(Uri uri)
        {

            var dirInfo = new DirectoryInfo(Path.Combine(GetBaseFolderPath(), "DownloadedFiles"));
            if (dirInfo.Exists == false) dirInfo.Create();
            
            string getTargetPath()
            {
                string fileName = Path.GetFileName(uri.AbsolutePath);
                var finalPath = Path.Combine(dirInfo!.FullName, fileName);
                if (File.Exists(finalPath) == false) return finalPath;

                string finalFileName = Guid.NewGuid().ToString();
                string ext = Path.GetExtension(finalPath);
                if (string.IsNullOrWhiteSpace(ext) == false) ext = $".{ext}";


                for (int i = 0; i < 1000; i++)
                {
                    finalPath = Path.Combine(dirInfo!.FullName, $"{fileName}_{i}{ext}");
                    if (File.Exists(finalPath) == false) return finalPath;
                }

                return Path.Combine(dirInfo!.FullName, Guid.NewGuid().ToString());
            }

            var targetPath = getTargetPath();

            using (var client = new HttpClient())
            {
                using (var s = client.GetStreamAsync(uri))
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
            var fileInfo = new FileInfo(filePath);
            if (fileInfo!.Directory!.Exists == false) fileInfo.Directory.Create();

            using (var writer = System.Xml.XmlWriter.Create(filePath))
            {
                serializer.Serialize(writer, instance);
                writer.Flush();
            }
        }

        public static T? DeSerialize<T>(string filePath)
        {
            if (File.Exists(filePath) == false) return default;

            var serializer = new XmlSerializer(typeof(T));

            var fi = new FileInfo(filePath);
            using (var stream = fi.OpenRead())
            {
                return (T)serializer.Deserialize(stream)!;
            }
        }

        public static bool IsLocalAbsolutePath(string input)
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                return uri.IsFile;
            }

            return false;
        }

        public static bool IsRemoteAbsolutePath(string input)
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                return !uri.IsFile;
            }

            return false;
        }

        public static bool IsBinary(string filePath, int requiredConsecutiveNul = 1)
        {
            const int charsToCheck = 8000;
            const char nulChar = '\0';

            int nulCount = 0;

            using (var streamReader = new StreamReader(filePath))
            {
                for (var i = 0; i < charsToCheck; i++)
                {
                    if (streamReader.EndOfStream)
                        return false;

                    if ((char)streamReader.Read() == nulChar)
                    {
                        nulCount++;

                        if (nulCount >= requiredConsecutiveNul)
                            return true;
                    }
                    else
                    {
                        nulCount = 0;
                    }
                }
            }

            return false;
        }
    }
}