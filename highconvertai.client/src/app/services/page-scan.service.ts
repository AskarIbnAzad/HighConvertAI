import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ScanAndAnalyzeResponseDto } from '../models/dto/scan-analyze-response.dto';

@Injectable({
  providedIn: 'root',
})
export class PageScanService {
  private apiUrl = '/api/PageScans';

  constructor(private http: HttpClient) {}

  scanAndAnalyze(url: string): Observable<ScanAndAnalyzeResponseDto> {
    return this.http.post<ScanAndAnalyzeResponseDto>(
      `${this.apiUrl}/scan-and-analyze?url=${encodeURIComponent(url)}`,
      {},
    );
  }
}
