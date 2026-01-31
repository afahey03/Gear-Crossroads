using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace GearCrossroads.Api.Services;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
    Task<string> UploadImageAsync(Stream stream, string fileName, string folder);
    Task DeleteImageAsync(string publicId);
}

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        // Try to get CLOUDINARY_URL environment variable first (Cloudinary's recommended format)
        var cloudinaryUrl = config["CLOUDINARY_URL"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_URL");

        if (!string.IsNullOrEmpty(cloudinaryUrl))
        {
            try
            {
                _cloudinary = new Cloudinary(cloudinaryUrl);
                _logger.LogInformation("Cloudinary service initialized successfully from CLOUDINARY_URL");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Cloudinary from CLOUDINARY_URL");
            }
        }

        // Fallback to individual configuration values
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Cloudinary configuration is missing. Set either CLOUDINARY_URL or Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret.");
            _cloudinary = null!;
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _logger.LogInformation("Cloudinary service initialized successfully with cloud: {CloudName}", cloudName);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        using var stream = file.OpenReadStream();
        return await UploadImageAsync(stream, file.FileName, folder);
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, string folder)
    {
        if (_cloudinary == null)
        {
            throw new InvalidOperationException("Cloudinary is not configured. Please set Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret in your configuration.");
        }

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = $"gear-crossroads/{folder}",
                Transformation = new Transformation()
                    .Width(1200)
                    .Height(1200)
                    .Crop("limit")
                    .Quality("auto"),
                UseFilename = true,
                UniqueFilename = true
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                throw new Exception($"Image upload failed: {result.Error.Message}");
            }

            return result.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image to Cloudinary");
            throw;
        }
    }

    public async Task DeleteImageAsync(string publicId)
    {
        try
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return;
            }

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Error != null)
            {
                _logger.LogWarning("Cloudinary deletion warning: {Error}", result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image from Cloudinary");
            // Don't throw - deletion failures shouldn't break the app
        }
    }
}