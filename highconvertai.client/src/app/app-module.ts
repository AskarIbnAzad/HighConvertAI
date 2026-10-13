import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { providePrimeNG } from 'primeng/config';
import { SelectModule } from 'primeng/select';

import Aura from '@primeuix/themes/aura';
import { HomeComponent } from './pages/home/home';
import { AuditResultComponent } from './pages/audit-result/audit-result';
import { ScanLoadingModalComponent } from './components/scan-loading-modal/scan-loading-modal';
import { SiteHeaderComponent } from './components/site-header/site-header';
import { SiteFooterComponent } from './components/site-footer/site-footer';

import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { LoginComponent } from './pages/auth/login/login';

import { RegisterComponent } from './pages/auth/register/register.component';

import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard';

import { UserDashboardComponent } from './pages/user-dashboard/user-dashboard';
import { LoginModalComponent } from './components/login-modal/login-modal';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { VerifyOtpComponent } from './pages/auth/verify-otp/verify-otp.component';

import { TokenInterceptor } from '../app/core/token.interceptor';
import { UserComponent } from './pages/user/user';
import { AuditComponent } from './pages/audit/audit';
import { UserAuditComponent } from './pages/user-audit/user-audit';
import { ValidationCardComponent } from './components/validation-card/validation-card';
import { DatePickerModule } from 'primeng/datepicker';

@NgModule({
  declarations: [
    App,
    HomeComponent,
    AuditResultComponent,
    ScanLoadingModalComponent,
    SiteHeaderComponent,
    SiteFooterComponent,
    LoginComponent,
    RegisterComponent,
    VerifyOtpComponent,
    AdminDashboardComponent,
    UserDashboardComponent,
    LoginModalComponent,
    UserComponent,
    AuditComponent,
    UserAuditComponent,
    ValidationCardComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,

    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    AppRoutingModule,

    TableModule,
    ButtonModule,
    InputTextModule,
    TagModule,
    IconFieldModule,
    InputIconModule,
    SelectModule,
    DatePickerModule,
  ],
  providers: [
    providePrimeNG({
      theme: {
        preset: Aura,
      },
    }),

    {
      provide: HTTP_INTERCEPTORS,
      useClass: TokenInterceptor,
      multi: true,
    },
  ],
  bootstrap: [App],
})
export class AppModule {}
