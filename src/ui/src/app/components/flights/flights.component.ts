import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, combineLatest, distinctUntilChanged, filter, map, Observable, of, ReplaySubject, share, shareReplay, startWith, Subject, switchMap, take, tap, withLatestFrom } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as FileSaver from 'file-saver';
import { MatDialog } from '@angular/material/dialog';
import { UnitUtils } from 'src/app/unit-utils';

import { AddFlightModalComponent } from '../add-flight-modal/add-flight-modal.component';
import * as moment from 'moment';
import { SettingsService } from 'src/app/services/settings.service';
import { Scopes, User } from 'src/app/models/user.model';
import { AuthService } from 'src/app/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { DisplayMode } from 'src/app/models/display-mode';
import { TitleService } from 'src/app/services/title.service';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { AssignPilotModalComponent } from '../assign-pilot-modal/assign-pilot-modal.component';
import { SnackbarService } from 'src/app/services/snackbar.service';

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

  /** An array of all flights in the current week. This prevents making an API call when changing dates with in the same week. */
  private allFlights$: Observable<Flight[]>;
  public date$: Observable<moment.Moment>;
  public flights$: Observable<Flight[]>;
  public weekDays$: Observable<WeekDay[]>;
  public isLoading$ = new BehaviorSubject<boolean>(true);
  public user$: Observable<User | null>;

  public canAssignPilots$: Observable<boolean>;
  public canManageFlights$: Observable<boolean>;


  private refreshFlights$ = new Subject();
  public sortDirection$ = new ReplaySubject<'asc' | 'desc'>(1);

  private allColumns = ['time', 'glider', 'releaseHeight', 'duration', 'pilots', 'towplane', 'actions'];
  public columns$: Observable<string[]>;

  public displayMode$ = new ReplaySubject<DisplayMode>(1);

  readonly DisplayMode = DisplayMode;

  constructor(
    private flightService: FlightService,
    private route: ActivatedRoute,
    private dialog: MatDialog,
    private router: Router,
    private settings: SettingsService,
    private auth: AuthService,
    private admiralSnackbar: SnackbarService,
    private title: TitleService
  ) {

    this.user$ = this.auth.user$;

    this.sortDirection$.next(
      this.settings.flightSortOrder
    );

    this.displayMode$.next(
      this.settings.displayMode
    );

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

    this.columns$ = this.auth.isAuthenticated$.pipe(
      map(isAuthenticated => isAuthenticated ?
        this.allColumns
        : this.allColumns.filter(x => x != "pilots")
      )
    );

    const includes$ = this.auth.isAuthenticated$.pipe(
      map(isAuthenticated => isAuthenticated ?
          `${FlightService.INCLUDE_STATISTICS},${FlightService.INCLUDE_PILOTS}`
        : FlightService.INCLUDE_STATISTICS
      )
    );

    this.allFlights$ = combineLatest([
      week$,
      this.refreshFlights$.pipe(startWith(null)),
      includes$
    ]).pipe(
      tap(_ => {
        this.isLoading$.next(true);
      }),
      switchMap(([date, _, includes]) =>
        this.flightService.getFlights(
          date.toDate(),
          date.clone()
            .endOf('isoWeek')
            .toDate(),
          includes)
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
                patternEntryAltitude: null,
                maxDistanceFromField: null
              },
              towFlight: flight,
              waypoints: null,
              occupants: null
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

    this.canAssignPilots$ = this.auth.hasScope(Scopes.AssignPilots);
    this.canManageFlights$ = this.auth.hasScope(Scopes.ManageFlights);
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
    this.date$.pipe(
      map(date => date.isSame(moment(), 'day') ?
        'Flights'
        : `Flights - ${date.format('M/D/YYYY')}`)
    ).subscribe(title =>
      this.title.setTitle(title)
    );
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

  public calculateHobsTime(seconds: number | null) {
    if (seconds == null)
      return null;
    return Math.round(seconds / 60.0 / 60.0 * 10) / 10.0;
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

  debugRow(row: any) {
    console.log(row);
  }

  public onDateChange(event: MatDatepickerInputEvent<Date>) {
    const newDate = event.value
    this.router.navigate(["/flights/dashboard", `${moment(newDate!).format('YYYY-MM-DD')}`])
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

  onTableSortChange(sortState: Sort) {
    if (sortState.direction != '') {
      this.settings.flightSortOrder = sortState.direction;
      this.sortDirection$.next(sortState.direction);
    }
  }

  public mToFt(value: number | undefined | null): number | null {
    return value ?
      Math.round(UnitUtils.mToFt(value)!)
      : null;
  }

  public setDisplayMode(mode: DisplayMode) {
    this.displayMode$.next(mode);
    this.settings.displayMode = mode;
  }

  public addToLogbook(flight: Flight, user: User) {
    this.flightService.addPilot(flight.flightId!, user.userId).subscribe(() => {
      this.admiralSnackbar.open("Flight added to your logbook.", "Close");

      this.refreshFlights$.next(null);
    });
  }

  public formatPilot(flight: Flight): string {
    let pilots = "";

    if (flight.occupants) {
      if (flight.occupants.length == 1)
        pilots = flight.occupants[0].name;
      else
        pilots = flight.occupants[0].name + " +" + (flight.occupants.length - 1);
    }

    return pilots;
  }

  public isMyFlight(flight: Flight): Observable<boolean> {
    if (!flight.occupants)
      return of(false);

    return this.user$.pipe(
      map(user => user == null ?
        false
        : flight.occupants!.some(x => x.userId == user.userId))
    );
  }

  public assignPilots(flight: Flight) {
    this.dialog.open(AssignPilotModalComponent, {
      data: {
        flight
      },
      panelClass: "dialog-md"
    })
    .afterClosed()
    .pipe(
      filter((x: boolean) => x),
    )
    .subscribe(() => this.refreshFlights());
  }

  public isUserOnFlight(flight: Flight, user: User) {
    return flight.occupants != null && user != null && flight.occupants.some(x => x.userId = user.userId);
  }

  public uploadIgcFile(event: Event) {
    const element = event.currentTarget as HTMLInputElement;
    this.flightService.uploadFlight(
      element.files![0],
      "92A"
    ).subscribe({
      next: (flight) => {
        this.router.navigate(["flights", flight.flightId]);
      },
      error: () => {
        this.admiralSnackbar.openError("Error adding flight. Please try again.");
        // Reset the input so the user can upload the same file again
        element.value = "";
      }
    })
  }
}
