import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  constructor() { }

  public get flightSortOrder(): 'asc' | 'desc' {
    const value  = <'asc' | 'desc' | null>localStorage.getItem('flightSortOrder');
    return value ?? 'asc';
  }

  public set flightSortOrder(value: string) {
    localStorage.setItem('flightSortOrder', value);
  }
}
