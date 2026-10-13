import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuditDto } from '../models/dto/audit.dto';

@Injectable({
  providedIn: 'root',
})
export class AuditService {
  private apiUrl = '/api/audits';

  constructor(private http: HttpClient) {}

  getAllAudits(): Observable<AuditDto[]> {
    return this.http.get<AuditDto[]>(`${this.apiUrl}/get-all-audits`);
  }

  getAuditsByUserId(): Observable<AuditDto[]> {
    return this.http.get<AuditDto[]>(`${this.apiUrl}/get-audits-by-user-id`);
  }

  getAuditById(id: number): Observable<AuditDto> {
    return this.http.get<AuditDto>(`${this.apiUrl}/get-audit-by-id?id=${id}`);
  }
}
