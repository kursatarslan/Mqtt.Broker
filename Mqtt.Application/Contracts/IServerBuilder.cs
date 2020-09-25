using System.Collections.Generic;
using Mqtt.Domain.Models;
using MQTTnet.Server;

namespace Mqtt.Application.Contracts
{
    public interface IServerBuilder
    {
        IMqttServer GetServer();

        public MqttServerOptionsBuilder GetOptionsBuilder(
            IMqttServerSubscriptionInterceptor interceptor,
            IMqttServerApplicationMessageInterceptor messageInterceptor);
    }
}