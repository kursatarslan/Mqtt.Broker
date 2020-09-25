using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mqtt.Data.Contracts;
using Mqtt.Domain.Models;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using Newtonsoft.Json;
using Mqtt.Application.Helpers;

namespace Mqtt.Application.Services
{
    public class MqttServerApplicationMessageInterceptor : IMqttServerApplicationMessageInterceptor
    {
        private readonly IMqttRepository _repo;
        private readonly ILogger<MqttServerApplicationMessageInterceptor> _logger;

        public MqttServerApplicationMessageInterceptor(
            IMqttRepository repo,
            ILogger<MqttServerApplicationMessageInterceptor> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            context.AcceptPublish = true;
            if (context.ApplicationMessage == null)
                return;

            var payload = context.ApplicationMessage.Payload == null
                ? null
                : Encoding.ASCII.GetString(context.ApplicationMessage?.Payload, 0, context.ApplicationMessage.Payload.Count());

            if(payload == null) {
                 _logger.LogError(
                $"Payload is null Message: ClientId = {context.ClientId}, Topic = {context.ApplicationMessage?.Topic},"
                + $" Payload = {payload}, QoS = {context.ApplicationMessage?.QualityOfServiceLevel},"
                + $" Retain-Flag = {context.ApplicationMessage?.Retain}");
                return;
            }
            
            var afterPars = Functions.GetPayload(context.ApplicationMessage?.Payload);
            if (context.ApplicationMessage.Retain)
            {
                var msg = _repo.AddMessage(new MqttMessage
                {
                    Created = DateTime.Now,
                    Message =  JsonConvert.SerializeObject(afterPars, Formatting.Indented),
                    Topic = context.ApplicationMessage.Topic,
                    ContentType = context.ApplicationMessage.ContentType
                });
                //var tree = context.ApplicationMessage.Topic.Split('/');

                if (await _repo.SaveChangesAsync())
                    context.AcceptPublish = true;
            }

            _logger.LogInformation(
                $"Message: ClientId = {context.ClientId}, Topic = {context.ApplicationMessage?.Topic},"
                + $" Payload = {payload}, QoS = {context.ApplicationMessage?.QualityOfServiceLevel},"
                + $" Retain-Flag = {context.ApplicationMessage?.Retain}");
        }
    }
}