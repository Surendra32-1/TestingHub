export interface EmployeeHierarchyPerson {
  employeeId: number;
  parentId: number | null;
  employeeName: string;
  designationName: string | null;
  departmentName: string | null;
  imagePath: string | null;
  employeeNumber: number;
  source: 'Employee';
  level: number;
}
export interface EmployeeHierarchyTreeNode extends EmployeeHierarchyPerson {
  reports: EmployeeHierarchyTreeNode[];
}

export interface HierarchyNodeView {
  id: string;
  name: string;
  designation: string | null;
  department: string | null;
  employeeNumber: number | null;
  imageUrl: string | null;
}

export interface HierarchyConnection {
  id: string;
  fromId: string;
  toId: string;
  route?: 'direct';
}

export interface CotivityHierarchyPerson {
  cotivityEmployeeId: number;
  parentId: number | null;
  cotivityEmployeeName: string;
  cotivityDesignationName: string | null;
  cotivityDepartmentName: string | null;
  imagePath: string | null;
  source: 'Cotivity';
  level: number;
}

export interface EmployeeHierarchyResponse {
  employeeManagers: EmployeeHierarchyPerson[];
  selectedEmployee: EmployeeHierarchyPerson;
  cotivityHierarchy: CotivityHierarchyPerson[];
  reportingHierarchy: EmployeeHierarchyPerson[];
}
