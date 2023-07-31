export interface AircraftLocationUpdate {
    latitude: number;
    longitude: number;
    lastCheckin: Date;
    model: string;
    altitude: number;
    distanceFromFieldKm: number;
    bearingFromField: number;
    registration: string;
    contestId: string;
}