import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';
import Swal from 'sweetalert2';

import { AuthService } from '../../services/auth.service';
import { AuditService } from '../../services/audit.service';
import { ScreenshotService } from '../../services/screenshot.service';

@Component({
  selector: 'app-audit-result',
  templateUrl: './audit-result.html',
  styleUrls: ['./audit-result.css'],
  standalone: false,
})
export class AuditResultComponent implements OnInit, OnDestroy {
  result: any = null;
  url = '';

  desktopSelected = true;
  isSubmitting = false;
  selectedGroupIndex = 0;
  currentDate = new Date();

  pageScanId = 0;

  desktopScreenshotUrl: string | null = null;
  mobileScreenshotUrl: string | null = null;

  desktopScreenshotLoading = true;
  mobileScreenshotLoading = true;

  registerModel: any = {
    name: '',
    email: '',
    password: '',
    roleId: 2,
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private auditService: AuditService,
    private screenshotService: ScreenshotService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id) {
      this.router.navigate(['/']);
      return;
    }

    this.pageScanId = id;

    this.loadAudit(id);
    this.loadScreenshots(id);
  }

  ngOnDestroy(): void {
    this.revokeObjectUrl(this.desktopScreenshotUrl);
    this.revokeObjectUrl(this.mobileScreenshotUrl);
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  goToRegister(): void {
    this.router.navigate(['/register'], {
      queryParams: { scanId: this.pageScanId },
    });
  }

  private loadAudit(id: number): void {
    this.auditService.getAuditById(id).subscribe({
      next: (res) => {
        this.result = res.resultJson;
        this.url = res.resultJson?.url ?? '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load audit:', err);
      },
    });
  }

  private loadScreenshots(pageScanId: number): void {
    this.desktopScreenshotLoading = true;
    this.mobileScreenshotLoading = true;

    this.screenshotService.getDesktopScreenshot(pageScanId).subscribe({
      next: (blob: Blob) => {
        this.revokeObjectUrl(this.desktopScreenshotUrl);
        this.desktopScreenshotUrl = URL.createObjectURL(blob);
        this.desktopScreenshotLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Desktop screenshot failed:', err);
        this.desktopScreenshotLoading = false;
        this.cdr.detectChanges();
      },
    });

    this.screenshotService.getMobileScreenshot(pageScanId).subscribe({
      next: (blob: Blob) => {
        this.revokeObjectUrl(this.mobileScreenshotUrl);
        this.mobileScreenshotUrl = URL.createObjectURL(blob);
        this.mobileScreenshotLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Mobile screenshot failed:', err);
        this.mobileScreenshotLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private revokeObjectUrl(url: string | null): void {
    if (url) {
      URL.revokeObjectURL(url);
    }
  }

  get groups(): any[] {
    return this.result?.issueGroup ?? [];
  }

  get pageType(): string {
    return this.result?.pageType ?? '';
  }

  get estimatedRevenueIncrease(): number {
    return this.result?.affectedRevenueOpportunity ?? 0;
  }

  getGroupLabel(name: string): string {
    return name ?? '';
  }

  selectGroup(index: number): void {
    this.selectedGroupIndex = index;

    const el = document.getElementById('group-section-' + index);

    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  getSeverityClass(severityName: string): string {
    switch ((severityName ?? '').toLowerCase()) {
      case 'high':
        return 'sev-high';
      case 'medium':
        return 'sev-medium';
      case 'low':
        return 'sev-low';
      default:
        return 'sev-low';
    }
  }

  selectDesktop(): void {
    if (this.desktopSelected) {
      return;
    }

    this.desktopSelected = true;
  }

  selectMobile(): void {
    if (!this.desktopSelected) {
      return;
    }

    this.desktopSelected = false;
  }

  register(): void {
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;

    this.authService.register(this.registerModel).subscribe({
      next: () => {
        const loginModel = {
          email: this.registerModel.email,
          password: this.registerModel.password,
        };

        this.authService.login(loginModel).subscribe({
          next: (res: any) => {
            sessionStorage.setItem('token', res.token);
            sessionStorage.setItem('user', JSON.stringify(res.user));
            this.isSubmitting = false;
            this.router.navigate(['/user-dashboard']);
          },
          error: () => {
            this.isSubmitting = false;
            alert('Registration successful. Please login.');
          },
        });
      },
      error: (err) => {
        this.isSubmitting = false;

        const message =
          err?.error?.message || err?.error?.title || err?.message || 'Registration failed.';

        Swal.fire({
          icon: 'error',
          title: 'Registration Failed',
          text: message,
          confirmButtonText: 'OK',
          confirmButtonColor: '#ef4444',
        });
      },
    });
  }
}
