import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as dayjs from 'dayjs';
import { map, Observable, of, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { IGNORE_AUTH_HEADER } from '../interceptors/auth.interceptor';
import { LogBookEntry } from '../models/flight.model';
import { InvitationToken, Role, User, UserLogin } from '../models/user.model';
import { FlightService } from './flight.service';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private userCache: {
    expires: dayjs.Dayjs,
    users: User[]
  } | null = null;
  
  constructor(private http: HttpClient) { }

  public logIn(email: string, password: string): Observable<UserLogin> {
    return this.http.post<UserLogin>(
      `${environment.apiUrl}/users/login`,
      {
        email,
        password
      },
      {
        withCredentials: true,
        headers: IGNORE_AUTH_HEADER
      }
    );
  }

  public refreshToken(): Observable<UserLogin> {
    return this.http.post<UserLogin>(
      `${environment.apiUrl}/users/refresh`,
      null,
      {
        withCredentials: true,
        headers: IGNORE_AUTH_HEADER
      }
    );
  }


  public logOut(): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/users/logout`,
      null,
      {
        withCredentials: true
      }
    );
  }

  getAll(): Observable<User[]> {
    if (this.userCache != null && dayjs().isBefore(this.userCache.expires)) {
      return of(this.userCache.users);
    }

    return this.http.get<User[]>(
      `${environment.apiUrl}/users`
    ).pipe(
      tap(users => {
        this.userCache = {
          expires: dayjs().add(5, 'minutes'),
          users: users
        }
      })
    );
  }

  updatePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/users/update-password`,
      {
        currentPassword,
        newPassword
      }
    );
  }

  createUser(email: string, name: string, role: Role): Observable<User> {
    return this.http.post<User>(
      `${environment.apiUrl}/users`,
      {
        email,
        name,
        role
      }
    ).pipe(
      tap(() => this.userCache = null)
    );
  }

  getInvitationToken(userId: string): Observable<InvitationToken> {
    return this.http.get<InvitationToken>(
      `${environment.apiUrl}/users/${userId}/invitation`
    ).pipe(
      map(token => ({
        ...token,
        expirationDate: new Date(token.expirationDate)
      }))
    );
  }

  validateInvitationToken(email: string, token: string): Observable<void> {
    return this.http.put<void>(
      `${environment.apiUrl}/users/validate-invitation`,
      {
        token,
        email
      }
    );
  }

  /**
   * Finish building out a user after validating their invitation token.
   * @param value 
   * @param value1 
   * @param arg2 
   */
  buildUser(email: string, password: string, token: string): Observable<UserLogin> {
    return this.http.post<UserLogin>(
      `${environment.apiUrl}/users/onboarding`,
      {
        email,
        password,
        token
      }
    );
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/users/${userId}`
    ).pipe(
      tap(() => this.userCache = null)
    );
  }

  getLogbook(userId: string): Observable<LogBookEntry[]> {
    return this.http.get<LogBookEntry[]>(
      `${environment.apiUrl}/users/${userId}/logbook`
    ).pipe(
      map(entries => entries.map(entry => ({
        ...entry,
        flight: FlightService.parseFlight(entry.flight)
      })))
    );
  }

  updateUser(user: User): Observable<User> {
    return this.http.put<User>(
      `${environment.apiUrl}/users/${user.userId}`,
      user
    ).pipe(
      tap(user => this.userCache = null)
    );
  }
}
