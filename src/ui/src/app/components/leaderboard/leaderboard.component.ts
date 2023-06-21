import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest, distinctUntilChanged, map, Observable, shareReplay, switchMap, tap, withLatestFrom } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as dayjs from 'dayjs';
import { Leaderboard } from 'src/app/models/leaderboard.model';
import { LeaderboardService } from 'src/app/services/leaderboard.service';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { UnitUtils } from 'src/app/unit-utils';
import { TitleService } from 'src/app/services/title.service';

@Component({
  selector: 'app-leaderboard',
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.scss']
})
export class LeaderboardComponent implements OnInit {

  leaderboard$: Observable<Leaderboard>;
  date$: Observable<Date>;

  constructor(
    private readonly flightService: FlightService,
    private readonly leaderboardService: LeaderboardService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly title: TitleService
  ) {
    this.date$ = this.route.paramMap.pipe(
      map(params => params.get('date') ?
      // use dayjs to avoid daylight savings time errors
        dayjs(params.get('date')!).toDate()
        : dayjs().startOf('day').toDate()
      ),
      distinctUntilChanged()
    );

    this.leaderboard$ = this.date$.pipe(
      switchMap(date => this.leaderboardService.getLeaderboard(date)),
      shareReplay(1)
    );
  }

  ngOnInit(): void {
    this.title.setTitle("Leaderboard");
  }

  public onDateChange(event: MatDatepickerInputEvent<Date>) {
    const newDate = event.value
    console.log("New date: ", newDate);
    this.router.navigate(["leaderboard", `${dayjs(newDate!).format('YYYY-MM-DD')}`])
  }

  public formatDistance(meters: number | null) {
    return meters ?
      Math.round(UnitUtils.kmToNm(meters)! * 10) / 10.0 + "nm"
      : null;
  }

  public onFlightClick(flight: Flight) {
    this.router.navigate(['flights', flight.flightId]);
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

  public formatAircraft(flight: Flight): string {
    let description = flight.aircraft?.description ?? "Unknown";

    if (flight.contestId) {
      description = description + " (" + flight.contestId + ")";
    }

    return description;
  }

}
