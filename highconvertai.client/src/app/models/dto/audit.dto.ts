export interface AuditDto {
  id: number;
  userName: string | null;
  role: string | null;
  url: string;
  pageType: string;
  score: number;
  createdAtUtc: string;
  resultJson: any | null;
}
