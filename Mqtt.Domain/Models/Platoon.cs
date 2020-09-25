using System;

namespace Mqtt.Domain.Models
{
    public class Platoon
    {
        public int Id { get; set; }
        public string PlatoonRealId { get; set; }
        public string Type { get; set; }
        public string ClientId { get; set; }
        public string VechicleId { get; set; }
        public bool IsLead { get; set; } = false;
        public bool IsFollower { get; set; } = false;
        public bool Enable { get; set; } = false;
        public DateTime CreationDate { get; set; } = DateTime.Now;
    }
}