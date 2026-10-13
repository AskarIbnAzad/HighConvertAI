import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrl: './login.css',
  standalone: false,
})
export class LoginComponent {
  model = {
    email: '',
    password: '',
  };

  loading = false;
  error = '';

  showPassword = false;

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  login() {
    if (!this.model.email || !this.model.password) {
      this.error = 'Email and password are required.';
      return;
    }

    this.loading = true;
    this.error = '';

    this.authService.login(this.model).subscribe({
      next: (res: any) => {
        this.authService.setSession(res);

        const isAdmin = res.user.roles?.includes('Admin');

        this.loading = false;

        if (isAdmin) {
          this.router.navigate(['/admin-dashboard']);
        } else {
          this.router.navigate(['/user-dashboard']);
        }
      },

      error: (err: any) => {
        this.loading = false;

        this.error = err.error?.message ?? 'Invalid email or password.';
      },
    });
  }
}
