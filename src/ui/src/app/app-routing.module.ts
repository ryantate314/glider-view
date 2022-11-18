import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FlightsComponent } from './components/flights/flights.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'flights/dashboard' },
  { path: 'flights/dashboard', component: FlightsComponent },
  { path: 'flights/dashboard/:date', component: FlightsComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
