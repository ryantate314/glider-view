import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { IGNORE_AUTH_HEADER } from '../interceptors/auth.interceptor';
import { User, UserLogin } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  updatePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/users/update-password`,
      {
        currentPassword,
        newPassword
      }
    );
  }

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
}
