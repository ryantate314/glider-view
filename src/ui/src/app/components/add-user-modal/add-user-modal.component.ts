import { Component, OnInit, Inject } from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { InvitationToken, Role, User } from 'src/app/models/user.model';
import { UserService } from 'src/app/services/user.service';
import { Location } from '@angular/common';

@Component({
  selector: 'app-add-user-modal',
  templateUrl: './add-user-modal.component.html',
  styleUrls: ['./add-user-modal.component.scss']
})
export class AddUserModalComponent implements OnInit {

  public form: FormGroup;
  public Roles = Role;

  public user: User | null = null;
  public token: string | null = null;

  public submitted: boolean = false;

  public error: string | null = null;

  public isEditing: boolean = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) data: { user: User } | null,
    fb: FormBuilder,
    private userService: UserService,
    private matDialogRef: MatDialogRef<AddUserModalComponent>,
    private location: Location
  ) {

    if (data?.user) {
      this.user = data.user;
      this.isEditing = true;
    }

    const email = data?.user?.email ?? "";
    const name = data?.user?.name ?? "";
    const role = data?.user?.role ?? Role.User;

    this.form = fb.group({
      'name': [name],
      'email': [email],
      'role': [role, []]
    });

  }

  ngOnInit(): void {
  }

  public get name(): AbstractControl {
    return this.form.controls['name'];
  }
  public get email(): AbstractControl {
    return this.form.controls['email'];
  }
  public get role(): AbstractControl {
    return this.form.controls['role'];
  }

  onSubmit(event: any) {
    event.preventDefault();

    if (this.form.valid) {
      this.error = null;

      if (this.user?.userId) {
        const updatedUser = {
          ...this.user,
          email: this.email.value,
          name: this.name.value,
          role: this.role.value
        };
        this.userService.updateUser(updatedUser).subscribe({
          next: user => {
            this.submitted = true;
            this.user = user;
          },
          error: err => this.error = "Error updating user."
        })
      }
      else {
        this.userService.createUser(
          this.email.value,
          this.name.value,
          this.role.value
        ).subscribe({
          next: user => {
            this.submitted = true;
            this.user = user;
          },
          error: err => {
            this.error = "Error creating user.";
          }
        });
      }
    }
  }

  generateInvitation() {
    this.userService.getInvitationToken(this.user!.userId)
      .subscribe(token => {
        this.token = window.location.origin + this.location.prepareExternalUrl(`welcome/${token.token}`);
      });
  }

  resetForm() {
    this.form.reset();
    this.submitted = false;
    this.token = null;
    this.user = null;
    this.error = null;
  }

  onClose() {
    this.matDialogRef.close(this.submitted);
  }

}
