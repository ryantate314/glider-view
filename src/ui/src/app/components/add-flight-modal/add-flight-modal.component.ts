import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog'
import { FlightService } from 'src/app/services/flight.service';

@Component({
  selector: 'app-add-flight-modal',
  templateUrl: './add-flight-modal.component.html',
  styleUrls: ['./add-flight-modal.component.scss']
})
export class AddFlightModalComponent implements OnInit {

  private file: File | null = null;

  constructor(private dialog: MatDialogRef<AddFlightModalComponent>, private flightService: FlightService) { }

  ngOnInit(): void {
  }

  onFileSelected(event: any) {
    this.file = event.target.files[0];
  }

  onSubmit() {
    
  }

  onCancel() {
    this.dialog.close();
  }

}
