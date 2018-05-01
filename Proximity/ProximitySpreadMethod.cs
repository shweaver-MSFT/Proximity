namespace Proximity
{
    public enum ProximitySpreadMethod
    {
        //
        // Summary:
        //     The proximity value at the ends of the proximity field stay at the max range.
        Pad,
        //
        // Summary:
        //     The proximity value at the ends of the proximity field revert to 0 when out of range.
        Clamp,
        //
        // Summary:
        //     The proximity value at the ends of the proximity field is repeated in the reverse direction and back until the space is filled.
        Reflect,
        //
        // Summary:
        //     The proximity value at the ends of the proximity field restarts at 0 and is repeated in the original direction until the space is filled.
        Repeat
    }
}
