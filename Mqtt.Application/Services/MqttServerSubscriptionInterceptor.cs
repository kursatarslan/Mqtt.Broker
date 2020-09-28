using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Mqtt.Data.Contracts;
using Mqtt.Domain.Models;
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
                var tree = context.TopicFilter.Topic.Split('/');
                //var plotooningId = context.TopicFilter.Topic.Replace("platooning/broadcast/", "");
                var platoon = tree.Length > 2 ? _repo.GetPlatoonById(tree[2]) : null;
                if (platoon == null)
                {
                    context.AcceptSubscription = false;
                    context.CloseConnection = true;
                    _logger.LogInformation(
                        $"Reject for not found platoonId on our system | ClientId = {context.ClientId}, TopicFilter = {context.TopicFilter},"
                        + $" AcceptSubscription = {context.AcceptSubscription}, SessionItems = {context.SessionItems}");

                }

                try
                {
                    if (_repo.GetPlatoon(context.ClientId) == null)
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
                    _repo.AddLogAsync(new Log()
                    {
                        Exception =
                            $"Close connection for subscriptions Exception MqttSubscriptionInterceptorContext = {exception.StackTrace}",

                            CreationDate = DateTime.Now
                    });
                    _logger.LogError($"Close connection for subscriptions Exception MqttSubscriptionInterceptorContext = {exception.StackTrace}");
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