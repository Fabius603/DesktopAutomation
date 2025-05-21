using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.Video
{
    public static class VideoHelper
    {
        public static string GetUniqueFilePath(string basePath)
        {
            string filePath = basePath;
            int counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(Path.GetDirectoryName(basePath) ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(basePath)}_{counter}{Path.GetExtension(basePath)}");
                counter++;
            }
            return filePath;
        }
    }
}
