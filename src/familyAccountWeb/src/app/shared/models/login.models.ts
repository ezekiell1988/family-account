// ========================================
// LOGIN (Usuarios del sistema)
// ========================================

export interface LoginDto {
  idLogin: number;
  codeLogin: string;
  nameLogin: string;
  phoneLogin: string;
  emailLogin: string;
}

export interface CreateLoginRequest {
  codeLogin: string;
  nameLogin: string;
  phoneLogin: string;
  emailLogin: string;
}

export interface UpdateLoginRequest {
  nameLogin: string;
  phoneLogin: string;
  emailLogin: string;
}

// ========================================
// ROLES
// ========================================

export interface RoleDto {
  idRole: number;
  codeRole: string;
  nameRole: string;
}

export interface LoginRoleAssignmentDto {
  idClientCustomerLoginRole: number;
  idClientCustomerLogin: number;
  idRole: number;
  codeRole: string;
  nameRole: string;
}

export interface ClientCustomerLoginSummaryDto {
  idClientCustomerLogin: number;
  idClientCustomer: number;
}

export interface AssignLoginRoleRequest {
  idClientCustomerLogin: number;
  idRole: number;
}
