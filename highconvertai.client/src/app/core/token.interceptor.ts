// src/app/core/auth/token.interceptor.ts

import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../app/services/auth.service';
import { switchMap } from 'rxjs/operators';

@Injectable()
export class TokenInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.currentToken();

    const authReq = token
      ? req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        })
      : req;

    return next.handle(authReq).pipe(
      catchError((err: HttpErrorResponse) => {
        debugger;
        if (err.status === 401) {
          // Prevent infinite refresh loop
          if (req.url.includes('/refresh')) {
            this.authService.logout();
            return throwError(() => err);
          }

          return this.authService.refresh().pipe(
            switchMap((response) => {
              debugger;
              this.authService.setSession(response);

              const retryReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${response.accessToken}`,
                },
              });

              return next.handle(retryReq);
            }),

            catchError(() => {
              this.authService.logout();

              return throwError(() => err);
            }),
          );
        }
        return throwError(() => err);
      }),
    );
  }
}
