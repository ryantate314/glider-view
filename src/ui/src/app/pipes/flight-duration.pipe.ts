import { Pipe, PipeTransform } from '@angular/core';
import * as moment from 'moment';

@Pipe({
  name: 'flightDuration'
})
export class FlightDurationPipe implements PipeTransform {

  transform(seconds: number | null, ...args: unknown[]): string {
    if (seconds === null)
      return "";

    const duration = moment.duration(seconds, 'second');
    if (seconds > 60 * 60)
      return moment.utc(duration.as('milliseconds'))
        .format('H[h]m[m]s[s]')
    else if (seconds > 60)
      return moment.utc(duration.as('milliseconds'))
        .format('m[m]s[s]')
    else
      return seconds + "s";
  }

}
