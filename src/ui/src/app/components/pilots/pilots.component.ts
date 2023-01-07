import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { User } from 'src/app/models/user.model';
import { UserService } from 'src/app/services/user.service';
import { AddUserModalComponent } from '../add-user-modal/add-user-modal.component';

@Component({
  selector: 'app-pilots',
  templateUrl: './pilots.component.html',
  styleUrls: ['./pilots.component.scss']
})
export class PilotsComponent implements OnInit {

  public users$: Observable<User[]>;

  constructor(
    private userService: UserService,
    private dialog: MatDialog
  ) {
    this.users$ = this.userService.getAll();
  }

  ngOnInit(): void {
  }

  public createUser() {
    this.dialog.open(AddUserModalComponent);
  }

}
