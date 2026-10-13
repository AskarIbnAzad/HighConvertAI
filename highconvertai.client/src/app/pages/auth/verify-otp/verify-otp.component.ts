import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  ViewChild,
  ElementRef,
  ChangeDetectorRef,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { AuthService } from '../../../services/auth.service';

const SCAN_ID_KEY = 'pendingScanId';
const EMAIL_KEY = 'pendingEmail';

@Component({
  selector: 'app-verify-otp',
  templateUrl: './verify-otp.component.html',
  styleUrls: ['./verify-otp.component.css'],
  standalone: false,
})
export class VerifyOtpComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('otpInput')
  otpInput!: ElementRef<HTMLInputElement>;

  form: FormGroup;

  email = '';

  loading = false;
  resending = false;

  error = '';
  successMsg = '';

  resendCooldown = 60;

  private countdown?: Subscription;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {
    this.form = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    });
  }

  //────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Prefer router state; fall back to sessionStorage (survives refresh/back)
    this.email = history.state?.email ?? sessionStorage.getItem(EMAIL_KEY) ?? '';

    if (!this.email) {
      this.router.navigate(['/register']);
      return;
    }

    this.startCooldown();

    // Auto verify after entering 6 digits
    this.form.get('otp')?.valueChanges.subscribe((value: string) => {
      if (value && value.length === 6 && this.form.valid && !this.loading) {
        this.onSubmit();
      }
    });
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.otpInput?.nativeElement.focus();
    });
  }

  ngOnDestroy(): void {
    this.countdown?.unsubscribe();
  }

  //────────────────────────────────────────────────────────────

  get f() {
    return this.form.controls;
  }

  //────────────────────────────────────────────────────────────
  // Only numbers
  //────────────────────────────────────────────────────────────

  onlyNumbers(event: Event): void {
    const input = event.target as HTMLInputElement;

    const value = input.value.replace(/\D/g, '').substring(0, 6);

    input.value = value;

    this.form.patchValue(
      {
        otp: value,
      },
      {
        emitEvent: true,
      },
    );
  }

  //────────────────────────────────────────────────────────────
  // Verify OTP
  //────────────────────────────────────────────────────────────

  onSubmit(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.successMsg = '';

    this.authService
      .verifyOtp({
        email: this.email,
        otp: this.form.value.otp,
      })
      .subscribe({
        next: () => {
          this.loading = false;

          // Verification successful — clean up, the scan is now linked to the account
          sessionStorage.removeItem(SCAN_ID_KEY);
          sessionStorage.removeItem(EMAIL_KEY);

          this.router.navigate(['/user-dashboard']);
        },

        error: (err) => {
          this.loading = false;

          const message = err.error?.message ?? 'Verification failed. Please try again.';

          this.form.get('otp')?.setErrors({
            serverError: message,
          });

          this.form.get('otp')?.markAsTouched();

          this.error = message;
        },
      });
  }

  //────────────────────────────────────────────────────────────
  // Resend OTP
  //────────────────────────────────────────────────────────────

  resend(): void {
    if (this.resending || this.resendCooldown > 0) {
      return;
    }

    this.resending = true;
    this.error = '';
    this.successMsg = '';

    this.authService
      .resendOtp({
        email: this.email,
      })
      .subscribe({
        next: () => {
          this.resending = false;

          this.successMsg = 'A new verification code has been sent to your email.';

          this.form.reset();

          this.startCooldown();

          setTimeout(() => {
            this.otpInput?.nativeElement.focus();
          });
        },

        error: (err) => {
          this.resending = false;

          this.error = err.error?.message ?? 'Unable to resend verification code.';
        },
      });
  }

  //────────────────────────────────────────────────────────────
  // Countdown
  //────────────────────────────────────────────────────────────

  private startCooldown(seconds = 60): void {
    this.countdown?.unsubscribe();

    this.resendCooldown = seconds;

    this.countdown = interval(1000).subscribe(() => {
      if (this.resendCooldown > 0) {
        this.resendCooldown--;

        this.cdr.detectChanges();
      } else {
        this.countdown?.unsubscribe();
      }
    });
  }
}
