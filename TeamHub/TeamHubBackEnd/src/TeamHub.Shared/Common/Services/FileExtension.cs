using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace TeamHub.Shared.Common.Services;

public static class FileExtension
{
    private static IWebHostEnvironment? _env;

    public static void Initialize(IWebHostEnvironment env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env), "Web hosting environment cannot be null.");
    }

    public static string ToFileUrl(this string base64String, string folder, string filename)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                return string.Empty;
            }

            var newFilename = filename.Replace("/", "-");
            if (base64String.Contains($"{folder}/{newFilename}", StringComparison.OrdinalIgnoreCase))
            {
                return base64String;
            }

            var base64Parts = base64String.Split(',');
            if (base64Parts.Length > 1)
            {
                base64String = base64Parts[1];
            }

            string extension = GetFileExtension(base64String);
            byte[] imageBytes = Convert.FromBase64String(base64String);

            if (_env == null)
            {
                throw new InvalidOperationException("IWebHostEnvironment has not been initialized.");
            }

            string rootDirectory = _env.WebRootPath;
            string safeFolder = folder.Replace('\\', '/').Trim('/');
            newFilename += extension;
            string fullPath = Path.Combine(rootDirectory, safeFolder, newFilename);

            if (!Directory.Exists(Path.Combine(rootDirectory, safeFolder)))
            {
                Directory.CreateDirectory(Path.Combine(rootDirectory, safeFolder));
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            using var ms = new MemoryStream(imageBytes);
            using var image = Image.Load<Rgba32>(ms);
            image.Save(fullPath, GetEncoder(extension));

            return Path.Combine(safeFolder, newFilename).Replace('\\', '/');
        }
        catch (Exception ex)
        {
            throw new Exception("Error Saving File.", ex);
        }
    }

    public static void DeleteFile(this string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            File.Delete(path);
        }
        catch (Exception ex)
        {
            throw new Exception("Error Deleting Files. Check Logs", ex);
        }
    }

    private static string GetFileExtension(string base64String)
    {
        string[] formats = { "jpeg", "png", "gif" };
        const int headerLength = 20;
        byte[] imageBytes = Convert.FromBase64String(base64String);

        var header = BitConverter.ToString(imageBytes.Take(headerLength).ToArray());
        string? format = formats.FirstOrDefault(f => header.StartsWith(GetHeader(f), StringComparison.OrdinalIgnoreCase));

        if (format == null)
        {
            throw new NotSupportedException("Unsupported image format.");
        }

        return format switch
        {
            "jpeg" => ".jpg",
            "png" => ".png",
            "gif" => ".gif",
            _ => throw new NotSupportedException("Unsupported image format.")
        };
    }

    private static string GetHeader(string format)
    {
        return format switch
        {
            "jpeg" => "FF-D8-FF",
            "png" => "89-50-4E",
            "gif" => "47-49-46",
            _ => throw new NotSupportedException("Unsupported image format.")
        };
    }

    private static IImageEncoder GetEncoder(string extension)
    {
        return extension switch
        {
            ".jpg" => new JpegEncoder(),
            ".png" => new PngEncoder(),
            ".gif" => new GifEncoder(),
            _ => throw new NotSupportedException("Unsupported file extension.")
        };
    }
}

