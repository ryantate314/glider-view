import { Component, OnInit, Inject } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup } from '@angular/forms';
import { MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { combineLatest, debounce, distinctUntilChanged, map, Observable, ReplaySubject, shareReplay, startWith, timer, withLatestFrom } from 'rxjs';
import { Flight, Occupant } from 'src/app/models/flight.model';
import { User } from 'src/app/models/user.model';
import { FlightService } from 'src/app/services/flight.service';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-assign-pilot-modal',
  templateUrl: './assign-pilot-modal.component.html',
  styleUrls: ['./assign-pilot-modal.component.scss']
})
export class AssignPilotModalComponent implements OnInit {

  public flight: Flight;
  private pilots$: Observable<User[]>;
  public filteredPilots$: Observable<User[]>;
  form: FormGroup;
  private selectedPilots: Occupant[] = [];
  public selectedPilots$ = new ReplaySubject<Occupant[]>(1);

  constructor(
    @Inject(MAT_DIALOG_DATA) data: { flight: Flight },
    private readonly userService: UserService,
    private readonly flightService: FlightService,
    private readonly fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<AssignPilotModalComponent>
  ) {
    this.flight = data.flight;

    this.form = fb.group({
      'pilot': []
    });

    this.pilots$ = this.userService.getAll();

    this.selectedPilots = this.flight.occupants ?
      [ ...this.flight.occupants ]
      : [];
    this.selectedPilots$.next(this.selectedPilots);

    const searchTerm$ = this.pilotsInput.valueChanges.pipe(
      startWith(''),
      debounce(() => timer(100)),
      distinctUntilChanged(),
    );

    this.filteredPilots$ = combineLatest([
      searchTerm$,
      this.pilots$,
      this.selectedPilots$]
    ).pipe(
      map(([searchTerm, allPilots, selectedPilots]) => allPilots.filter(x =>
        (searchTerm == '' || x.name.includes(searchTerm))
        && !selectedPilots.some(y => y.userId == x.userId)
      ))
    );
  }
  
  public get pilotsInput() : AbstractControl {
    return this.form.get('pilot')!; 
  }

  ngOnInit(): void {
  }

  public onPilotSelected(event: MatAutocompleteSelectedEvent) {
    this.pilots$.subscribe(pilots => {
      const pilot = pilots.find(x => x.userId == event.option.value)!;
      this.selectedPilots.push({
        flightNumber: null,
        name: pilot.name,
        notes: null,
        userId: pilot.userId
      });
      this.selectedPilots$.next(this.selectedPilots);
    });
    this.pilotsInput.setValue('');
  }

  public onRemovePilot(pilot: Occupant) {
    console.log("Removing pilot ", pilot);
    this.selectedPilots = this.selectedPilots.filter(x => x.userId != pilot.userId);
    this.selectedPilots$.next(this.selectedPilots);
  }

  public onSubmit(event: Event) {
    event.preventDefault();

    this.flightService.updatePilots(this.flight.flightId!, this.selectedPilots)
      .subscribe(() => {
        this.dialogRef.close(true);
      });
  }

}
