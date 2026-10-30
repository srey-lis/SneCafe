using System.Text.RegularExpressions;

namespace SneCafe.Services
{
    // Saves, checks and deletes uploaded product pictures
    public class ImageStorage
    {
        public const long MaxBytes = 2 * 1024 * 1024;   // 2 MB
        private const string UrlFolder = "/images/products/";

        // Only files WE saved look like this: a random name + a safe extension
        private static readonly Regex OurFile =
            new(@"^/images/products/[a-f0-9]{32}\.(jpg|png|webp|gif)\z", RegexOptions.Compiled);

        private readonly string _folder;

        public ImageStorage(IWebHostEnvironment env)
        {
            _folder = Path.Combine(env.WebRootPath, "images", "products");
        }

        // Returns the new picture path, or an error message
        public async Task<(string? Url, string? Error)> SaveAsync(IFormFile file)
        {
            if (file.Length == 0)
                return (null, "The file is empty.");

            if (file.Length > MaxBytes)
                return (null, "The picture is too big. Maximum size is 2 MB.");

            // Look at the first bytes to see what the file REALLY is.
            // The file name can be faked, the first bytes cannot.
            var header = new byte[12];
            int read;
            await using (var s = file.OpenReadStream())
            {
                read = await s.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            }

            string? ext = DetectExtension(header, read);
            if (ext == null)
                return (null, "Only JPG, PNG, WEBP or GIF pictures are allowed.");

            Directory.CreateDirectory(_folder);

            // We choose the name ourselves. The uploaded name is never used.
            string name = Guid.NewGuid().ToString("N") + ext;

            await using (var fs = new FileStream(Path.Combine(_folder, name), FileMode.CreateNew))
            {
                await file.CopyToAsync(fs);
            }

            return (UrlFolder + name, null);
        }

        // Deletes the file only if it is one that we uploaded. Links and other paths are ignored.
        public void DeleteIfOurs(string? url)
        {
            if (string.IsNullOrEmpty(url) || !OurFile.IsMatch(url))
                return;

            string path = Path.Combine(_folder, Path.GetFileName(url));

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
                // The file is busy. Leaving it is better than crashing.
            }
        }

        private static string? DetectExtension(byte[] h, int n)
        {
            // JPEG: FF D8 FF
            if (n >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (n >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 &&
                h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return ".png";

            // GIF: "GIF87a" or "GIF89a"
            if (n >= 6 && h[0] == 'G' && h[1] == 'I' && h[2] == 'F' && h[3] == '8' &&
                (h[4] == '7' || h[4] == '9') && h[5] == 'a') return ".gif";

            // WEBP: "RIFF" .... "WEBP"
            if (n >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
                h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";

            return null;
        }
    }
}