import { Flight } from "./flight.model";

export interface Leaderboard {
    date: Date;

    numFlightsThisYear: number;
    maxDistanceFromFieldThisYear: Flight[];
    longestDurationFlightsThisYear: Flight[];

    numFlightsThisMonth: number;
    maxDistanceFromFieldThisMonth: Flight[];
    longestDurationFlightsThisMonth: Flight[];

    numFlightsToday: number;
    maxDistanceFromFieldToday: Flight[];
    longestDurationFlightsToday: Flight[];
}