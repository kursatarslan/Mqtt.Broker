using System;

namespace Mqtt.Domain.Models
{
    public class Audit
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string ClientId { get; set; }
        public string Topic { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public string Payload { get; set; }
    }
}