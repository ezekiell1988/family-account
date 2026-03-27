export interface BankDto {
  idBank:   number;
  codeBank: string;
  nameBank: string;
  isActive: boolean;
}

export interface CreateBankRequest {
  codeBank: string;
  nameBank: string;
  isActive: boolean;
}

export interface UpdateBankRequest {
  codeBank: string;
  nameBank: string;
  isActive: boolean;
}
