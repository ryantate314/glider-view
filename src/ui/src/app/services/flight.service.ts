import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Flight, Occupant } from '../models/flight.model';

@Injectable({
  providedIn: 'root'
})
export class FlightService {
  

  public static readonly INCLUDE_WAYPOINTS = "waypoints";
  public static readonly INCLUDE_STATISTICS = "statistics";
  public static readonly INCLUDE_PILOTS = "occupants";

  constructor(private http: HttpClient) { }

  public getFlights(startDate: Date, endDate: Date, includes: string = ""): Observable<Flight[]> {
    return this.http.get<Flight[]>(
      `${environment.apiUrl}/flights`,
      {
        params: {
          startDate: startDate.toISOString(),
          endDate: endDate.toISOString(),
          includes: includes
        }
      }
    ).pipe(
      map(flights => flights.map(FlightService.parseFlight))
    )
  }

  private getFileNameFromContentDisposition(contentDisposition: string | null): string | null {
    if (!contentDisposition)
      return null;

    let fileName = contentDisposition.split(";")
      .map(x => x.trim())
      .filter(x => x.startsWith("filename="));

    if (fileName.length == 0)
      return null;

    return fileName[0].split("=")[1];
  }

  public downloadIgcFile(flightId: string): Observable<File> {
    return this.http.get(
      `${environment.apiUrl}/flights/${flightId}/igc`,
      {
        observe: "response",
        responseType: "blob"
      }
    ).pipe(
      map((response: HttpResponse<any>) => {
        
        const fileName = this.getFileNameFromContentDisposition(response.headers.get('content-disposition'))
          || "flight.igc";

        let file = new File([response.body], fileName, { type: "text/plain" });

        return file;
      })
    );
  }

  public getFlight(id: string, includes: string = ""): Observable<Flight> {
    return this.http.get<Flight>(
      `${environment.apiUrl}/flights/${id}`,
      {
        params: {
          includes: includes
        }
      }
    ).pipe(
      map(FlightService.parseFlight)
    );
  }

  public recalculateStatistics(flightId: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/flights/${flightId}/recalculate-statistics`,
      null
    );
  }

  public reloadIgcFile(flightId: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/flights/${flightId}/reprocess`,
      null
    );
  }

  public addPilot(flightId: string, userId: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/flights/${flightId}/pilots`,
      null,
      {
        params: {
          'pilotId': userId
        }
      }
    );
  }

  public removePilot(flightId: string, userId: string): any {
    return this.http.delete(
      `${environment.apiUrl}/flights/${flightId}/pilots/${userId}`
    );
  }

  public updatePilots(flightId: string, occupants: Occupant[]): Observable<void> {
    return this.http.put<void>(
      `${environment.apiUrl}/flights/${flightId}/pilots`,
      occupants
    );
  }

  public static parseFlight(flight: Flight): Flight {
    return {
      ...flight,
      startDate: new Date(flight.startDate + "Z"),
      endDate: new Date(flight.endDate + "Z"),
      waypoints: flight.waypoints == null ? null : flight.waypoints.map(waypoint => ({
        ...waypoint,
        time: new Date(waypoint.time + "Z")
      }))
    };
  }

  public uploadFlight(file: File, airField: string): Observable<Flight> {
    
    const form = new FormData();
    form.append('file', file, file.name);

    return this.http.post<Flight>(
      `${environment.apiUrl}/igcd/upload?airfield=${airField}`,
      form
    )
  }
}
