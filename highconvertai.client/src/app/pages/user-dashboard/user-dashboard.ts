import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { PageScanService } from '../../services/page-scan.service';
import { ChangeDetectorRef } from '@angular/core';
import { AuditService } from '../../services/audit.service';

export interface AuditItem {
  id: number;
  url: string;
  createdAtUtc: string;
  pageType: string;
  score: number;
}

@Component({
  selector: 'app-user-dashboard',
  templateUrl: './user-dashboard.html',
  styleUrls: ['./user-dashboard.css'],
  standalone: false,
})
export class UserDashboardComponent implements OnInit {
  user: any;
  audits: AuditItem[] = [];

  constructor(
    private auditService: AuditService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.user = this.authService.getUser();
    this.loadAudits();
  }

  loadAudits(): void {
    this.auditService.getAuditsByUserId().subscribe({
      next: (res: AuditItem[]) => {
        this.audits = [...res];
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err),
    });
  }

  // =========================================================
  // STATS (computed from real data)
  // =========================================================

  get auditsUsedCount(): number {
    return this.audits.length;
  }

  get averageScore(): string {
    if (this.audits.length === 0) return '—';

    const total = this.audits.reduce((sum, a) => sum + (a.score ?? 0), 0);
    const avg = total / this.audits.length;

    return `${Math.round(avg)}%`;
  }

  // =========================================================
  // RECENT AUDITS (first 5 rows only)
  // =========================================================

  get recentAudits(): AuditItem[] {
    return this.audits.slice(0, 5);
  }

  trackByAudit(index: number, audit: AuditItem): number {
    return audit.id;
  }

  openAudit(id: number): void {
    this.router.navigate(['/result', id]);
  }

  viewAllAudits(): void {
    this.router.navigate(['/user-audit']);
  }

  logout(): void {
    this.authService.logout();
  }

  runAudit(): void {
    this.router.navigate(['/']);
  }
}
