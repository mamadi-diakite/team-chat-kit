import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@team-chat-kit/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { RegisterPage } from './auth/register-page';
import { HomePage } from './home/home-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Team Chat Demo' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · Team Chat Demo',
  },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [guestGuard],
    title: 'Create an account · Team Chat Demo',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · Team Chat Demo',
  },
  { path: '**', redirectTo: '' },
];
