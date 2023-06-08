import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SnackbarService {

  constructor(private readonly snackbar: MatSnackBar) { }

  public open(message: string, action: string = "Close"): Observable<void> {
    return this.snackbar.open(
      message,
      action,
      {
        duration: 5*1000
      }
    ).afterDismissed()
    .pipe(
      map(() => void 0)
    );
  }

  public openError(message: string) {
    this.snackbar.open(
      message,
      "Close",
      {
        duration: 5*1000,
        panelClass: "error"
      }
    )
  }
}
