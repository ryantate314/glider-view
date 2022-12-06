import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, combineLatest, distinctUntilChanged, map, Observable, of, ReplaySubject, share, shareReplay, startWith, Subject, switchMap, tap, withLatestFrom } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as FileSaver from 'file-saver';
import { MatDialog } from '@angular/material/dialog';

import { AddFlightModalComponent } from '../add-flight-modal/add-flight-modal.component';
import * as moment from 'moment';
import { SettingsService } from 'src/app/services/settings.service';
import { User } from 'src/app/models/user.model';
import { AuthService } from 'src/app/services/auth.service';

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
  public user$: Observable<User | null>;

  private refreshFlights$ = new Subject();
  public sortDirection$ = new ReplaySubject<'asc' | 'desc'>(1);

  constructor(
    private flightService: FlightService,
    private route: ActivatedRoute,
    private dialog: MatDialog,
    private router: Router,
    private settings: SettingsService,
    private auth: AuthService
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
      map(date => date.clone().startOf('isoWeek')),
      distinctUntilChanged((prev, curr) => prev.isSame(curr, 'day')),
      // Without share, each subscription gets their own observable pipeline
      shareReplay(1)
    );

    this.allFlights$ = combineLatest([
      week$,
      this.refreshFlights$.pipe(startWith(null))
    ]).pipe(
      tap(_ => {
        this.isLoading$.next(true);
      }),
      switchMap(([date, _]) =>
        this.flightService.getFlights(
          date.toDate(),
          date.clone()
            .endOf('isoWeek')
            .toDate())
      ),
      tap(_ => {
        this.isLoading$.next(false);
      }),
      shareReplay(1)
    );

    const flightsOnDate$ = this.flights$ = combineLatest([
      this.allFlights$,
      this.date$
    ]).pipe(
      map(([flights, date]) => flights.filter(x =>
        moment(date).isSame(x.startDate, 'day')))
    );

    this.flights$ = combineLatest([
      flightsOnDate$,
      this.sortDirection$
    ]).pipe(
      // Sort flights
      map(([flights, sortDirection]) =>
        flights.sort((a, b) => sortDirection === 'asc'
          ? a.startDate.getTime() - b.startDate.getTime()
          : b.startDate.getTime() - a.startDate.getTime())
      ),
      map(flights => {
        // Dictionary of tow flights mapped to their corresponding gliders
        const towFlights = flights.filter(x => x.towFlight !== null)
          .reduce((dict, flight) => ({
            ...dict,
            [flight.towFlight!.flightId!]: flight
          }), {});

        // The API returns a flat list of glider flights and tows. Convert them into a list of glider flights with
        // linked tow flights.
        return flights.filter(flight => !(flight.flightId! in towFlights))
          .map<Flight>(flight => flight.aircraft?.isGlider
            ? flight
            : {
              flightId: null,
              aircraft: null,
              duration: null,
              startDate: flight.startDate,
              endDate: null,
              igcFileName: null,
              statistics: {
                releaseHeight: flight.statistics?.maxAltitude ?? null,
                altitudeGained: null,
                distanceTraveled: null,
                maxAltitude: null,
                patternEntryAltitude: null
              },
              towFlight: flight,
              waypoints: null
            }
          );
      })
    );

    this.weekDays$ = combineLatest([
      this.allFlights$,
      this.date$
    ]).pipe(
      map(([flights, date]) => this.groupFlightsIntoDays(date, flights))
    );

    this.sortDirection$.next(
      this.settings.flightSortOrder
    );

    this.user$ = this.auth.user$;
  }

  private groupFlightsIntoDays(date: moment.Moment, flights: Flight[]): WeekDay[] {

    const days = [];
    const dateIterator = date.clone().startOf('isoWeek');

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

  refreshFlights() {
    this.refreshFlights$.next(null);
  }

  public downloadIgc(flight: Flight) {
    this.flightService.downloadIgcFile(flight.flightId!).subscribe({
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
          .startOf('isoWeek')
          .subtract(1, 'day'))
    );
  }


  public navigateWeekForward() {
    of(true).pipe(
      withLatestFrom(this.date$)
    ).subscribe(([_, date]) =>
      this.navigateToDate(
        date.clone()
          .endOf('isoWeek')
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

  public sortDescending() {
    this.settings.flightSortOrder = 'desc';
    this.sortDirection$.next('desc');
  }

  public sortAscending() {
    this.settings.flightSortOrder = 'asc';
    this.sortDirection$.next('asc');
  }

  public mToFt(value: number | undefined | null): number | null {
    return !value
      ? null
      : Math.round(value! * 3.281);
  }

  public addToLogbook(user: User) {

  }
}
