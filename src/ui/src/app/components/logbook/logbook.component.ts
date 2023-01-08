import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { combineLatest, iif, map, Observable, of, startWith, Subject, switchMap, withLatestFrom } from 'rxjs';
import { LogBookEntry } from 'src/app/models/flight.model';
import { AuthService } from 'src/app/services/auth.service';
import { FlightService } from 'src/app/services/flight.service';
import { UserService } from 'src/app/services/user.service';
import { UnitUtils } from 'src/app/unit-utils';

const sortEntries = (a: LogBookEntry, b: LogBookEntry) =>
  (a.flightNumber == null || b.flightNumber == null)
    ? a.flight.startDate.getTime() - b.flight.startDate.getTime()
    : a.flightNumber - b.flightNumber;

@Component({
  selector: 'app-logbook',
  templateUrl: './logbook.component.html',
  styleUrls: ['./logbook.component.scss']
})
export class LogbookComponent implements OnInit {

  public flights$: Observable<LogBookEntry[]>;
  public userId$: Observable<string>;

  private refresh$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private route: ActivatedRoute,
    private auth: AuthService,
    private flightService: FlightService
  ) {

    this.userId$ = this.route.params.pipe(
      switchMap(params => 
        iif(
          () => params["id"] !== undefined,
          of(<string>params["id"]),
          this.auth.user$.pipe(
            map(user => user!.userId)
          )
        )
      )
    );

    this.flights$ = combineLatest([
      this.userId$,
      this.refresh$.pipe(
        startWith(null)
      )
    ]).pipe(
      map(([userId, _]) => userId),
      switchMap(userId => this.userService.getLogbook(userId)),
      map(entries => [...entries].sort(sortEntries))
    );
  }

  ngOnInit(): void {
  }

  getHobsTime(duration: number | null) {
    duration = duration ?? 0;
    const hours = duration / 60 / 60;
    return {
      hours: Math.floor(hours),
      tenths: Math.round(((hours - Math.floor(hours)) * 10))
    }
  }

  onMoreClick(event: any, entry: LogBookEntry) {
    event.stopPropagation();
  }

  removeFlight(entry: LogBookEntry) {
    of(null).pipe(
      withLatestFrom(this.auth.user$),
      switchMap(([_, user]) => this.flightService.removePilot(entry.flight.flightId!, user!.userId))
    ).subscribe(() => {
      this.refresh$.next();
    })
    
  }

  public mToFt(value: number | undefined | null): number | null {
    return UnitUtils.mToFt(value);
  }

}

