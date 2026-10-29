export interface Employee {
  id: number;
  eNumber?: number;
  reportsToEmployeeId?: number | null;
  reportsToEmployee?: string | null;
  name: string;
  designation: string;
  designationRank: number;
  department: string;
  roleName?: string | null;
  joinedDate?: string | null;
  primarySkill?: string | null;
  isActive?: boolean;
  isDeleted?: boolean;
  teamName: string;
  businessUnitId?: number | null;
  businessUnitName?: string | null;
  imageBase64: string;
  accent:string
  
}
