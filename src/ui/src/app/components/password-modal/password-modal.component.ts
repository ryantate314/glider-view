import { HttpErrorResponse } from '@angular/common/http';
import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, switchMap, throwError, withLatestFrom } from 'rxjs';
import { AuthService } from 'src/app/services/auth.service';

/**
 * Modal used to confirm the user's password before performing a sensitive operation.
 */
@Component({
  selector: 'app-password-modal',
  templateUrl: './password-modal.component.html',
  styleUrls: ['./password-modal.component.scss']
})
export class PasswordModalComponent implements OnInit {

  public form: FormGroup;

  public loginError: string | null = null;

  constructor(private auth: AuthService,
    fb: FormBuilder,
    private matDialogRef: MatDialogRef<PasswordModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Partial<{ note: string }>
  ) {
    this.form = fb.group({
      "password": ['', [Validators.required]]
    });
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
      of(true).pipe(
        withLatestFrom(this.auth.user$),
        switchMap(([_, user]) => {
          if (user === null)
            throwError(() => "User is not logged in.");
          return this.auth.logIn(user!.email, this.passwordInput.value)
        })
      ).subscribe({
          next: user => {
            this.matDialogRef.close(true);
          },
          error: (err: any) => {
            if (err instanceof HttpErrorResponse)
              this.loginError = err.status === 401
                ? 'Invalid password.'
                : 'There was a problem logging you in. Please try again later.';
            else
              this.loginError = err.toString();
          }
        });
    }
  }

  public onClose() {
    this.matDialogRef.close();
  }


}
