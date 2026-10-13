import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

const SCAN_ID_KEY = 'pendingScanId';
const EMAIL_KEY = 'pendingEmail';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  standalone: false,
})
export class RegisterComponent implements OnInit {
  form: FormGroup;
  loading = false;
  error = '';
  showPassword = false;
  showConfirm = false;

  initialPageScanId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.form = this.fb.group(
      {
        fullName: ['', [Validators.required, Validators.maxLength(100)]],
        email: ['', [Validators.required, Validators.email]],
        password: [
          '',
          [Validators.required, Validators.minLength(8), this.passwordStrengthValidator],
        ],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  ngOnInit(): void {
    // Prefer a fresh query param (came from audit result CTA).
    // Fall back to sessionStorage (user navigated back from OTP page).
    const queryScanId = this.route.snapshot.queryParamMap.get('scanId');

    if (queryScanId) {
      sessionStorage.setItem(SCAN_ID_KEY, queryScanId);
      this.initialPageScanId = Number(queryScanId);
    } else {
      const storedScanId = sessionStorage.getItem(SCAN_ID_KEY);
      this.initialPageScanId = storedScanId ? Number(storedScanId) : null;
    }
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    this.error = '';

    const payload = {
      ...this.form.value,
      initialPageScanID: this.initialPageScanId,
    };

    this.authService.register(payload).subscribe({
      next: () => {
        // Persist email so the OTP page survives refresh/back navigation too
        sessionStorage.setItem(EMAIL_KEY, this.form.value.email);

        this.router.navigate(['/verify-otp'], {
          state: { email: this.form.value.email },
        });
      },
      error: (err) => {
        this.loading = false;

        const apiError = err.error;
        let message = 'Registration failed. Please try again.';

        if (apiError?.message) {
          message = apiError.message;
        }

        if (message.toLowerCase().includes('email')) {
          this.form.get('email')?.setErrors({ serverError: message });
        }

        if (message.toLowerCase().includes('password')) {
          this.form.get('password')?.setErrors({ serverError: message });
        }

        this.error = message;
      },
    });
  }

  // ── Validators ────────────────────────────────────────────────────────────

  private passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const v = control.value as string;
    if (!v) return null;
    if (!/[A-Z]/.test(v)) return { noUppercase: true };
    if (!/[0-9]/.test(v)) return { noNumber: true };
    if (!/[^a-zA-Z0-9]/.test(v)) return { noSpecial: true };
    return null;
  }

  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const pw = group.get('password')?.value;
    const cpw = group.get('confirmPassword')?.value;
    return pw === cpw ? null : { passwordMismatch: true };
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  get f() {
    return this.form.controls;
  }
}
