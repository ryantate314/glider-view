import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FlightComponent } from './components/flight/flight.component';
import { FlightsComponent } from './components/flights/flights.component';
import { LeaderboardComponent } from './components/leaderboard/leaderboard.component';
import { LogbookComponent } from './components/logbook/logbook.component';
import { PilotsComponent } from './components/pilots/pilots.component';
import { ProfileComponent } from './components/profile/profile.component';
import { WelcomeComponent } from './components/welcome/welcome.component';
import { AuthGuard } from './guards/auth.guard';
import { Scopes } from './models/user.model';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'flights/dashboard' },
  { path: 'flights/dashboard', component: FlightsComponent },
  { path: 'flights/dashboard/:date', component: FlightsComponent },
  { path: 'flights/:id', component: FlightComponent },
  { path: 'user/profile', component: ProfileComponent, canActivate: [AuthGuard] },
  { path: 'user/logbook', component: LogbookComponent, canActivate: [AuthGuard] },
  { path: 'welcome/:invitationToken', component: WelcomeComponent },
  {
    path: 'pilots',
    component: PilotsComponent,
    canActivate: [AuthGuard],
    data: {
      scopes: [Scopes.ViewAllUsers]
    }
  },
  { path: 'leaderboard', component: LeaderboardComponent, canActivate: [AuthGuard] },
  { path: 'leaderboard/:date', component: LeaderboardComponent, canActivate: [AuthGuard] },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
