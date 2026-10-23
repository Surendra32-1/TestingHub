using FluentValidation;
using MediatR;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc;
using TeamHub.API.Responses;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.Services;
using TeamHub.Application.Validators.BloodGroup;
using TeamHub.Shared.Common.Services;
using TeamHub.Application.Validators.Department;
using TeamHub.Application.Validators.Designation;
using TeamHub.Application.Validators.AddressType;
using TeamHub.Application.Validators.Gender;
using TeamHub.Application.Validators.JobType;
using TeamHub.Application.Validators.Recruitment;
using TeamHub.Application.Validators.Role;
using TeamHub.Application.Validators.Skill;
using TeamHub.Application.Validators.Team;
using TeamHub.Application.Validators.CotivityDepartment;
using TeamHub.Application.Validators.CotivityDesignation;
using TeamHub.Application.Validators.CotivityEmployee;
using TeamHub.Application.Validators.Employee;
using TeamHub.Application.Validators.BusinessUnit;
using TeamHub.Application.Validators.DressSize;
using TeamHub.Infrastructure;
using TeamHub.Auth;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200", "https://localhost:4200"];

var connectionString = builder.Configuration.GetConnectionString("TeamHub")
    ?? throw new InvalidOperationException(
        "The 'ConnectionStrings:TeamHub' connection string is not configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddTeamHubAuth(builder.Configuration, builder.Environment);
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddScoped<IBloodGroupService, BloodGroupService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<IGenderService, GenderService>();
builder.Services.AddScoped<IAddressTypeService, AddressTypeService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IJobTypeService, JobTypeService>();
builder.Services.AddScoped<ICotivityDepartmentService, CotivityDepartmentService>();
builder.Services.AddScoped<ICotivityDesignationService, CotivityDesignationService>();
builder.Services.AddScoped<ICotivityEmployeeService, CotivityEmployeeService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeReleaseService, EmployeeReleaseService>();
builder.Services.AddScoped<IEmployeeTerminationService, EmployeeTerminationService>();
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssembly(typeof(TeamHub.Application.Features.Employees.GetEmployeeHierarchyQuery).Assembly));
builder.Services.AddScoped<IBusinessUnitService, BusinessUnitService>();
builder.Services.AddScoped<IDressSizeService, DressSizeService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserRoleMatrixService, UserRoleMatrixService>();
builder.Services.AddScoped<IRolePermissionMatrixService, RolePermissionMatrixService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateDepartmentRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateBloodGroupRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateDesignationRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateGenderRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateAddressTypeRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateRoleRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateTeamRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateSkillRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateRecruitmentRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateJobTypeRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateCotivityDepartmentRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateCotivityDesignationRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateCotivityEmployeeRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateEmployeeRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateBusinessUnitRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrUpdateDressSizeRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddApiErrorHandling();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TeamHub API",
        Version = "v1",
        Description = "TeamHub employee and organization management API."
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

FileExtension.Initialize(app.Environment);
FileHelper.Initialize(app.Environment);

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TeamHub API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.Run();
