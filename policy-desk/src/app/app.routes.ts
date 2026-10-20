import { Routes } from '@angular/router';
import { Admin } from './admin/admin';
import { adminGuard } from './admin.guard';
import { agentGuard } from './agent.guard';
import { Dashboard } from './dashboard/dashboard';
import { Home } from './home/home';
import { Inbox } from './inbox/inbox';
import { Login } from './login/login';
import { QuoteRequest } from './quote-request/quote-request';
import { Quotes } from './quotes/quotes';
import { underwriterGuard } from './underwriter.guard';
import { Underwriting } from './underwriting/underwriting';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'quote/:code', component: QuoteRequest },
  { path: 'login', component: Login },
  { path: 'admin', component: Admin, canActivate: [adminGuard] },
  { path: 'dashboard', component: Dashboard, canActivate: [agentGuard] },
  { path: 'inbox', component: Inbox, canActivate: [agentGuard] },
  { path: 'quotes', component: Quotes, canActivate: [agentGuard] },
  { path: 'underwriting', component: Underwriting, canActivate: [underwriterGuard] },
];