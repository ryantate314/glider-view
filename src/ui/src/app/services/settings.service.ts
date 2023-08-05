import { Injectable } from '@angular/core';
import { DisplayMode } from '../models/display-mode';
import { CookieService } from 'ngx-cookie';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  constructor(
    private readonly cookies: CookieService
  ) { }

  private getItem(key: string): string | null {
    return sessionStorage.getItem(key);
  }

  private setItem(key: string, value: string) {
    sessionStorage.setItem(key, value);
  }

  public get flightSortOrder(): 'asc' | 'desc' {
    const value  = <'asc' | 'desc' | null>this.getItem('flightSortOrder');
    return value ?? 'asc';
  }

  public set flightSortOrder(value: string) {
    this.setItem('flightSortOrder', value);
  }

  public set displayMode(value: DisplayMode) {
    this.setItem('displayMode', value);
  }

  public get displayMode() {
    const value = <DisplayMode | null>this.getItem('displayMode');
    return value ?? DisplayMode.Table;
  }

  public get showPricing(): boolean | null {
    const value = this.cookies.get('showPricing');
    return value === undefined
      ? null
      : value === "true";
  }

  public set showPricing(value: boolean | null) {
    this.cookies.put('showPricing', (!!value) ? "true" : "false");
  }
}
