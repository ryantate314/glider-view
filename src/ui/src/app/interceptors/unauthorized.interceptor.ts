import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HTTP_INTERCEPTORS,
  HttpErrorResponse,
  HttpStatusCode
} from '@angular/common/http';
import { Observable, catchError, iif, switchMap, throwError, withLatestFrom } from 'rxjs';
import { AuthInterceptor, IGNORE_AUTH_HEADER_NAME } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

/**
 * Listens for failed API requests and attempts to automatically refresh the Auth token.
 */
@Injectable()
export class UnauthorizedInterceptor implements HttpInterceptor {

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((err: HttpErrorResponse) =>
        iif(
          () => err.status == HttpStatusCode.Unauthorized && this.shouldRetry(request),
          this.auth.isAuthenticated$.pipe(
            switchMap(isAuthenticated =>
              iif(
                () => isAuthenticated,
                this.authenticateAndTryAgain(request, next),
                throwError(() => err)
              ))
          ),
          throwError(() => err)
        )
      )
    );
  }

  authenticateAndTryAgain(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    console.log("Detected unauthorized API call to " + request.url + ". Attempting to refresh token.");

    return this.auth.refreshToken().pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status == HttpStatusCode.Unauthorized) {
          console.log("Refresh attempt failed. Logging user out.");
          this.auth.logOut();
          this.router.navigate(["/"]);
        }
        else {
          console.log("Unexpected error while refreshing auth token");
        }
        return throwError(() => err);
      }),
      switchMap(() => next.handle(request))
    )
  }

  shouldRetry(request: HttpRequest<unknown>): boolean {
    return !request.headers.has(IGNORE_AUTH_HEADER_NAME);
  }
}

export const UNAUTHORIZED_INTERCEPTOR = { provide: HTTP_INTERCEPTORS, useClass: UnauthorizedInterceptor, multi: true };
