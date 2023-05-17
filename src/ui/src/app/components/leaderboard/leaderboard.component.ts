import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest, map, Observable, shareReplay, switchMap, withLatestFrom } from 'rxjs';
import { Flight } from 'src/app/models/flight.model';
import { FlightService } from 'src/app/services/flight.service';
import * as moment from 'moment';
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
        new Date(params.get('date')!)
        : new Date()
      )
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
    this.router.navigate(["leaderboard", `${newDate!.getFullYear()}-${newDate!.getMonth() + 1}-${newDate!.getDate()}`])
  }

  public formatDistance(meters: number | null) {
    return meters ?
      Math.round(UnitUtils.kmToNm(meters)! * 10) / 10.0 + "nm"
      : null;
  }

  public formatDuration(seconds: number | null) {
    if (seconds === null)
      return null;

    const duration = moment.duration(seconds, 'second');
    if (seconds > 60 * 60)
      return moment.utc(duration.as('milliseconds'))
        .format('H[h]m[m]s[s]')
    else if (seconds > 60)
      return moment.utc(duration.as('milliseconds'))
        .format('m[m]s[s]')
    else
      return seconds + "s";
  }

  public onFlightClick(flight: Flight) {
    this.router.navigate(['flights', flight.flightId]);
  }

}
