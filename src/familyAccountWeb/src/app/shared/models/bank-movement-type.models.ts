export interface BankMovementTypeDto {
  idBankMovementType:   number;
  codeBankMovementType: string;
  nameBankMovementType: string;
  idAccountCounterpart:   number;
  codeAccountCounterpart: string;
  nameAccountCounterpart: string;
  movementSign: string;
  isActive:     boolean;
}

export interface CreateBankMovementTypeRequest {
  codeBankMovementType: string;
  nameBankMovementType: string;
  idAccountCounterpart: number;
  movementSign: string;
  isActive:     boolean;
}

export interface UpdateBankMovementTypeRequest {
  codeBankMovementType: string;
  nameBankMovementType: string;
  idAccountCounterpart: number;
  movementSign: string;
  isActive:     boolean;
}
