using System;

namespace Mqtt.Domain.Models
{
    public class Subscribe
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string Topic { get; set; }
        public string QoS { get; set; }
        public bool Enable { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
    }
}