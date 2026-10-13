import { Component, EventEmitter, Input, Output } from '@angular/core';

import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login-modal',
  templateUrl: './login-modal.html',
  styleUrls: ['./login-modal.css'],
  standalone: false,
})
export class LoginModalComponent {
  @Input() visible = false;

  @Output() close = new EventEmitter<void>();

  model: any = {};

  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  closeModal(): void {
    this.close.emit();
  }
  login(): void {
    console.log('Login not implemented yet');
  }
  //login(): void {

  //  if (this.isLoading) {
  //    return;
  //  }

  //  this.isLoading = true;

  //  this.authService.login(this.model)
  //    .subscribe({

  //      next: (res: any) => {

  //        sessionStorage.setItem(
  //          'token',
  //          res.token
  //        );

  //        sessionStorage.setItem(
  //          'user',
  //          JSON.stringify(res.user)
  //        );

  //        this.isLoading = false;

  //        this.closeModal();

  //        if (res.user.role === 'Admin') {

  //          this.router.navigate([
  //            '/admin-dashboard'
  //          ]);
  //        }
  //        else {

  //          this.router.navigate([
  //            '/user-dashboard'
  //          ]);
  //        }
  //      },

  //      error: (err) => {

  //        this.isLoading = false;

  //        alert(
  //          err.error || 'Login failed'
  //        );
  //      }
  //    });
  //}
}
