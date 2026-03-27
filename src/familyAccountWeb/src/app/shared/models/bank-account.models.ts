export interface BankAccountDto {
  idBankAccount: number;
  idBank: number;
  codeBank: string;
  nameBank: string;
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  idCurrency: number;
  codeCurrency: string;
  nameCurrency: string;
  codeBankAccount: string;
  accountNumber: string;
  accountHolder: string;
  isActive: boolean;
}

export interface CreateBankAccountRequest {
  idBank: number;
  idAccount: number;
  idCurrency: number;
  codeBankAccount: string;
  accountNumber: string;
  accountHolder: string;
  isActive: boolean;
}

export interface UpdateBankAccountRequest {
  idBank: number;
  idAccount: number;
  idCurrency: number;
  codeBankAccount: string;
  accountNumber: string;
  accountHolder: string;
  isActive: boolean;
}
