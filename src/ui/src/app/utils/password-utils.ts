import { AbstractControl } from "@angular/forms";

export function passwordComplexityValidator(control: AbstractControl) {
    const password: string | null = control.value;
  
    if (!password)
      return null;
  
    if (password.length < 8
        || !/[0-9]/.test(password))
      return {
        'password': "Password must be at least 8 characters and contain a number."
      };
  
    return null;
  };