export interface RoleOption {
  roleId: number;
  roleName: string;
}

export interface EmployeeRoleMatrixRow {
  userId: number;
  employeeId: number;
  username: string;
  fullName: string;
  roleIds: number[];
}

export interface EmployeeRoleMatrix {
  roles: RoleOption[];
  rows: EmployeeRoleMatrixRow[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface UserRoleAssignment {
  userId: number;
  roleIds: number[];
}

export interface SaveUserRolesRequest {
  assignments: UserRoleAssignment[];
}

export interface RolePermissionMatrixRow {
  permissionId: number;
  permissionCode: string;
  permissionName: string;
  module: string | null;
  roleIds: number[];
}

export interface RolePermissionMatrix {
  roles: RoleOption[];
  rows: RolePermissionMatrixRow[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RolePermissionAssignment {
  permissionId: number;
  roleIds: number[];
}

export interface SaveRolePermissionsRequest {
  assignments: RolePermissionAssignment[];
}

export interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}
