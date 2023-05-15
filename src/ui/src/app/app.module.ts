import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http'
import { BrowserModule } from '@angular/platform-browser';

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

Chart.register(Annotation);

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
    WelcomeComponent
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

    NgChartsModule,
  ],
  providers: [
    AUTH_INTERCEPTOR
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
