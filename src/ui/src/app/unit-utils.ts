export class UnitUtils
{
    public static mToFt(meters: number | undefined | null): number | null {
        return !meters
            ? null
            : Math.round(meters! * 3.281);
    }
}