export class UnitUtils
{
    public static mToFt(meters: number | undefined | null): number | null {
        return !meters
            ? null
            : meters! * 3.281;
    }

    public static kmToNm(kilometers: number) {
        return kilometers
            ? this.mToFt(kilometers * 1000)! / 6067.0
            : null;
    }
}