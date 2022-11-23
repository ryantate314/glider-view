export interface Flight {
    flightId: string | null;
    startDate: Date;
    endDate: Date | null;
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
    isGlider: boolean | null;
}

export interface Waypoint {
    time: Date;
    gpsAltitude: number;
    latitude: number;
    longitude: number;
}

export interface Statistics {
    distanceTraveled: number | null;
    releaseHeight: number | null;
    altitudeGained: number | null;
    maxAltitude: number | null;
    patternEntryAltitude: number | null;
}