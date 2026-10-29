using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Shared.Common.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace TeamHub.Application.Services;

public sealed class EmployeeProfileService(IEmployeeProfileRepository repository) : IEmployeeProfileService
{
    private static readonly Regex PhonePattern = new(@"^\+?[0-9][0-9\s().-]{5,19}$", RegexOptions.Compiled);

    public Task<EmployeeProfileDto?> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default) =>
        repository.GetByEmployeeIdAsync(employeeId, cancellationToken);

    public Task<EmployeeProfileSummaryDto?> GetSummaryAsync(
        int employeeId,
        CancellationToken cancellationToken = default) =>
        repository.GetSummaryAsync(employeeId, cancellationToken);

    public async Task<EmployeeProfileUpdateResult> UpdateAsync(
        int employeeId,
        EmployeeProfilePatch patch,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!patch.AlternateEmailSpecified
            && !patch.BloodGroupIdSpecified
            && !patch.MobileNumberSpecified
            && !patch.EmergencyContactNumberSpecified
            && !patch.MotherNameSpecified
            && !patch.SpouseNameSpecified
            && !patch.LatitudeSpecified
            && !patch.LongitudeSpecified
            && !patch.ImageBase64Specified
            && !patch.SkillsSpecified
            && !patch.DressIdsSpecified)
        {
            errors["profile"] = ["At least one editable field is required."];
            return new EmployeeProfileUpdateResult(null, errors);
        }

        if (patch.AlternateEmailSpecified
            && !string.IsNullOrWhiteSpace(patch.AlternateEmail)
            && (!new EmailAddressAttribute().IsValid(patch.AlternateEmail)
                || patch.AlternateEmail.Length > 150))
        {
            errors["alternateEmail"] = ["Enter a valid email address up to 150 characters."];
        }

        ValidatePhone(errors, "mobileNumber", patch.MobileNumberSpecified, patch.MobileNumber);
        ValidatePhone(errors, "emergencyContactNumber", patch.EmergencyContactNumberSpecified, patch.EmergencyContactNumber);
        ValidateOptionalName(errors, "motherName", patch.MotherNameSpecified, patch.MotherName);
        ValidateOptionalName(errors, "spouseName", patch.SpouseNameSpecified, patch.SpouseName);

        if (patch.LatitudeSpecified && patch.Latitude is < -90 or > 90)
        {
            errors["latitude"] = ["Latitude must be between -90 and 90."];
        }

        if (patch.LongitudeSpecified && patch.Longitude is < -180 or > 180)
        {
            errors["longitude"] = ["Longitude must be between -180 and 180."];
        }

        if (patch.ImageBase64Specified && !string.IsNullOrWhiteSpace(patch.ImageBase64)
            && !IsSupportedImage(patch.ImageBase64))
        {
            errors["imageBase64"] = ["Choose a valid JPEG, PNG, or GIF image up to 5 MB."];
        }

        if (patch.BloodGroupIdSpecified
            && !await repository.IsActiveBloodGroupAsync(patch.BloodGroupId, cancellationToken))
        {
            errors["bloodGroupId"] = ["Select an active blood group."];
        }

        if (patch.SkillsSpecified)
        {
            var skillIds = patch.Skills.Select(skill => skill.SkillId).ToArray();
            if (patch.Skills.Any(skill => skill.SkillId <= 0 || skill.SkillLevel is < 1 or > 5)
                || skillIds.Distinct().Count() != skillIds.Length
                || !await repository.AreSelectableSkillsAsync(employeeId, skillIds, cancellationToken))
            {
                errors["skills"] = ["Select available skills and provide a level from 1 to 5 for each skill."];
            }
        }

        if (patch.DressIdsSpecified)
        {
            if (patch.DressIds.Any(dressId => dressId <= 0)
                || patch.DressIds.Distinct().Count() != patch.DressIds.Count
                || !await repository.AreSelectableDressSizesAsync(employeeId, patch.DressIds, cancellationToken))
            {
                errors["dressIds"] = ["Select available dress sizes."];
            }
        }

        if (errors.Count > 0)
        {
            return new EmployeeProfileUpdateResult(null, errors);
        }

        var normalizedPatch = patch with
        {
            AlternateEmail = patch.AlternateEmailSpecified
                ? NormalizeEmail(patch.AlternateEmail)
                : patch.AlternateEmail,
            MobileNumber = patch.MobileNumberSpecified
                ? NormalizeOptionalText(patch.MobileNumber)
                : patch.MobileNumber,
            EmergencyContactNumber = patch.EmergencyContactNumberSpecified
                ? NormalizeOptionalText(patch.EmergencyContactNumber)
                : patch.EmergencyContactNumber,
            MotherName = patch.MotherNameSpecified
                ? NormalizeOptionalText(patch.MotherName)
                : patch.MotherName,
            SpouseName = patch.SpouseNameSpecified
                ? NormalizeOptionalText(patch.SpouseName)
                : patch.SpouseName
        };

        if (!await repository.UpdateAsync(employeeId, normalizedPatch, cancellationToken))
        {
            return new EmployeeProfileUpdateResult(null, new Dictionary<string, string[]>
            {
                ["profile"] = ["The profile could not be updated."]
            });
        }

        if (patch.ImageBase64Specified && !string.IsNullOrWhiteSpace(patch.ImageBase64))
        {
            var imagePath = patch.ImageBase64.ToFileUrl(
                FolderName.Infinite_Employee_FOLDER,
                $"employee-{employeeId}-{Guid.NewGuid():N}");
            await repository.AddImageAsync(employeeId, imagePath, cancellationToken);
        }

        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken);
        return new EmployeeProfileUpdateResult(profile, new Dictionary<string, string[]>());
    }

    private static void ValidatePhone(
        IDictionary<string, string[]> errors,
        string field,
        bool specified,
        string? value)
    {
        if (specified
            && !string.IsNullOrWhiteSpace(value)
            && (value.Length > 20 || !PhonePattern.IsMatch(value.Trim())))
        {
            errors[field] = ["Enter a valid phone number up to 20 characters."];
        }
    }

    private static void ValidateOptionalName(
        IDictionary<string, string[]> errors,
        string field,
        bool specified,
        string? value)
    {
        if (specified && value?.Trim().Length > 150)
        {
            errors[field] = ["Enter a name up to 150 characters."];
        }
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static bool IsSupportedImage(string imageData)
    {
        const int maxImageBytes = 5 * 1024 * 1024;
        var separator = imageData.IndexOf(',');
        if (separator < 0 || imageData.Length > ((maxImageBytes + 2) / 3 * 4) + 128)
        {
            return false;
        }

        var metadata = imageData[..separator];
        if (!metadata.Equals("data:image/jpeg;base64", StringComparison.OrdinalIgnoreCase)
            && !metadata.Equals("data:image/png;base64", StringComparison.OrdinalIgnoreCase)
            && !metadata.Equals("data:image/gif;base64", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(imageData[(separator + 1)..]);
            if (bytes.Length == 0 || bytes.Length > maxImageBytes)
            {
                return false;
            }

            var imageInfo = Image.Identify(bytes);
            return imageInfo is not null && MatchesDeclaredImageFormat(metadata, imageInfo.Metadata.DecodedImageFormat);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (UnknownImageFormatException)
        {
            return false;
        }
        catch (InvalidImageContentException)
        {
            return false;
        }
    }

    private static bool MatchesDeclaredImageFormat(string metadata, IImageFormat? imageFormat) =>
        imageFormat?.Name switch
        {
            "JPEG" => metadata.Equals("data:image/jpeg;base64", StringComparison.OrdinalIgnoreCase),
            "PNG" => metadata.Equals("data:image/png;base64", StringComparison.OrdinalIgnoreCase),
            "GIF" => metadata.Equals("data:image/gif;base64", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
}
