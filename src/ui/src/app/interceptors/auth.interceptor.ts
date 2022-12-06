import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HTTP_INTERCEPTORS
} from '@angular/common/http';
import { iif, Observable, switchMap } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const IGNORE_AUTH_HEADER_NAME = "X-Ignore-Auth-Interceptor";
export const IGNORE_AUTH_HEADER = { [IGNORE_AUTH_HEADER_NAME]: "true" };

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(private auth: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {

    if (request.headers.has(IGNORE_AUTH_HEADER_NAME)) {
      request = request.clone({
        headers: request.headers.delete(IGNORE_AUTH_HEADER_NAME)
      });
      return next.handle(request);
    }

    // TODO: Implement auto refresh
    return this.auth.isAuthenticated$.pipe(
      switchMap(isAuthenticated =>
        iif(
          () => isAuthenticated,
          this.auth.token$.pipe(
            switchMap(token => {
              var clone = request.clone({
                setHeaders: {
                  'Authorization': `Bearer ${token.value}`
                }
              });
              return next.handle(clone);
            })
          ),
          next.handle(request)
        )
      )
    );
  }
}

export const AUTH_INTERCEPTOR = { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true };
