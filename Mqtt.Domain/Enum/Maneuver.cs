namespace Mqtt.Domain.Enums
{
    public enum Maneuver
    {
        Unavailable = 0,
        JoinRequest,
        JoinAccepted,
        JoinRejected,
        SplitRequest,
        MergeRequest,
        ManueverCompleted,
        CreatePlatoon
    }
}