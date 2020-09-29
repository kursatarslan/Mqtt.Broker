using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mqtt.Context;
using Mqtt.Data.Contracts;
using Mqtt.Domain.Models;
using MQTTnet;
using MQTTnet.Server;

namespace Mqtt.Application.Services
{
    public class MqttStorage : IMqttServerStorage
    {
        private readonly IServiceProvider _serviceProvider;
        public MqttStorage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public  Task SaveRetainedMessagesAsync(
            IList<MqttApplicationMessage> messages)
        {/*
            using (var newcontext = new DataContext(new DbContextOptions<DataContext>()))
            {
                foreach (var message in messages)
                {
                    var payload = message?.Payload == null ? null : BitConverter.ToString((message?.Payload));
                    var msg = newcontext.MqttMessages.Add(new MqttMessage
                    {
                        Created = DateTime.Now,
                        Message = payload,
                        Topic = message?.Topic,
                        ContentType = message?.ContentType
                    });
                }
                //newcontext.SaveChanges();
            }*/


           
            messages.Clear();
            return Task.CompletedTask;
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            var context = _serviceProvider.GetRequiredService<DataContext>();
            
            var messages = context.MqttMessages;
            IList<MqttApplicationMessage> lst = messages.Select(m => new MqttApplicationMessage
            {
                Payload = Encoding.UTF8.GetBytes(m.Message),
                Topic = m.Topic,
                Retain = true,
                ContentType = m.ContentType,
            }).ToList();
            return Task.FromResult(lst);
        }
    }
}