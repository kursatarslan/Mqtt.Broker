using Mqtt.Domain.Enums;

namespace Mqtt.Domain.Models
{
  public class Payload
  {
    public int Id { get; set; }
    public Maneuver Maneuver { get; set; }
    public int PlatoonGap { get; set; }
    public bool PlatoonOverrideStatus { get; set; }
    public int VehicleRank { get; set; }
    public int BreakPedal { get; set; }
    public bool PlatoonDissolveStatus { get; set; }
    public uint StationId { get; set; }
    public int StreamingRequests { get; set; }
    public bool V2HealthStatus { get; set; }
    public int TruckRoutingStaus { get; set; }
    public string RealPayload { get; set; }
    public uint MyPlatoonId { get; set; }
  }
}