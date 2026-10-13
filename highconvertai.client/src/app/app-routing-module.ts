import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home';
import { AuditResultComponent } from './pages/audit-result/audit-result';
import { LoginComponent } from './pages/auth/login/login';
import { RegisterComponent } from './pages/auth/register/register.component';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard';
import { UserDashboardComponent } from './pages/user-dashboard/user-dashboard';
import { VerifyOtpComponent } from './pages/auth/verify-otp/verify-otp.component';
import { AuthGuard } from './core/auth.guard';
import { RoleGuard } from './core/role.guard';
import { UserComponent } from './pages/user/user';
import { AuditComponent } from './pages/audit/audit';
import { UserAuditComponent } from './pages/user-audit/user-audit';

const routes: Routes = [
  {
    path: '',
    component: HomeComponent,
  },

  {
    path: 'result/:id',
    component: AuditResultComponent,
  },

  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: 'register',
    component: RegisterComponent,
  },

  {
    path: 'verify-otp',
    component: VerifyOtpComponent,
  },

  {
    path: 'admin-dashboard',
    component: AdminDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: {
      role: 'Admin',
    },
  },

  {
    path: 'user-dashboard',
    component: UserDashboardComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'user',
    component: UserComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'audit',
    component: AuditComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'user-audit',
    component: UserAuditComponent,
    canActivate: [AuthGuard],
  },
  {
    path: '**',
    redirectTo: '',
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
