import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { combineLatest, iif, map, Observable, of, shareReplay, startWith, Subject, switchMap, tap, withLatestFrom } from 'rxjs';
import { LogBookEntry } from 'src/app/models/flight.model';
import { AuthService } from 'src/app/services/auth.service';
import { FlightService } from 'src/app/services/flight.service';
import { TitleService } from 'src/app/services/title.service';
import { UserService } from 'src/app/services/user.service';
import { UnitUtils } from 'src/app/unit-utils';

const sortEntries = (a: LogBookEntry, b: LogBookEntry) =>
  (a.flightNumber == null || b.flightNumber == null)
    ? a.flight.startDate.getTime() - b.flight.startDate.getTime()
    : a.flightNumber - b.flightNumber;

const pageSize = 10;

interface PageInfo {
  page: number;
  numPages: number;
  pageSize: number;

  nextEnabled: boolean;
  previousEnabled: boolean;

  links: number[];
}

@Component({
  selector: 'app-logbook',
  templateUrl: './logbook.component.html',
  styleUrls: ['./logbook.component.scss']
})
export class LogbookComponent implements OnInit {

  public allFlights$: Observable<LogBookEntry[]>;
  public flights$: Observable<LogBookEntry[]>;
  public userId$: Observable<string>;
  
  private page$: Observable<number>;
  private numPages$: Observable<number>;
  public pageInfo$: Observable<PageInfo>;

  private refresh$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private route: ActivatedRoute,
    private auth: AuthService,
    private flightService: FlightService,
    title: TitleService
  ) {

    // TODO: Consider adding the username to the title
    title.setTitle("Logbook");

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

   

    this.allFlights$ = combineLatest([
      this.userId$,
      this.refresh$.pipe(
        startWith(null)
      )
    ]).pipe(
      map(([userId, _]) => userId),
      switchMap(userId => this.userService.getLogbook(userId)),
      map(entries => [...entries].sort(sortEntries)),
      shareReplay(1)
    );

    this.numPages$ = this.allFlights$.pipe(
      map(x => Math.ceil(x.length / pageSize))
    );

    this.page$ = combineLatest([
      this.route.queryParams.pipe(
        map(x => x["page"])
      ),
      this.numPages$
    ]).pipe(
      // Default to the last page if one is not chosen
      map(([page, numPages]) => page ? +page : numPages - 1)
    );

    this.pageInfo$ = combineLatest([
      this.page$,
      this.numPages$
    ]).pipe(
      map(([page, numPages]) => this.generatePageInfo(page, numPages))
    );

    this.flights$ = combineLatest([
      this.allFlights$,
      this.page$
    ]).pipe(
      map(([flights, page]) =>
        flights.slice(page * pageSize, page * pageSize + pageSize)
      )
    );
  }

  private generatePageInfo(currentPage: number, numPages: number): PageInfo {
    let links: number[] = [currentPage];
    // First page
    if (currentPage == 0 && numPages > 1)
      for (let i = 1; i < Math.min(3, numPages); i++)
        links.push(i);
    // Last page
    else if (currentPage == (numPages - 1) && numPages > 1)
      for (let i = currentPage - 1; i >= Math.max(0, currentPage - 2); i--)
        links = [i, ...links];
    else if (numPages > 2)
      links = [currentPage - 1, currentPage, currentPage + 1];

    return {
      page: currentPage,
      numPages: numPages,
      nextEnabled: currentPage < numPages - 1,
      previousEnabled: currentPage > 0,
      pageSize: pageSize,
      links: links
    };
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
    return value ?
      Math.round(UnitUtils.mToFt(value)!)
      : null;
  }

}

