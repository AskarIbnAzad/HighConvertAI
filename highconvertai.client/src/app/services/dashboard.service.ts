import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserDto } from '../models/dto/user.dto';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private apiUrl = '/api/dashboard';

  constructor(private http: HttpClient) {}

  getAdminOverview(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/get-admin-overview`);
  }
}
