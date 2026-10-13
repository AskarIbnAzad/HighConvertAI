import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import {
  AuthResponse,
  LoginRequest,
  ResendOtpRequest,
  User,
  VerifyOtpRequest,
} from '../core/auth/models/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  apiUrl = '/api/auth';

  private _accessToken = signal<string | null>(null);
  private _user = signal<User | null>(null);

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

  register(data: any) {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  verifyOtp(payload: VerifyOtpRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/verify-otp`, payload, {
        withCredentials: true,
      })
      .pipe(tap((res) => this.setSession(res)));
  }

  login(payload: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, payload, {
      withCredentials: true,
    });
  }

  logout() {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('user');

    this._accessToken.set(null);
    this._user.set(null);

    this.router.navigate(['/login']);
  }

  getRoles() {
    return this.http.get<any[]>(`${this.apiUrl}/roles`);
  }

  getUser() {
    return JSON.parse(sessionStorage.getItem('user') || '{}');
  }

  isLoggedIn(): boolean {
    return !!sessionStorage.getItem('accessToken');
  }

  resendOtp(payload: ResendOtpRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/resend-otp`, payload);
  }

  setSession(response: AuthResponse) {
    this._accessToken.set(response.accessToken);
    this._user.set(response.user);

    sessionStorage.setItem('accessToken', response.accessToken);
    sessionStorage.setItem('user', JSON.stringify(response.user));
  }

  restoreSession() {
    const token = sessionStorage.getItem('accessToken');
    const user = sessionStorage.getItem('user');

    if (token) {
      this._accessToken.set(token);
    }

    if (user) {
      this._user.set(JSON.parse(user));
    }
  }

  currentToken() {
    return this._accessToken();
  }

  refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${this.apiUrl}/refresh`,
      {},
      {
        withCredentials: true,
      },
    );
  }
}
