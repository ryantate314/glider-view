import { Flight } from "./flight.model";

export interface Leaderboard {
    date: Date;

    numFlightsThisYear: number;
    longestLengthFlightsThisYear: Flight[];
    longestDurationFlightsThisYear: Flight[];

    numFlightsThisMonth: number;
    longestLengthFlightsThisMonth: Flight[];
    longestDurationFlightsThisMonth: Flight[];

    numFlightsToday: number;
    longestLengthFlightsToday: Flight[];
    longestDurationFlightsToday: Flight[];
}