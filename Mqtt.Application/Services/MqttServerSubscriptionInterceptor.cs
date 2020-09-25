using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mqtt.Data.Contracts;
using MQTTnet.Server;

namespace Mqtt.Application.Services
{
    public class MqttServerSubscriptionInterceptor : IMqttServerSubscriptionInterceptor
    {
        private readonly ILogger<MqttServerSubscriptionInterceptor> _logger;
        private readonly IMqttRepository _repo;

        public MqttServerSubscriptionInterceptor(ILogger<MqttServerSubscriptionInterceptor> logger,
        IMqttRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }
        
        public Task InterceptSubscriptionAsync(
            MqttSubscriptionInterceptorContext context)
        {
            if (context == null)
            {
                return Task.CompletedTask;
            }

            if (context.TopicFilter.Topic.StartsWith("platooning/broadcast/"))
            {
                var plotooningId = context.TopicFilter.Topic.Replace("platooning/broadcast/", "");

                try
                {
                    var followvehicle = _repo.GetPlatoon(context.ClientId); 

                    if (followvehicle == null)
                    {
                        context.AcceptSubscription = false;
                        context.CloseConnection = true;
                        _logger.LogInformation(
                            $"Reject for not found joined ClientId = {context.ClientId}, TopicFilter = {context.TopicFilter},"
                            + $" AcceptSubscription = {context.AcceptSubscription}, SessionItems = {context.SessionItems}");


                    }
                }
                catch (Exception exception)
                {
                    context.AcceptSubscription = false;
                    context.CloseConnection = true;
                    _logger.LogError($"Close connection for subcriptions Exception MqttSubscriptionInterceptorContext = {exception.StackTrace}");
                }
            }
                
            
            context.AcceptSubscription = true;
            _logger.LogInformation(
                $"New Subcription: ClientId = {context.ClientId}, TopicFilter = {context.TopicFilter},"
                + $" AcceptSubscription = {context.AcceptSubscription}, SessionItems = {context.SessionItems}");


            return Task.CompletedTask;
        }


    }
}