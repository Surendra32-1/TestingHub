using Microsoft.AspNetCore.Hosting;

namespace TeamHub.Shared.Common.Services;

public class FileHelper
{
    private static IWebHostEnvironment? _env;

    public static void Initialize(IWebHostEnvironment env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env), "Web hosting environment cannot be null.");
    }

    public static (string folder, string filename) GetFolderAndFilename(string fullPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentException("Path cannot be empty.", nameof(fullPath));
            }

            string normalizedPath = fullPath.Replace('\\', '/');
            string filename = Path.GetFileName(normalizedPath);
            int lastSlash = normalizedPath.LastIndexOf('/');
            string folder = lastSlash >= 0 ? normalizedPath.Substring(0, lastSlash) : string.Empty;
            return (folder, filename);
        }
        catch (Exception ex)
        {
            throw new Exception("Error extracting folder and filename.", ex);
        }
    }

    public static string GetBase64StringFromFile(string fullPath)
    {
        try
        {
            if (_env == null)
            {
                throw new InvalidOperationException("IWebHostEnvironment has not been initialized.");
            }

            string relativePath = fullPath.Replace("\\", "/").TrimStart('/');
            string filePath = Path.Combine(_env.WebRootPath, relativePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(fileBytes);
        }
        catch (Exception ex)
        {
            throw new Exception("Error reading file and converting to base64.", ex);
        }
    }

    public static List<string> ProcessImagePaths(string commaSeparatedPaths)
    {
        List<string> base64List = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedPaths))
            {
                return base64List;
            }

            string[] fullPaths = commaSeparatedPaths.Split(',');

            foreach (var fullPath in fullPaths)
            {
                var trimmedPath = fullPath.Trim();
                if (string.IsNullOrWhiteSpace(trimmedPath))
                {
                    continue;
                }

                try
                {
                    var result = GetFolderAndFilename(trimmedPath);
                    string fileExtension = Path.GetExtension(result.filename);
                    string fileNameWithoutDotInExtension = fileExtension.StartsWith('.') ? fileExtension.Substring(1) : fileExtension;
                    string encodedBase64String = GetBase64StringFromFile(trimmedPath);
                    string newBase64String = $"data:image/{fileNameWithoutDotInExtension};base64,{encodedBase64String}";
                    base64List.Add(newBase64String);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading base64 for {trimmedPath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing image paths: {ex.Message}");
        }

        return base64List;
    }
}

