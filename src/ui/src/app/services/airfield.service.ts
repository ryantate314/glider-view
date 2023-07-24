import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Airfield } from '../models/airfield.model';
import { environment } from 'src/environments/environment';
import { Observable, catchError, of, shareReplay, tap, throwError } from 'rxjs';

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
}
