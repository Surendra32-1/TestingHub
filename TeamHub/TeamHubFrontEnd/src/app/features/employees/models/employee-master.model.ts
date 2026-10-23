export interface Department {
  departmentId: number;
  departmentName: string;
}

export interface Designation {
  designationId: number;
  designationName: string;
  rank: number;
}

export interface JobType {
  jobTypeId: number;
  jobTypeName: string;
}

export interface BloodGroup {
  bloodGroupId: number;
  bloodGroupName: string;
}

export interface Gender {
  genderId: number;
  genderName: string;
}

export interface Role {
  roleId: number;
  roleName: string;
}

export interface Team {
  teamId: number;
  teamName: string;
}

export interface Skill {
  skillId: number;
  skillName: string;
}

export interface Recruitment {
  recruitmentId: number;
  recruitmentName: string;
}

export interface ReportingManager {
  employeeId: number;
  employeeName: string;
}

export interface EmployeeMastersResponse {
  departments: Department[];
  designations: Designation[];
  jobTypes: JobType[];
  bloodGroups: BloodGroup[];
  genders: Gender[];
  roles: Role[];
  teams: Team[];
  skills: Skill[];
  recruitments: Recruitment[];
  reportingManagers: ReportingManager[];
}