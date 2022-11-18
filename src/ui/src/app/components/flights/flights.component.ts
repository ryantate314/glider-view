import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map, Observable, switchMap, tap } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as FileSaver from 'file-saver';
import { MatDialog } from '@angular/material/dialog';

import * as moment from 'moment';
import { AddFlightModalComponent } from '../add-flight-modal/add-flight-modal.component';

interface WeekDay {
  abbreviation: string;
  date: string;
  numFlights: number;
}

@Component({
  selector: 'app-flights',
  templateUrl: './flights.component.html',
  styleUrls: ['./flights.component.scss']
})
export class FlightsComponent implements OnInit {

  public flights$: Observable<Flight[]>;
  public weekDays$: Observable<WeekDay[]>;
  public isLoading: boolean = true;

  constructor(
    private flightService: FlightService,
    private route: ActivatedRoute,
    private dialog: MatDialog
  ) {

    this.flights$ = this.route.params.pipe(
      map(params => {
        let date = moment().startOf('day');
        if (params["date"])
          date = moment(params["date"]);
        return date;
      }),
      tap(_ => {
        this.isLoading = true;
      }),
      switchMap(params =>
        this.flightService.getFlights(params.startDate.toDate(), params.endDate.toDate())
      ),
      tap(_ => {
        this.isLoading = false;
      })
    );



  }

  ngOnInit(): void {
    
  }

  public downloadIgc(flight: Flight) {
    this.flightService.downloadIgcFile(flight.flightId).subscribe({
      next: (file) => {
        FileSaver.saveAs(file);
      },
      error: (error) => {
        console.error(error);
      }
    });
  }

  public uploadIgc(flight: Flight) {
    
  }

  public formatDuration(seconds: number): string {
    return moment.duration(seconds, 'second')
      .humanize({ m: 60 });
  }

  public addFlight() {
    var dialog = this.dialog.open(AddFlightModalComponent, {

    });
  }

}
