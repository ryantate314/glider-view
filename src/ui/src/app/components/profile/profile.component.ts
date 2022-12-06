import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, of, switchMap } from 'rxjs';
import { User } from 'src/app/models/user.model';
import { AuthService } from 'src/app/services/auth.service';
import { ChangePasswordModalComponent } from '../change-password-modal/change-password-modal.component';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {

  public user$: Observable<User | null>;

  constructor(
    private auth: AuthService,
    private dialog: MatDialog,
  ) {
    this.user$ = this.auth.user$;
  }

  ngOnInit(): void {
  }

  public changePassword() {
    this.dialog.open(
      ChangePasswordModalComponent
    )
      .afterClosed()
      .pipe(
        switchMap((password: { currentPassword: string, newPassword: string } | undefined) => {
          if (password) {
            return this.auth.updatePassword(password.currentPassword, password.newPassword);
          }
          return of(null);
        })
      ).subscribe(() => {

      });
  }

}
