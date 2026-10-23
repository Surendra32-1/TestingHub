export interface EmployeeNode {
  id: number;
  name: string;
}

export interface DesignationTree {
  designation: string;
  employees: EmployeeNode[];
}

export interface DepartmentTree {
  departmentName: string;
  designations: DesignationTree[];
}
