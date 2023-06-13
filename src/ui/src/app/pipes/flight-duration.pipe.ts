import { Pipe, PipeTransform } from '@angular/core';
import * as dayjs from 'dayjs';

@Pipe({
  name: 'flightDuration'
})
export class FlightDurationPipe implements PipeTransform {

  transform(seconds: number | null, ...args: unknown[]): string {
    if (seconds === null)
      return "";

    const duration = dayjs.duration(seconds, 'second');
    if (seconds > 60 * 60)
      return dayjs.utc(duration.as('milliseconds'))
        .format('H[h]m[m]s[s]')
    else if (seconds > 60)
      return dayjs.utc(duration.as('milliseconds'))
        .format('m[m]s[s]')
    else
      return seconds + "s";
  }

}
