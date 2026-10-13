import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ScreenshotService {
  private apiUrl = '/api/screenshot';

  constructor(private http: HttpClient) {}

  getDesktopScreenshot(pageScanId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/desktop`, {
      params: { pageScanId: pageScanId.toString() },
      responseType: 'blob',
    });
  }

  getMobileScreenshot(pageScanId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/mobile`, {
      params: { pageScanId: pageScanId.toString() },
      responseType: 'blob',
    });
  }
}
