import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map, Observable, switchMap, tap } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';

import * as moment from 'moment';

@Component({
  selector: 'app-flights',
  templateUrl: './flights.component.html',
  styleUrls: ['./flights.component.scss']
})
export class FlightsComponent implements OnInit {

  public flights$: Observable<Flight[]>;
  public isLoading: boolean = true;

  constructor(private flightService: FlightService, private route: ActivatedRoute) {

    this.flights$ = this.route.queryParams.pipe(
      map(params => {
        let startDate = moment().startOf('day');
        if (params["startDate"])
          startDate = moment(params["startDate"]);
        let endDate = moment().endOf('day');
        if (params["endDate"])
          endDate = moment(params["endDate"]);
        return {
          startDate,
          endDate
        }
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

}
