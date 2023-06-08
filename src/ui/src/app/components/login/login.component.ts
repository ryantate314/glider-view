import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { AuthService } from 'src/app/services/auth.service';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {

  public form: FormGroup;

  public loginError: string | null = null;

  constructor(private auth: AuthService,
    fb: FormBuilder,
    private matDialogRef: MatDialogRef<LoginComponent>,
  ) {
    this.form = fb.group({
      "email": ['', [Validators.required, Validators.email]],
      "password": ['', [Validators.required]]
    });
  }

  private get emailInput() {
    return this.form.get('email')!;
  }

  private get passwordInput() {
    return this.form.get('password')!;
  }

  ngOnInit(): void {
  }

  public onSubmit(event: any) {
    event.preventDefault();

    this.loginError = null;

    if (this.form.valid) {
      this.auth.logIn(this.emailInput.value, this.passwordInput.value)
        .subscribe({
          next: user => {
            this.matDialogRef.close();
          },
          error: (err: HttpErrorResponse) => {
            this.loginError = err.status === 401
              ? 'Invalid username or password.'
              : 'There was a problem logging you in. Please try again later.';
          }
        });
    }
  }

  public onClose() {
    this.matDialogRef.close();
  }

}
