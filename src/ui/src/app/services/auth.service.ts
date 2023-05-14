import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of, ReplaySubject, startWith, throwError, withLatestFrom } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Scopes, Token, User, UserLogin } from '../models/user.model';
import { UserService } from './user.service';

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
    return this._isAuthenticated$;
  }

  private get _isLoggedIn(): boolean {
    return localStorage.getItem("gliderView-loggedIn") === 'true';
  }
  private set _isLoggedIn(value: boolean) {
    localStorage.setItem("gliderView-loggedIn", value.toString());
  }

  constructor(private userService: UserService,
    private http: HttpClient) {
  }

  public init() {
    if (this._isLoggedIn)
    {
      this.refreshToken().pipe(
        withLatestFrom(
          this.token$.pipe(
            startWith(null)
          )
        ),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 401)
            return of([undefined, null]);
          return throwError(() => err);
        })
      ).subscribe(([_, token]) => {
        if (token !== null)
          this._isAuthenticated$.next(true);
        else
          this._isAuthenticated$.next(false);
      });
    }
    else
      this._isAuthenticated$.next(false);

    this.isAuthenticated$.subscribe(isAuthenticated =>
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
      })
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
  }

  public updatePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.userService.updatePassword(currentPassword, newPassword);
  }
}
