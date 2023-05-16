export interface Flight {
    flightId: string | null;
    startDate: Date;
    endDate: Date | null;
    /**
     * Seconds
     */
    duration: number | null;
    igcFileName: string | null;
    
    aircraft: Aircraft | null;
    towFlight: Flight | null;
    statistics: Statistics | null;
    waypoints: Waypoint[] | null;
}

export interface Aircraft {
    aircraftId: string;
    description: string;
    registrationId: string | null;
    isGlider: boolean | null;
}

export interface Waypoint {
    time: Date;
    gpsAltitude: number;
    latitude: number;
    longitude: number;
    flightEvent: FlightEventType | null;
}

export enum FlightEventType {
    release = 1,
    patternEntry = 2
}

export interface Statistics {
    /**
     * Meters
     */
    distanceTraveled: number | null;
    /**
     * Meters MSL
     */
    releaseHeight: number | null;
    altitudeGained: number | null;
    maxAltitude: number | null;
    patternEntryAltitude: number | null;
}

export interface LogBookEntry {
    flight: Flight;
    flightNumber: number | null;
    remarks: string | null;
}