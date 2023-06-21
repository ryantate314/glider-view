import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { Scopes, User } from 'src/app/models/user.model';
import { AuthService } from 'src/app/services/auth.service';
import { LoginComponent } from '../login/login.component';

@Component({
  selector: 'app-menu-bar',
  templateUrl: './menu-bar.component.html',
  styleUrls: ['./menu-bar.component.scss']
})
export class MenuBarComponent implements OnInit {

  public canViewAllUsers$: Observable<boolean>;
  public isAuthenticated$: Observable<boolean>;
  public user$: Observable<User | null>;

  constructor(
    private dialog: MatDialog,
    private auth: AuthService,
    private router: Router
  ) {
    this.canViewAllUsers$ = this.auth.hasScope(Scopes.ViewAllUsers);
    this.isAuthenticated$ = this.auth.isAuthenticated$;
    this.user$ = this.auth.user$;
  }

  ngOnInit(): void {
  }

  public logIn() {
    this.dialog.open(LoginComponent, {
      panelClass: "dialog-md"
    });
  }

  public logOut() {
    this.auth.logOut();

    this.router.navigate(["/"]);
  }
}
