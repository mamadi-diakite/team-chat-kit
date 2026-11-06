import { Routes } from '@angular/router';
import { ChatContacts, ChatDirectory, ChatInvitations } from '@team-chat-kit/chat';
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
  {
    path: 'directory',
    component: ChatDirectory,
    canActivate: [authGuard],
    title: 'Directory · Team Chat Demo',
  },
  {
    path: 'invitations',
    component: ChatInvitations,
    canActivate: [authGuard],
    title: 'Invitations · Team Chat Demo',
  },
  {
    path: 'contacts',
    component: ChatContacts,
    canActivate: [authGuard],
    title: 'Contacts · Team Chat Demo',
  },
  { path: '**', redirectTo: '' },
];
