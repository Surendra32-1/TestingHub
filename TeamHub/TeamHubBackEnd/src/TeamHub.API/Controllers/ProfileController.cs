using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Auth.Authorization;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Shared.Responses;

namespace TeamHub.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController(IEmployeeProfileService profileService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = RbacPolicies.ProfileViewOrEdit)]
    [ProducesResponseType<ApiResponse<EmployeeProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> GetMyProfile(
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
        {
            return Unauthorized();
        }

        var profile = await profileService.GetByEmployeeIdAsync(employeeId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Profile not found",
                Detail = "No active employee profile is associated with the authenticated account.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new ApiResponse<EmployeeProfileDto>
        {
            Success = true,
            Message = "Profile retrieved successfully.",
            Data = profile
        });
    }

    [HttpGet("me/summary")]
    [ProducesResponseType<ApiResponse<EmployeeProfileSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmployeeProfileSummaryDto>>> GetMySummary(
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
        {
            return Unauthorized();
        }

        var summary = await profileService.GetSummaryAsync(employeeId, cancellationToken);
        if (summary is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Profile not found",
                Detail = "No active employee profile is associated with the authenticated account.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new ApiResponse<EmployeeProfileSummaryDto>
        {
            Success = true,
            Message = "Profile summary retrieved successfully.",
            Data = summary
        });
    }

    [HttpPatch("me")]
    [Authorize(Policy = RbacPolicies.ProfileEdit)]
    [ProducesResponseType<ApiResponse<EmployeeProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> UpdateMyProfile(
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
        {
            return Unauthorized();
        }

        if (!TryReadPatch(body, out var patch, out var errors))
        {
            return BadRequest(ApiProblemDetailsFactory.Validation(
                errors,
                "Profile update validation failed",
                HttpContext.Request.Path));
        }

        var result = await profileService.UpdateAsync(employeeId, patch, cancellationToken);
        if (result.Errors.Count > 0)
        {
            return BadRequest(ApiProblemDetailsFactory.Validation(
                new Dictionary<string, string[]>(result.Errors),
                "Profile update validation failed",
                HttpContext.Request.Path));
        }

        if (result.Profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Profile not found",
                Detail = "No active employee profile is associated with the authenticated account.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new ApiResponse<EmployeeProfileDto>
        {
            Success = true,
            Message = "Profile updated successfully.",
            Data = result.Profile
        });
    }

    private bool TryGetEmployeeId(out int employeeId) =>
        int.TryParse(User.FindFirstValue("EmployeeId"), out employeeId) && employeeId > 0;

    private static bool TryReadPatch(
        JsonElement body,
        out EmployeeProfilePatch patch,
        out Dictionary<string, string[]> errors)
    {
        patch = new EmployeeProfilePatch(
            false, null, false, 0, false, null, false, null, false, null, false, null,
            false, 0, false, 0, false, null, false, [], false, [],
            false, null, false, null, false, null, false, null);
        errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (body.ValueKind != JsonValueKind.Object)
        {
            errors["profile"] = ["A JSON object containing editable profile fields is required."];
            return false;
        }

        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in body.EnumerateObject())
        {
            if (!values.TryAdd(property.Name, property.Value))
            {
                errors[property.Name] = ["A profile field may only be specified once."];
            }
        }

        var allowedFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "alternateEmail",
            "bloodGroupId",
            "mobileNumber",
            "emergencyContactNumber",
            "motherName",
            "spouseName",
            "latitude",
            "longitude",
            "imageBase64",
            "skills",
            "dressIds",
            "joinedDate",
            "joinedDateBS",
            "dateOfBirth",
            "dateOfBirthBS"
        };
        foreach (var name in values.Keys)
        {
            if (!allowedFields.Contains(name))
            {
                errors[name] = ["This field cannot be changed through self-service profile editing."];
            }
        }

        var valid = errors.Count == 0;
        var alternateEmail = ReadOptionalString(values, "alternateEmail", errors, ref valid);
        var bloodGroupId = ReadOptionalInt(values, "bloodGroupId", errors, ref valid);
        var mobileNumber = ReadOptionalString(values, "mobileNumber", errors, ref valid);
        var emergencyContactNumber = ReadOptionalString(values, "emergencyContactNumber", errors, ref valid);
        var motherName = ReadOptionalString(values, "motherName", errors, ref valid);
        var spouseName = ReadOptionalString(values, "spouseName", errors, ref valid);
        var latitude = ReadOptionalDecimal(values, "latitude", errors, ref valid);
        var longitude = ReadOptionalDecimal(values, "longitude", errors, ref valid);
        var imageBase64 = ReadOptionalString(values, "imageBase64", errors, ref valid);
        var skills = ReadOptionalSkills(values, errors, ref valid);
        var dressIds = ReadOptionalIntArray(values, "dressIds", errors, ref valid);
        var joinedDate = ReadOptionalDateOnly(values, "joinedDate", errors, ref valid);
        var joinedDateBS = ReadOptionalString(values, "joinedDateBS", errors, ref valid);
        var dateOfBirth = ReadOptionalDateOnly(values, "dateOfBirth", errors, ref valid);
        var dateOfBirthBS = ReadOptionalString(values, "dateOfBirthBS", errors, ref valid);

        patch = new EmployeeProfilePatch(
            values.ContainsKey("alternateEmail"), alternateEmail,
            values.ContainsKey("bloodGroupId"), bloodGroupId ?? 0,
            values.ContainsKey("mobileNumber"), mobileNumber,
            values.ContainsKey("emergencyContactNumber"), emergencyContactNumber,
            values.ContainsKey("motherName"), motherName,
            values.ContainsKey("spouseName"), spouseName,
            values.ContainsKey("latitude"), latitude ?? 0,
            values.ContainsKey("longitude"), longitude ?? 0,
            values.ContainsKey("imageBase64"), imageBase64,
            values.ContainsKey("skills"), skills,
            values.ContainsKey("dressIds"), dressIds,
            values.ContainsKey("joinedDate"), joinedDate,
            values.ContainsKey("joinedDateBS"), joinedDateBS,
            values.ContainsKey("dateOfBirth"), dateOfBirth,
            values.ContainsKey("dateOfBirthBS"), dateOfBirthBS);

        if (values.Count == 0)
        {
            errors["profile"] = ["At least one editable field is required."];
            valid = false;
        }

        return valid;
    }

    private static IReadOnlyList<EmployeeProfileSkillUpdateDto> ReadOptionalSkills(
        IReadOnlyDictionary<string, JsonElement> values,
        IDictionary<string, string[]> errors,
        ref bool valid)
    {
        if (!values.TryGetValue("skills", out var value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            errors["skills"] = ["This field must be an array."];
            valid = false;
            return [];
        }

        var skills = new List<EmployeeProfileSkillUpdateDto>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("skillId", out var skillIdValue)
                || !skillIdValue.TryGetInt32(out var skillId)
                || !item.TryGetProperty("skillLevel", out var skillLevelValue)
                || !skillLevelValue.TryGetInt32(out var skillLevel))
            {
                errors["skills"] = ["Each skill must include an integer skillId and skillLevel."];
                valid = false;
                continue;
            }

            skills.Add(new EmployeeProfileSkillUpdateDto(skillId, skillLevel));
        }

        return skills;
    }

    private static IReadOnlyList<int> ReadOptionalIntArray(
        IReadOnlyDictionary<string, JsonElement> values,
        string name,
        IDictionary<string, string[]> errors,
        ref bool valid)
    {
        if (!values.TryGetValue(name, out var value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            errors[name] = ["This field must be an array of integers."];
            valid = false;
            return [];
        }

        var ids = new List<int>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var id))
            {
                errors[name] = ["This field must be an array of integers."];
                valid = false;
                continue;
            }

            ids.Add(id);
        }

        return ids;
    }

    private static string? ReadOptionalString(
        IReadOnlyDictionary<string, JsonElement> values,
        string name,
        IDictionary<string, string[]> errors,
        ref bool valid)
    {
        if (!values.TryGetValue(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        errors[name] = ["This field must be a string or null."];
        valid = false;
        return null;
    }

    private static int? ReadOptionalInt(
        IReadOnlyDictionary<string, JsonElement> values,
        string name,
        IDictionary<string, string[]> errors,
        ref bool valid)
    {
        if (!values.TryGetValue(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
        {
            return result;
        }

        errors[name] = ["This field must be an integer."];
        valid = false;
        return null;
    }

    private static decimal? ReadOptionalDecimal(
        IReadOnlyDictionary<string, JsonElement> values,
        string name,
        IDictionary<string, string[]> errors,
        ref bool valid)
    {
        if (!values.TryGetValue(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var result))
        {
            return result;
        }

        errors[name] = ["This field must be a number."];
        valid = false;
        return null;
    }

    private static DateOnly? ReadOptionalDateOnly(
        IReadOnlyDictionary<string, JsonElement> values,
        string name,
        IDictionary<string, string[]> errors,
        ref bool valid)
    {
        if (!values.TryGetValue(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
        {
            return result;
        }

        errors[name] = ["This field must be an ISO date (yyyy-MM-dd) or null."];
        valid = false;
        return null;
    }
}
