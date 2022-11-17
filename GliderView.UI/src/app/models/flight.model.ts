export interface Flight {
    flightId: string;
    startDate: Date;
    endDate: Date;
    duration: number;
    aircraft: Aircraft | null;
    releaseAltitude: number;
    maxAltitude: number;
    igcFileName: string;
}

export interface Aircraft {
    aircraftId: string;
    description: string;
}