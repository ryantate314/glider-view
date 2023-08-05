import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http'
import { BrowserModule } from '@angular/platform-browser';

import * as dayjs from 'dayjs';
import * as duration from 'dayjs/plugin/duration';
import * as isoWeek from 'dayjs/plugin/isoWeek';
import * as utc from 'dayjs/plugin/utc';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { FlightsComponent } from './components/flights/flights.component';
import { AddFlightModalComponent } from './components/add-flight-modal/add-flight-modal.component';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { MatCardModule } from '@angular/material/card';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog'
import { MatDividerModule } from '@angular/material/divider';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import {MatAutocompleteModule} from '@angular/material/autocomplete';

import { FlightComponent } from './components/flight/flight.component';
import { NgChartsModule } from 'ng2-charts';
import { Chart } from 'chart.js';
import Annotation from 'chartjs-plugin-annotation';
import { icon, Marker } from 'leaflet';
import { LoginComponent } from './components/login/login.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ProfileComponent } from './components/profile/profile.component';
import { ChangePasswordModalComponent } from './components/change-password-modal/change-password-modal.component';
import { AUTH_INTERCEPTOR } from './interceptors/auth.interceptor';
import { PilotsComponent } from './components/pilots/pilots.component';
import { AddUserModalComponent } from './components/add-user-modal/add-user-modal.component';
import { LogbookComponent } from './components/logbook/logbook.component';
import { WelcomeComponent } from './components/welcome/welcome.component';
import { QRCodeModule } from 'angularx-qrcode';
import { LeaderboardComponent } from './components/leaderboard/leaderboard.component';
import { FlightDurationPipe } from './pipes/flight-duration.pipe';
import { FlightDurationDirective } from './directives/flight-duration.directive';
import { AssignPilotModalComponent } from './components/assign-pilot-modal/assign-pilot-modal.component';
import { UNAUTHORIZED_INTERCEPTOR } from './interceptors/unauthorized.interceptor';
import { MenuBarComponent } from './components/menu-bar/menu-bar.component';
import { ErrorComponent } from './components/error/error.component';
import { PasswordModalComponent } from './components/password-modal/password-modal.component';

Chart.register(Annotation);

dayjs.extend(duration);
dayjs.extend(isoWeek);
dayjs.extend(utc);

const iconRetinaUrl = "assets/leaflet/marker-icon-2x.png";
const iconUrl = "assets/leaflet/marker-icon.png";
const shadowUrl =  "assets/leaflet/marker-shadow.png";
const iconDefault = icon({
  iconRetinaUrl,
  iconUrl,
  shadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  tooltipAnchor: [16, -28],
  shadowSize: [41, 41]
});
Marker.prototype.options.icon = iconDefault;

@NgModule({
  declarations: [
    AppComponent,
    FlightsComponent,
    AddFlightModalComponent,
    FlightComponent,
    LoginComponent,
    ProfileComponent,
    ChangePasswordModalComponent,
    PilotsComponent,
    AddUserModalComponent,
    LogbookComponent,
    WelcomeComponent,
    LeaderboardComponent,
    FlightDurationPipe,
    FlightDurationDirective,
    AssignPilotModalComponent,
    MenuBarComponent,
    ErrorComponent,
    PasswordModalComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    NgbModule,
    BrowserAnimationsModule,
    ReactiveFormsModule,
    QRCodeModule,

    MatCardModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatDividerModule,
    MatGridListModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
    MatSortModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatAutocompleteModule,

    NgChartsModule,
  ],
  providers: [
    // Unauthorized interceptor must go first because we need to attach a new auth token after
    // the old one is refreshed.
    UNAUTHORIZED_INTERCEPTOR,
    AUTH_INTERCEPTOR
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
