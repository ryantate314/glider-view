import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Flight } from '../models/flight.model';

@Injectable({
  providedIn: 'root'
})
export class FlightService {

  constructor(private http: HttpClient) { }

  public getFlights(startDate: Date, endDate: Date): Observable<Flight[]> {
    return this.http.get<Flight[]>(
      `${environment.apiUrl}/flights`,
      {
        params: {
          startDate: startDate.toISOString(),
          endDate: endDate.toISOString()
        }
      }
    )
  }
}
