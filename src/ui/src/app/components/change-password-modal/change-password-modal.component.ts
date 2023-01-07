import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';

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
      "newPassword": ["", []],
      "newPasswordConfirm": ["", []],
    }, {
      validators: [
        (form: AbstractControl) => {
          const newPassword = form.get('newPassword')!;
          const newPasswordConfirm = form.get('newPasswordConfirm')!;

          if (newPasswordConfirm.value && newPassword.value != newPasswordConfirm.value)
            newPasswordConfirm.setErrors({ passwordMismatch: true });

          return null;
        }
      ]
    });
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
