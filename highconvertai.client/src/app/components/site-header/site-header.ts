import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-site-header',
  templateUrl: './site-header.html',
  styleUrls: ['./site-header.css'],
  standalone: false,
})
export class SiteHeaderComponent {
  constructor(
    public authService: AuthService,
    private router: Router,
  ) {}

  get user() {
    return JSON.parse(sessionStorage.getItem('user') || 'null');
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }
  get getUserType(): string {
    if (this.user.roles?.includes('Admin')) {
      return 'Admin';
    } else {
      return 'User';
    }
  }

  goDashboard() {
    if (!this.user) return;

    if (this.user.roles?.includes('Admin')) this.router.navigate(['/admin-dashboard']);
    else this.router.navigate(['/user-dashboard']);
  }

  logout() {
    this.authService.logout();
  }
}
