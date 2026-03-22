export interface AccountDto {
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  typeAccount: string;
  levelAccount: number;
  idAccountParent: number | null;
  nameAccountParent: string | null;
  allowsMovements: boolean;
  isActive: boolean;
}

export interface CreateAccountRequest {
  codeAccount: string;
  nameAccount: string;
  typeAccount: string;
  levelAccount: number;
  idAccountParent: number | null;
  allowsMovements: boolean;
  isActive: boolean;
}

export interface UpdateAccountRequest {
  codeAccount: string;
  nameAccount: string;
  typeAccount: string;
  levelAccount: number;
  idAccountParent: number | null;
  allowsMovements: boolean;
  isActive: boolean;
}
