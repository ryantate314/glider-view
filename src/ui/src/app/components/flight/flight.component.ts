import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map, Observable, shareReplay, switchMap } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as leaflet from 'leaflet';
import { ChartData, ChartOptions } from 'chart.js';
import * as moment from 'moment';

@Component({
  selector: 'app-flight',
  templateUrl: './flight.component.html',
  styleUrls: ['./flight.component.scss']
})
export class FlightComponent implements OnInit, AfterViewInit {

  public flight$: Observable<Flight>;
  public altitudeChartData$: Observable<ChartData>;
  public altitudeChartOptions: ChartOptions = {
    plugins: {
      legend: {
        display: false
      },
      title: {
        display: true,
        text: 'Altitude'
      }
    },
    scales: {
      y: {
        title:{
          display: true,
          text: "Altitude (ft MSL)"
        }
      }
    }
  };

  private map: leaflet.Map | null = null;

  constructor(private flightService: FlightService, private route: ActivatedRoute) {
    this.flight$ = this.route.params.pipe(
      map(params => params["id"]),
      switchMap(id => this.flightService.getFlight(id)),
      shareReplay(1)
    );

    this.altitudeChartData$ = this.flight$.pipe(
      map(flight => {
        const data: ChartData<'line'> = {
          labels: flight.waypoints!.map(x => moment.utc(
            moment.duration(
              moment(x.time).diff(flight.startDate)
            )
            .as('milliseconds')
          ).format('T+HH:mm:ss')),
          datasets: [{
            label: 'Altitude',
            data: flight.waypoints!.map(x => this.mToFt(x.gpsAltitude)),
            showLine: true
          }]
        };
        return data;
      })
    );
  }

  ngOnInit(): void {
  }

  public mToFt(value: number | undefined | null): number | null {
    return !value ? null : value * 3.281;
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
      {
        
      }
    ).addTo(this.map);
  }

}
