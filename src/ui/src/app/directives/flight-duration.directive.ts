import { Component, Directive, ElementRef, Input } from '@angular/core';

/**
 * Formats the provided duration in hours, minutes, and seconds. It also calculates the Hobs time (10ths of hours) and adds it as a tooltip.
 */
@Component({
  selector: 'appFlightDuration',
  template: '<span title="{{ seconds != null ? calculateHobsTime(seconds) + \' hours\' : \'\' }}">{{ seconds != null ? (seconds | flightDuration) : defaultText }}</span>'
})
export class FlightDurationDirective {

  @Input()
  seconds: number | null = null;

  @Input()
  defaultText: string = "";

  constructor() {
  }

  public calculateHobsTime(seconds: number | null) {
    if (seconds == null)
      return null;
    return Math.round(seconds / 60.0 / 60.0 * 10) / 10.0;
  }

}
