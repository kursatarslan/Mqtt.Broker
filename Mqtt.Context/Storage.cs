using System;
using System.Linq;
using Mqtt.Domain.Models;

namespace Mqtt.Context
{
    public static class Storage
    {
        public static void Seed(DataContext context)
        {
            var test = context.MqttUsers.Find("test");
            if (test == null)
                context.MqttUsers.Add(new MqttUser
                {
                    Password = "test",
                    Username = "test"
                });

            context.SaveChanges();
        }
    }
}