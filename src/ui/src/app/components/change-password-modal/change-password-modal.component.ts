import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { passwordComplexityValidator } from 'src/app/utils/password-utils';

const passwordsMatch = (control: AbstractControl) => {
  const form = control.parent;
  if (!form)
    return null;

  const password = form.get('newPassword')?.value;
  if (control.value != password)
    return {
      'passwords-match': true
    };

  return null;
};

@Component({
  selector: 'app-change-password-modal',
  templateUrl: './change-password-modal.component.html',
  styleUrls: ['./change-password-modal.component.scss']
})
export class ChangePasswordModalComponent implements OnInit {

  public form: FormGroup;
  public error: string | null = null;

  constructor(
    private matDialogRef: MatDialogRef<ChangePasswordModalComponent>,
    fb: FormBuilder
  ) {
    this.form = fb.group({
      "currentPassword": ["", []],
      "newPassword": ["", [
        passwordComplexityValidator
      ]],
      "newPasswordConfirm": ["", [
        passwordsMatch
      ]],
    });

    this.newPassword.valueChanges.subscribe(() =>
      this.newPasswordConfirm.updateValueAndValidity()
    );
  }

  public get currentPassword(): AbstractControl {
    return this.form.get('currentPassword')!;
  }
  public get newPassword(): AbstractControl {
    return this.form.get('newPassword')!;
  }
  public get newPasswordConfirm(): AbstractControl {
    return this.form.get('newPasswordConfirm')!;
  }

  ngOnInit(): void {
  }

  public onClose() {
    this.matDialogRef.close();
  }

  public onSubmit(event: any) {
    event.preventDefault();

    if (this.form.valid) {
      this.matDialogRef.close({
        'currentPassword': this.currentPassword.value,
        'newPassword': this.newPassword.value
      });
    }
  }

}
