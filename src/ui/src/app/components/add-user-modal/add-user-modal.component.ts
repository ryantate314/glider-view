import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { InvitationToken, Roles, User } from 'src/app/models/user.model';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-add-user-modal',
  templateUrl: './add-user-modal.component.html',
  styleUrls: ['./add-user-modal.component.scss']
})
export class AddUserModalComponent implements OnInit {

  public form: FormGroup;
  public Roles = Roles;

  private user: User | null = null;
  public token: InvitationToken | null = null;

  public submitted: boolean = false;

  public error: string | null = null;

  constructor(
    fb: FormBuilder,
    private userService: UserService,
    private matDialogRef: MatDialogRef<AddUserModalComponent>
  ) {
    this.form = fb.group({
      'name': [],
      'email': [],
      'role': [Roles.User, []]
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

  generateInvitation() {
    this.userService.getInvitationToken(this.user!.userId)
      .subscribe(token => {
        this.token = token;
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
    this.matDialogRef.close();
  }

}
