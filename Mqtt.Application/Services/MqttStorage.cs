using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mqtt.Data.Contracts;
using Mqtt.Domain.Models;
using MQTTnet;
using MQTTnet.Server;

namespace Mqtt.Application.Services
{
    public class MqttStorage : IMqttServerStorage
    {
        private readonly IMqttRepository _repo;

        public MqttStorage(IMqttRepository repo)
        {
            _repo = repo;
        }
        
        public  Task SaveRetainedMessagesAsync(
            IList<MqttApplicationMessage> messages)
        {
             foreach (var message in messages)
             {
                 var payload = message?.Payload == null ? null : BitConverter.ToString((message?.Payload));
                 var msg = _repo.AddMessage(new MqttMessage
                 {
                     Created = DateTime.Now,
                     Message = payload,
                     Topic = message?.Topic,
                     ContentType = message?.ContentType
                 });
             }
            
             _repo.SaveChanges();
             messages.Clear();
            return Task.CompletedTask;
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            var messages = _repo.GetMessages();
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