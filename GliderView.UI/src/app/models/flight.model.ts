export interface Flight {
    flightId: string;
    startDate: Date;
    endDate: Date;
    duration: number;
    aircraft: string;
    releaseAltitude: number;
    maxAltitude: number;
}