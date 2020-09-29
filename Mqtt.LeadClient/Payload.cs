namespace Mqtt.LeadClient
{
    public class Payload
    {
        public int Maneuver { get; set; }
        public int PlatoonGap { get; set; }
        public bool PlatoonOverrideStatus { get; set; }
        public int VehicleRank { get; set; }
        public int BreakPedal { get; set; }
        public bool PlatoonDissolveStatus { get; set; }
        public int StationId { get; set; }
        public int StreamingRequests { get; set; }
        public bool V2HealthStatus { get; set; }
        public int TruckRoutingStaus { get; set; }
        public string RealPayload { get; set; }
    }
}