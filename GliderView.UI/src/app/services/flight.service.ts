import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
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
}
