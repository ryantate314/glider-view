import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { map, Observable, of, switchMap, withLatestFrom } from 'rxjs';
import { AuthService } from 'src/app/services/auth.service';
import { TitleService } from 'src/app/services/title.service';
import { UserService } from 'src/app/services/user.service';
import { passwordComplexityValidator } from 'src/app/utils/password-utils';

const passwordsMatch = (control: AbstractControl) => {
  const form = control.parent;
  if (!form)
    return null;

  const password = form.get('password')?.value;
  if (control.value != password)
    return {
      'passwords-match': true
    };

  return null;
};

@Component({
  selector: 'app-welcome',
  templateUrl: './welcome.component.html',
  styleUrls: ['./welcome.component.scss']
})
export class WelcomeComponent implements OnInit {

  public form: FormGroup;
  private token$: Observable<string | null>;

  public errorMessage: string | null = null;

  constructor(
    private readonly userService: UserService,
    private readonly route: ActivatedRoute,
    private readonly auth: AuthService,
    private readonly router: Router,
    fb: FormBuilder,
    title: TitleService
  ) {
    title.setTitle("New User Registration");
    
    this.token$ = this.route.paramMap.pipe(
      map(x => x.get('invitationToken'))
    );

    this.form = fb.group({
      'email': ['', [
        Validators.email
      ]],
      'password': ['', [
        passwordComplexityValidator
      ]],
      'confirmPassword': ['', [
        passwordsMatch
      ]]
    });

    this.password.valueChanges.subscribe(() =>
      this.confirmPassword.updateValueAndValidity()
    );
  }

  
  public get email() : AbstractControl {
    return this.form.get('email')!;
  }

  public get password() : AbstractControl {
    return this.form.get('password')!;
  }

  public get confirmPassword() : AbstractControl {
    return this.form.get('confirmPassword')!;
  }
  

  ngOnInit(): void {
  }

  public onSubmit(event: any) {
    if (this.form.valid) {
      of(true).pipe(
        withLatestFrom(this.token$),
        switchMap(([_, token]) =>
          this.userService.buildUser(this.email.value, this.password.value, token!)
        ),
        switchMap(login =>
          this.auth.logIn(this.email.value, this.password.value)
        )
      ).subscribe({
        next: user => {
          this.router.navigate(['/']);
        },
        error: error => {
          if (error instanceof HttpErrorResponse) {
            if (error.status == HttpStatusCode.Unauthorized) {
              this.errorMessage = "Your invitation token is expired or the email does not match the value we have on file. Please contact Chilhowee Gliderport for assistance.";
            }
            else {
              this.errorMessage = "There was a problem creating your user account. Please try again later.";
            }
          }
        }
      });
    }
  }

}
