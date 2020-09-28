using System.Threading.Tasks;
using Mqtt.Data.Contracts;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Mqtt.Domain.Models;

namespace Mqtt.Application.Services
{
    public class MqttServerConnectionValidator: IMqttServerConnectionValidator
    {
        private readonly IMqttRepository _repo;
        private readonly ILogger<MqttServerConnectionValidator> _logger;

        public MqttServerConnectionValidator(
            IMqttRepository repo,
            ILogger<MqttServerConnectionValidator> logger)
        {
            _repo = repo;
            _logger = logger;
        }
        
        public async Task ValidateConnectionAsync(
            MqttConnectionValidatorContext context)
        {
            
            var currentUser = _repo.GetUser(context.Username);

            if (currentUser == null)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                await LogMessage(context);
                return ;
            }

            if (context.Username != currentUser.Username)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                await LogMessage(context);
                return;
            }

            if (context.Password != currentUser.Password)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                await LogMessage(context);
                return ;
            }

            context.ReasonCode = MqttConnectReasonCode.Success;
            //_repo.
            await LogMessage(context,true);
            return;
        }

        private async Task LogMessage(MqttConnectionValidatorContext context, bool newConnection = false,
            bool showPassword = false)
        {
            if (context == null)
            {
                return;
            }

            if (newConnection) 
            {
                var connection = new Connection
                {
                    ClientId = context.ClientId,
                    CleanSession = context.CleanSession,
                    Endpoint = context.Endpoint,
                    Password = context.Password,
                    Username = context.Username

                };
                _repo.AddConnection(connection);
                await _repo.SaveChangesAsync();
            }

            if (showPassword)
            {
                _logger.LogInformation(
                    $"New connection: ClientId = {context.ClientId}, Endpoint = {context.Endpoint},"
                    + $" Username = {context.Username}, Password = {context.Password},"
                    + $" CleanSession = {context.CleanSession}");
            }
            else
            {
                _logger.LogInformation(
                    $"New connection: ClientId = {context.ClientId}, Endpoint = {context.Endpoint},"
                    + $" Username = {context.Username}, CleanSession = {context.CleanSession}");
            }
        }
    }
}