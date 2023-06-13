
export interface UserLogin {
    scopes: string[];
    user: User;
    token: Token;
}

export interface Token {
    value: string;
    validTo: Date;
}

export interface User {
    userId: string;
    email: string;
    name: string;
    role: Role;
    isSetUp: boolean;
}

export enum Scopes {
    ViewAllUsers = "user:viewall",
    CreateUser = "user:create",
    AssignPilots = "flight:assignPilot",
    ManageFlights = "flight:manage"
}

export enum Role {
    Admin = "A",
    User = "U"
}

export interface InvitationToken {
    userId: string;
    token: string;
    expirationDate: Date;
}