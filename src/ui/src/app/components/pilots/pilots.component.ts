import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { filter, Observable, switchMap, tap } from 'rxjs';
import { User } from 'src/app/models/user.model';
import { UserService } from 'src/app/services/user.service';
import { AddUserModalComponent } from '../add-user-modal/add-user-modal.component';
import { TitleService } from 'src/app/services/title.service';
import { SnackbarService } from 'src/app/services/snackbar.service';

@Component({
  selector: 'app-pilots',
  templateUrl: './pilots.component.html',
  styleUrls: ['./pilots.component.scss']
})
export class PilotsComponent implements OnInit {

  public users$: Observable<User[]>;

  constructor(
    private userService: UserService,
    private dialog: MatDialog,
    private snackbar: SnackbarService,
    title: TitleService
  ) {
    this.users$ = this.userService.getAll();

    title.setTitle("Pilots");
  }

  private refreshUsers() {
    this.users$ = this.userService.getAll();
  }

  ngOnInit(): void {
  }

  public createUser() {
    this.dialog.open(AddUserModalComponent, {
      panelClass: 'dialog-md'
    })
      .afterClosed()
      .pipe(
        filter(x => x)
      )
      .subscribe(() => this.refreshUsers());
  }

  public onEditUser(user: User) {
    this.dialog.open(AddUserModalComponent, {
      data: {
        user
      },
      panelClass: 'dialog-md'
    }).afterClosed()
    .pipe(
      filter(x => x)
    )
    .subscribe(() => {
      this.refreshUsers();
    })
  }

  public onDeleteUser(user: User) {
    this.snackbar.open(`Are you sure you want to delete ${user.name}?`, "Yes").pipe(
      switchMap(() => this.userService.deleteUser(user.userId))
    ).subscribe({
      next: () => this.refreshUsers(),
      error: (error: any) => this.snackbar.openError("There was a problem deleting the user. Please try again.")
    });
  }

}
