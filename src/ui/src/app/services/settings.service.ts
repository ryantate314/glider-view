import { Injectable } from '@angular/core';
import { DisplayMode } from '../models/display-mode';
import { CookieService } from 'ngx-cookie';
import * as dayjs from 'dayjs';
import { AuthService } from './auth.service';
import { Observable, Subject, combineLatest, distinctUntilChanged, map, of, shareReplay, startWith, switchMap, tap, withLatestFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  private _updateShouldShowPricing$ = new Subject<void>();
  public shouldShowPricing$: Observable<boolean>;

  private _updateShouldShowLiveAircraft$ = new Subject<void>();
  public shouldShowLiveAircraft$: Observable<boolean>;

  constructor(
    private readonly cookies: CookieService,
    private readonly auth: AuthService
  ) {
    this.shouldShowPricing$ = combineLatest([
      this.auth.user$,
      this._updateShouldShowPricing$.pipe(
        startWith(null)
      )
    ]).pipe(
      map(([user, _]) => {
        if (user === null)
          return false;
        else
          return this.cookies.get(this.generateCookieName(user.userId, "showPricing")) === "true"
      }),
      distinctUntilChanged(),
      shareReplay(1)
    );

    this.shouldShowLiveAircraft$ = combineLatest([
      this.auth.user$,
      this._updateShouldShowLiveAircraft$.pipe(
        startWith(null)
      )
    ]).pipe(
      map(([user, _]) => {
        if (user === null)
          return false;
        else
          return this.cookies.get(this.generateCookieName(user.userId, "showLiveAircraft")) === "true"
      }),
      distinctUntilChanged(),
      shareReplay(1)
    );
  }

  private getItem(key: string): string | null {
    return sessionStorage.getItem(key);
  }

  private setItem(key: string, value: string) {
    sessionStorage.setItem(key, value);
  }

  public get flightSortOrder(): 'asc' | 'desc' {
    const value  = <'asc' | 'desc' | null>this.getItem('flightSortOrder');
    return value ?? 'asc';
  }

  public set flightSortOrder(value: string) {
    this.setItem('flightSortOrder', value);
  }

  public set displayMode(value: DisplayMode) {
    this.setItem('displayMode', value);
  }

  public get displayMode() {
    const value = <DisplayMode | null>this.getItem('displayMode');
    return value ?? DisplayMode.Table;
  }

  private generateCookieName(userId: string, name: string): string {
    return `${userId}-${name}`;
  }

  public setShouldShowPricing(value: boolean | null): Observable<void> {
    const result = of(true).pipe(
      withLatestFrom(this.auth.user$.pipe(
        startWith(null)
      )),
      tap(([_, user]) => {
        if (user !== null) {
          this.cookies.put(
            this.generateCookieName(user.userId, "showPricing"),
            (!!value) ? "true" : "false",
            {
              expires: dayjs().add(5, 'year').toDate()
            }
          );
          this._updateShouldShowPricing$.next();
        }
        else
          console.log("Cannot set preferences. User not logged in.");
      }),
      switchMap(_ => of())
    );
    result.subscribe();
    return result;
  }

  public setShouldShowLiveAircraft(value: boolean | null): Observable<void> {
    const result = of(true).pipe(
      withLatestFrom(this.auth.user$.pipe(
        startWith(null)
      )),
      tap(([_, user]) => {
        if (user !== null) {
          this.cookies.put(
            this.generateCookieName(user.userId, "showLiveAircraft"),
            (!!value) ? "true" : "false",
            {
              expires: dayjs().add(5, 'year').toDate()
            }
          );
          this._updateShouldShowLiveAircraft$.next();
        }
        else
          console.log("Cannot set preferences. User not logged in.");
      }),
      switchMap(_ => of())
    );
    result.subscribe();
    return result;
  }
}
