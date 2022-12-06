import { Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { LoginComponent } from './components/login/login.component';
import { Scopes, User } from './models/user.model';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'GliderView';

  public canViewAllUsers$: Observable<boolean>;
  public isAuthenticated$: Observable<boolean>;
  public user$: Observable<User | null>;

  /**
   *
   */
  constructor(private dialog: MatDialog, private auth: AuthService) {
    this.canViewAllUsers$ = this.auth.hasScope(Scopes.ViewAllUsers);
    this.isAuthenticated$ = this.auth.isAuthenticated$;
    this.user$ = this.auth.user$;

    this.auth.init();
  }

  public logIn() {
    this.dialog.open(LoginComponent);
  }

  public logOut() {
    this.auth.logOut();
  }
}
