import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { combineLatest, filter, iif, map, merge, Observable, of, shareReplay, startWith, Subject, switchMap, take, throwError, withLatestFrom } from 'rxjs';
import { Flight, FlightEventType, Occupant, Waypoint } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as leaflet from 'leaflet';
import { ChartData, ChartOptions } from 'chart.js';
import * as moment from 'moment';
import { LineAnnotationOptions } from 'chartjs-plugin-annotation';
import { UnitUtils } from 'src/app/unit-utils';
import { AuthService } from 'src/app/services/auth.service';
import { TitleService } from 'src/app/services/title.service';
import { Scopes, User } from 'src/app/models/user.model';
import { MatDialog } from '@angular/material/dialog';
import { AssignPilotModalComponent } from '../assign-pilot-modal/assign-pilot-modal.component';

const baseChartOptions: ChartOptions<'line'> = {
  plugins: {
    legend: {
      display: true
    },
    title: {
      display: true,
      text: 'Altitude'
    },
    tooltip: {
      callbacks: {
        // label: (item) => {
        //   if (item.label)
        //     return item.label + ": " + item.parsed;
        //   else
        //     return moment(item.parsed).format('T+HH:mm:ss');
        // }
      }
    }
  },
  scales: {
    y: {
      title:{
        display: true,
        text: "Altitude (ft MSL)"
      },
      position: "left"
    },
    y1: {
      title: {
        display: true,
        text: "Vertical Speed (kts)"
      },
      position: "right"
    },
    x: {
      ticks: {
        // callback: function(value, index, ticks) {
        //   // value is the index. Must call getLabelforValue to get the actual value.
        //   const self = <any>this;
        //   return moment(self.getLabelForValue(value)).format('T+HH:mm:ss')
        // }
      }
    }
  }
};

const formatAltitudeLabel = (date: Date, flightStart: Date) =>
  moment(
    moment.duration(
      moment(date).diff(flightStart)
    )
    .as('milliseconds')
  ).format('T+HH:mm:ss');


@Component({
  selector: 'app-flight',
  templateUrl: './flight.component.html',
  styleUrls: ['./flight.component.scss']
})
export class FlightComponent implements OnInit, AfterViewInit {
  public user$: Observable<User>;
  public flight$: Observable<Flight>;
  public chartConfig$: Observable<{ data: ChartData<'line'>, options: ChartOptions<'line'> }>;
  public userOnFlight$: Observable<boolean>;
  public canAssignPilots$: Observable<boolean>;

  private map: leaflet.Map | null = null;
  private altitudeChartData$: Observable<ChartData<'line'>>;
  private altitudeChartOptions$: Observable<ChartOptions<'line'>>;
  private updateStatistics$ = new Subject<void>();
  private refreshFlight$ = new Subject<void>();

  constructor(
    private flightService: FlightService,
    private route: ActivatedRoute,
    private auth: AuthService,
    private title: TitleService,
    private dialog: MatDialog
  ) {

    this.user$ = this.auth.user$.pipe(
      filter(x => x != null),
      map(x => x!)
    );

    const flightId$ = this.route.params.pipe(
      map(params => params["id"])
    );

    const updateStatsRequest$ = this.updateStatistics$.pipe(
      withLatestFrom(flightId$),
      switchMap(([_, id]) => this.flightService.recalculateStatistics(id)),
      startWith(null)
    );

    const includes$ = this.auth.isAuthenticated$.pipe(
      map(isAuthenticated => isAuthenticated ?
          `${FlightService.INCLUDE_STATISTICS},${FlightService.INCLUDE_PILOTS},${FlightService.INCLUDE_WAYPOINTS}`
        :  `${FlightService.INCLUDE_STATISTICS},${FlightService.INCLUDE_WAYPOINTS}`
      )
    );

    this.flight$ = combineLatest([
      flightId$,
      includes$,
      // Used only for triggering a reload
      updateStatsRequest$,
      this.refreshFlight$.pipe(startWith(null))
    ]).pipe(
      switchMap(([flightId, includes]) =>
        this.flightService.getFlight(
          flightId,
          includes
        )
      ),
      shareReplay(1)
    );

    this.userOnFlight$ = combineLatest([
      this.user$,
      this.flight$
    ]).pipe(
      map(([user, flight]) => (flight.occupants && user && flight.occupants.some(x => x.userId == user.userId)) ?? false)
    );

    this.altitudeChartData$ = this.flight$.pipe(
      map(flight => {
        const data: ChartData<'line'> = {
          labels: flight.waypoints!.map(x => formatAltitudeLabel(x.time, flight.startDate)),
          datasets: [{
            label: 'Altitude',
            data: flight.waypoints!.map(x => this.mToFt(x.gpsAltitude)!),
            showLine: true,
            yAxisID: "y"
          }, {
            label: 'Vertical Speed',
            data: this.calculateVerticalSpeed(flight.waypoints!),
            showLine: true,
            yAxisID: "y1"
          }]
        };
        return data;
      })
    );

    this.altitudeChartOptions$ = this.flight$.pipe(
      map(flight => {
        var annotations: LineAnnotationOptions[] = [];
        const patternEntry = flight.waypoints?.find(x => x.flightEvent === FlightEventType.patternEntry) ?? null;
        const releasePoint = flight.waypoints?.find(x => x.flightEvent === FlightEventType.release) ?? null;

        if (patternEntry)
          annotations.push({
            value: formatAltitudeLabel(patternEntry.time, flight.startDate),
            scaleID: 'x',
            label: {
              display: true,
              content: "Pattern Entry"
            }
          });

        if (releasePoint)
          annotations.push({
            value: formatAltitudeLabel(releasePoint.time, flight.startDate),
            scaleID: 'x',
            label: {
              display: true,
              content: "Release"
            }
          });
        
        return {
          ...baseChartOptions,
          plugins: {
            ...baseChartOptions.plugins,
            annotation: {
              annotations: annotations
            }
          }
        }
      })
    );

    this.chartConfig$ = combineLatest([
      this.altitudeChartData$,
      this.altitudeChartOptions$
    ]).pipe(
      map(([data, options]) => ({
        data: data,
        options: options
      }))
    );

    this.canAssignPilots$ = this.auth.hasScope(Scopes.AssignPilots);
  }

  ngOnInit(): void {
    this.flight$.pipe(
      take(1)
    ).subscribe(flight =>
      this.title.setTitle(`${flight.aircraft?.description ?? 'Unknown'} Flight on ${flight.startDate.toDateString()}`)
    );
  }

  public mToFt(value: number | undefined | null): number | null {
    return !value ? null : Math.round(value * 3.281);
  }

  // Kilometers to nautical miles
  public kmToM(value: number | undefined | null): number | null {
    if (value === null || value === undefined)
      return null;
    
    return Math.round(UnitUtils.kmToNm(value)! * 10) / 10;    
  }

  ngAfterViewInit(): void {
    this.flight$.subscribe(flight =>
      this.initMap(flight)
    );
  }

  private initMap(flight: Flight) {
    if (this.map)
      this.map.remove();
      
    // Chilhowee Gliderport coordinates
    let origin: leaflet.LatLngTuple = [35.2264622, -84.5849328];
    if (flight.waypoints?.length ?? 0 > 0)
      origin = [
        flight.waypoints![0].latitude,
        flight.waypoints![0].longitude
      ];

    this.map = leaflet.map('map', {
      center: origin,
      zoom: 13
    });

    const tiles = leaflet.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      minZoom: 8,
      attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    });

    tiles.addTo(this.map);

    // Hack to get the map to detect the screen size and render properly
    window.setTimeout(() => this.map?.invalidateSize(), 10);

    leaflet.polyline(
      flight.waypoints!.map(x => ([x.latitude, x.longitude])),
      {}
    ).addTo(this.map);

    const patternEntry = flight.waypoints?.find(x => x.flightEvent === FlightEventType.patternEntry) ?? null;
    const releasePoint = flight.waypoints?.find(x => x.flightEvent === FlightEventType.release) ?? null;

    if (patternEntry)
      leaflet.marker([patternEntry.latitude, patternEntry.longitude])
        .bindPopup("Pattern Entry: " + this.mToFt(patternEntry.gpsAltitude) + "ft MSL")
        .addTo(this.map);

    if (releasePoint)
      leaflet.marker([releasePoint.latitude, releasePoint.longitude])
        .bindPopup("Release: " + this.mToFt(releasePoint.gpsAltitude) + "ft MSL")
        .addTo(this.map);
  }

  public recalculateStatistics() {
    this.updateStatistics$.next();
  }

  private calculateVerticalSpeed(waypoints: Waypoint[]): number[] {
    // Add empty first value to account for the offset
    const data = [0];
    for (let i = 1; i < waypoints.length; i++) {
      // kts
      const speed = (waypoints[i].gpsAltitude - waypoints[i - 1].gpsAltitude) / (waypoints[i].time.getTime() - waypoints[i - 1].time.getTime()) * 1000.0 * 1.94384;
      data.push(speed);
    }
    return data;
  }

  public isUserOnFlight(flight: Flight, user: User) {
    return flight.occupants != null && user != null && flight.occupants.some(x => x.userId = user.userId);
  }

  public addToLogbook(flight: Flight, user: User) {

    this.flightService.addPilot(flight.flightId!, user.userId)
      .subscribe({
        next: () => {
          this.refreshFlight$.next();
        },
        error: () => {
          alert("Error adding to logbook.");
        }
      });
  }

  public removePilot(flightId: string, userId: string) {
    this.flightService.removePilot(flightId, userId!)
      .subscribe(() => this.refreshFlight$.next());
  }

  public assignPilots(flight: Flight) {
    this.dialog.open(
      AssignPilotModalComponent,
      {
        data: {
          flight
        },
        panelClass: 'dialog-md'
      }
    ).afterClosed()
    .pipe(
      filter(x => x)
    ).subscribe(() => this.refreshFlight$.next());
  }

}
