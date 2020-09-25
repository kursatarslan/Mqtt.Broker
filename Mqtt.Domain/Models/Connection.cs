using System;

namespace Mqtt.Domain.Models
{
    public class Connection
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string Endpoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool? CleanSession { get; set; }
    }
}
