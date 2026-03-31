export interface ContactDto {
  idContact:   number;
  codeContact: string;
  name:        string;
}

export interface GetOrCreateContactRequest {
  name:            string;
  contactTypeCode: string;
}
