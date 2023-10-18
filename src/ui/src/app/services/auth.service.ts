import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, distinctUntilChanged, map, Observable, of, ReplaySubject, shareReplay, startWith, throwError, withLatestFrom } from 'rxjs';
import { Scopes, Token, User, UserLogin } from '../models/user.model';
import { UserService } from './user.service';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private _token$ = new ReplaySubject<Token>(1);
  public get token$(): Observable<Token> {
    return this._token$;
  }

  private _user$ = new ReplaySubject<User | null>(1);
  public get user$(): Observable<User | null> {
    return this._user$;
  }

  private _scopes$ = new ReplaySubject<string[]>(1);
  public get scopes$(): Observable<string[]> {
    return this._scopes$;
  }

  private _isAuthenticated$ = new ReplaySubject<boolean>(1);
  public get isAuthenticated$(): Observable<boolean> {
    return this._isAuthenticated$.pipe(
      distinctUntilChanged()
    );
  }

  private get _isLoggedIn(): boolean {
    return localStorage.getItem("gliderView-loggedIn") === 'true';
  }
  private set _isLoggedIn(value: boolean) {
    localStorage.setItem("gliderView-loggedIn", value.toString());
  }

  constructor(
    private userService: UserService,
    private http: HttpClient,
    private router: Router
    ) {
  }

  public init() {
    // Triggered on page refresh
    // Save a flag to session storage indicating the user is logged in. If so,
    // make an API call to the server to see if our token is still valid. The access
    // token is not stored in session for security.s
    if (this._isLoggedIn)
    {
      this.refreshToken().pipe(
        withLatestFrom(
          this.token$.pipe(
            startWith(null)
          )
        ),
        catchError((err: HttpErrorResponse) => {
          if (err.status !== 401)
            this.router.navigate(['/error']);

          return of([undefined, null]);
        })
      ).subscribe(([_, token]) => {
        if (token !== null)
          this._isAuthenticated$.next(true);
        else
          this._isAuthenticated$.next(false);
      });
    }
    else {
      this._isAuthenticated$.next(false);
      this._user$.next(null);
    }

    this.isAuthenticated$.subscribe(isAuthenticated =>
      // Persist login state to session storage
      this._isLoggedIn = isAuthenticated
    );
  }

  public logIn(email: string, password: string): Observable<User> {
    return this.userService.logIn(email, password).pipe(
      map(login => {

        this._token$.next(login.token);
        this._user$.next(login.user);
        this._scopes$.next(login.scopes);
        this._isAuthenticated$.next(true);

        return login.user;
      })
    );
  }

  public hasScope(scope: Scopes): Observable<boolean> {
    return this.scopes$.pipe(
      map(scopes => {
        return scopes.some(x => x == scope);
      }),
      shareReplay(1)
    );
  }

  public refreshToken(): Observable<void> {
    return this.userService.refreshToken().pipe(
      map(login => {
        this._token$.next(login.token);
        this._user$.next(login.user);
        this._scopes$.next(login.scopes);
        this._isAuthenticated$.next(true);
      })
    );
  }

  public logOut() {
    this._user$.next(null);
    this._isAuthenticated$.next(false);
    this.userService.logOut()
      .subscribe();
    this._scopes$.next([]);
  }

  public updatePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.userService.updatePassword(currentPassword, newPassword);
  }
}
