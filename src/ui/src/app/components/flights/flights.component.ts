import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, combineLatest, distinctUntilChanged, map, Observable, of, share, shareReplay, switchMap, tap, withLatestFrom } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as FileSaver from 'file-saver';
import { MatDialog } from '@angular/material/dialog';

import { AddFlightModalComponent } from '../add-flight-modal/add-flight-modal.component';
import * as moment from 'moment';

interface WeekDay {
  abbreviation: string;
  urlDate: string;
  date: Date;
  numFlights: number;
  isActive: boolean;
}

@Component({
  selector: 'app-flights',
  templateUrl: './flights.component.html',
  styleUrls: ['./flights.component.scss']
})
export class FlightsComponent implements OnInit, AfterViewInit {

  private allFlights$: Observable<Flight[]>;
  public date$: Observable<moment.Moment>;
  public flights$: Observable<Flight[]>;
  public weekDays$: Observable<WeekDay[]>;
  public isLoading$ = new BehaviorSubject<boolean>(true);

  constructor(
    private flightService: FlightService,
    private route: ActivatedRoute,
    private dialog: MatDialog,
    private router: Router
  ) {
    this.date$ = this.route.params.pipe(
      map(params => {
        let date = moment().startOf('day');
        if (params["date"])
          date = moment(params["date"])
            .startOf('day');
        return date;
      })
    );

    const week$ = this.date$.pipe(
      map(date => date.clone().startOf('week')),
      distinctUntilChanged((prev, curr) => prev.isSame(curr, 'day')),
      // Without share, each subscription gets their own observable pipeline
      shareReplay(1)
    );

    this.allFlights$ = week$.pipe(
      tap(_ => {
        this.isLoading$.next(true);
      }),
      switchMap(date =>
        this.flightService.getFlights(
          date.toDate(),
          date.clone()
            .endOf('week')
            .toDate())
      ),
      tap(_ => {
        this.isLoading$.next(false);
      }),
      shareReplay(1)
    );

    this.flights$ = combineLatest([
      this.allFlights$,
      this.date$
    ]).pipe(
      map(([flights, date]) => flights.filter(x =>
        moment(date).isSame(x.startDate, 'day'))),
      // Order by start date ascending
      map(flights => {
        return flights.sort((a, b) => a.startDate.getTime() - b.startDate.getTime())
      })
    );

    this.weekDays$ = combineLatest([
      this.allFlights$,
      this.date$
    ]).pipe(
      map(([flights, date]) => this.groupFlightsIntoDays(date, flights))
    );
  }

  private groupFlightsIntoDays(date: moment.Moment, flights: Flight[]): WeekDay[] {

    const days = [];
    const dateIterator = date.clone().startOf('week');

    for (let i = 0; i < 7; i++) {

      const day: WeekDay = {
        abbreviation: dateIterator.format('ddd'),
        urlDate: dateIterator.format('YYYY-MM-DD'),
        date: dateIterator.toDate(),
        numFlights: flights.filter(x => dateIterator.isSame(x.startDate, 'day'))
          .length,
        isActive: dateIterator.isSame(date, 'day')
      };
      days.push(day);

      dateIterator.add(1, 'days');
    }

    return days;
  }

  ngOnInit(): void {
    
  }

  ngAfterViewInit(): void {

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
    alert("Not implemented");
  }

  public formatDuration(seconds: number): string {
    const duration = moment.duration(seconds, 'second');
    return moment.utc(duration.as('milliseconds'))
      .format('HH:mm:ss');
      
  }

  public addFlight() {
    var dialog = this.dialog.open(AddFlightModalComponent, {

    });
  }

  public navigateWeekBack() {
    of(true).pipe(
      withLatestFrom(this.date$)
    ).subscribe(([_, date]) =>
      this.navigateToDate(
        date.clone()
          .startOf('week')
          .subtract(1, 'day'))
    );
  }


  public navigateWeekForward() {
    of(true).pipe(
      withLatestFrom(this.date$)
    ).subscribe(([_, date]) =>
      this.navigateToDate(
        date.clone()
          .endOf('week')
          .add(1, 'day'))
    );
  }

  public navigateDayForward() {
    of(true).pipe(
      withLatestFrom(this.date$)
    ).subscribe(([_, date]) =>
      this.navigateToDate(
        date.clone()
          .add(1, 'day'))
    );
  }

  public navigateDayBackward() {
    of(true).pipe(
      withLatestFrom(this.date$)
    ).subscribe(([_, date]) =>
      this.navigateToDate(
        date.clone()
          .subtract(1, 'day'))
    );
  }

  private navigateToDate(date: moment.Moment) {
    this.router.navigate([
      '/flights/dashboard',
      date.format('YYYY-MM-DD')
    ]);
  }

  public mToFt(value: number | undefined | null): number | null {
    return !value
      ? null
      : value! * 3.281;
  }
}
