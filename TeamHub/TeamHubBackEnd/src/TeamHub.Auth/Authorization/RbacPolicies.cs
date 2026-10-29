namespace TeamHub.Auth.Authorization;

public static class RbacPolicies
{
    public const string ManagePermissions = "rbac:permissions.manage";
    public const string AssignUserRoles = "rbac:user-roles.assign";
    public const string ManageRolePermissions = "rbac:role-permissions.manage";
    public const string SettingsGearEmployeeRole = "setting.gear.EmployeeRole";
    public const string SettingsGearRolePermission = "setting.gear.RolePermission";

    public const string PeopleCreate = "people.create";
    public const string PeopleUpdate = "people.update";
    public const string PeopleRelease = "people.release";
    public const string PeopleView = "people.view";
    public const string PeopleDelete = "people.delete";
    public const string PeopleCardView = "people.cardview";
    public const string AllemployeeSearchView = "allemployee.search.view";
    public const string ProfileView = "profile.view";
    public const string ProfileEdit = "profile.edit";
    public const string ProfileViewOrEdit = "profile.view-or-edit";

    public const string TerminationView = "terminations.view";
    public const string TerminationAdd = "terminations.add";
    public const string TerminationManage = "terminations.manage";

    public const string MasterDepartment = "master.department";
    public const string MasterDesignation = "master.designation";
    public const string MasterBusinessUnit = "master.business-unit";
    public const string MasterDressSize = "master.dress-size";
    public const string MasterCotivityDepartment = "master.cotivity-department";
    public const string MasterCotivityDesignation = "master.cotivity-designation";
    public const string MasterCotivityEmployee = "master.cotivity-employee";
    public const string MasterRole = "master.role";
    public const string MasterAddressType = "master.address-type";
    public const string MasterBloodGroup = "master.blood-group";
    public const string MasterSkills = "master.skills";
    public const string MasterJobType = "master.job-type";
    public const string MasterTeam = "master.team";
    public const string MasterGender = "master.gender";
    public const string MasterRecruitment = "master.recruitment";

    public const string ResignationView = "resignations.view";
    public const string ResignationAdd = "resignations.add";
    public const string ResignationSubmit = "resignations.action.submit";
    public const string ResignationSendForApproval = "resignations.action.sendforapproval";
    public const string ResignationApprove = "resignations.action.approve";
    public const string ResignationReject = "resignations.action.reject";
    public const string ResignationCancel = "resignations.action.cancel";
    public const string ResignationExitChecklist = "resignations.action.exitchecklist";
    public const string ResignationRelease = "resignations.action.release";

    public static readonly IReadOnlyDictionary<string, string> PolicyPermissions = new Dictionary<string, string>
    {
        [ManagePermissions] = "permissions.manage",
        [AssignUserRoles] = SettingsGearEmployeeRole,
        [ManageRolePermissions] = SettingsGearRolePermission,
        [PeopleCreate] = "people.create",
        [PeopleUpdate] = "people.update",
        [PeopleRelease] = "people.release",
        [PeopleView] = "people.view",
        [PeopleDelete] = "people.delete",
        [PeopleCardView] = "people.cardview",
        [AllemployeeSearchView] = "allemployee.search.view",
        [ProfileView] = "profile.view",
        [ProfileEdit] = "profile.edit",
        [TerminationView] = "terminations.view",
        [TerminationAdd] = "terminations.add",
        [TerminationManage] = "terminations.manage",
        [MasterDepartment] = "master.department",
        [MasterDesignation] = "master.designation",
        [MasterBusinessUnit] = "master.business-unit",
        [MasterDressSize] = "master.dress-size",
        [MasterCotivityDepartment] = "master.cotivity-department",
        [MasterCotivityDesignation] = "master.cotivity-designation",
        [MasterCotivityEmployee] = "master.cotivity-employee",
        [MasterRole] = "master.role",
        [MasterAddressType] = "master.address-type",
        [MasterBloodGroup] = "master.blood-group",
        [MasterSkills] = "master.skills",
        [MasterJobType] = "master.job-type",
        [MasterTeam] = "master.team",
        [MasterGender] = "master.gender",
        [MasterRecruitment] = "master.recruitment",
        [ResignationView] = "resignations.view",
        [ResignationAdd] = "resignations.add",
        [ResignationSubmit] = "resignations.action.submit",
        [ResignationSendForApproval] = "resignations.action.sendforapproval",
        [ResignationApprove] = "resignations.action.approve",
        [ResignationReject] = "resignations.action.reject",
        [ResignationCancel] = "resignations.action.cancel",
        [ResignationExitChecklist] = "resignations.action.exitchecklist",
        [ResignationRelease] = "resignations.action.release"
    };
}
