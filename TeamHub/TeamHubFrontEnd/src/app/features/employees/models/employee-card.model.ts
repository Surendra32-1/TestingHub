export interface EmployeeCard {
  id: number;
  eNumber: number;
  name: string;
  designation: string | null;
  department: string | null;
  teamName: string | null;
  email: string;
  phoneNumber: string | null;
  extensionNumber: number;
  mobileNumber: string | null;
  stationNumber: string | null;
  reportsToEmployee: string | null;
  imageBase64: string | null;
}

export interface EmployeeCardFilters {
  name?: string;
  designation?: string;
  departmentId?: number | null;
  contact?: string;
}
