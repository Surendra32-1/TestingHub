export interface AuthResponse {
  accessToken: string;
  expiresIn: number;
  roles: string[];
  permissions: string[];
}

export interface DevSignInUser {
  userId: number;
  username: string;
  displayName: string;
  roles: string[];
}

export interface CurrentUser {
  userId: number;
  employeeId: number;
  username: string;
  email: string;
  departmentId: number;
  designationId: number;
  roles: string[];
  permissions: string[];
}

export interface AuthSession {
  isAuthenticated: boolean;
  permissions: ReadonlySet<string>;
}
