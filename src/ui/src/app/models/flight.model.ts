export interface Flight {
    flightId: string;
    startDate: Date;
    endDate: Date;
    duration: number;
    aircraft: Aircraft | null;
    releaseAltitude: number;
    maxAltitude: number;
    igcFileName: string;

    statistics: Statistics | null;

    waypoints: Waypoint[] | null;
}

export interface Aircraft {
    aircraftId: string;
    description: string;
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
}