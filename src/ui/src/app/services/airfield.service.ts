import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Airfield } from '../models/airfield.model';
import { environment } from 'src/environments/environment';
import { Observable, catchError, map, of, shareReplay, tap, throwError } from 'rxjs';
import { AircraftLocationUpdate } from '../models/aircraftLocationUpdate.model';

@Injectable({
  providedIn: 'root'
})
export class AirfieldService {

  private readonly cache: { [key: string]: Observable<Airfield | null> } = {};

  constructor(private readonly http: HttpClient) { }

  public getField(faaId: string): Observable<Airfield | null> {
    if (faaId in this.cache)
      return this.cache[faaId];
    
    this.cache[faaId] = this.http.get<Airfield>(
      `${environment.apiUrl}/airfields?faaId=${encodeURI(faaId)}`
    ).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 404)
          return of(null);
        return throwError(() => err);
      }),
      shareReplay(1)
    );

    return this.cache[faaId];
  }

  public getFleet(faaId: string): Observable<AircraftLocationUpdate[]> {
    return this.http.get<AircraftLocationUpdate[]>(
      `${environment.apiUrl}/airfields/${encodeURI(faaId)}/fleet`
    ).pipe(
      map(aircraft => aircraft.map(x => ({
        ...x,
        lastCheckin: new Date(x.lastCheckin)
      })))
    );
  }
}
