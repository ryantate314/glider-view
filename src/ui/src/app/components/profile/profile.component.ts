import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, of, switchMap } from 'rxjs';
import { User } from 'src/app/models/user.model';
import { AuthService } from 'src/app/services/auth.service';
import { ChangePasswordModalComponent } from '../change-password-modal/change-password-modal.component';
import { TitleService } from 'src/app/services/title.service';

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
    title: TitleService
  ) {
    this.user$ = this.auth.user$;

    title.setTitle("User Profile");
  }

  ngOnInit(): void {
  }

  public changePassword() {
    this.dialog.open(
      ChangePasswordModalComponent,
      {
        panelClass: "dialog-md"
      }
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
