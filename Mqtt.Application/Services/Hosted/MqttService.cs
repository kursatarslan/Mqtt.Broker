using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Mqtt.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using Mqtt.Domain.Models;
using Newtonsoft.Json;
using Mqtt.Data.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mqtt.Application.Helpers;
using Mqtt.Domain.Enums;
using MQTTnet.Client.Receiving;

namespace Mqtt.Application.Services.Hosted
{
    public class MqttService : IHostedService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IMqttServerSubscriptionInterceptor _interceptor;
        private readonly IMqttServerApplicationMessageInterceptor _messageInterceptor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IMqttRepository _repo;
        private int _dataLenght = 61;
        public event EventHandler<byte[]> DataReceived;
        public IMqttServer Server { get; set; }

        public MqttService(
            ILogger<MqttService> logger,
            IMqttServerSubscriptionInterceptor interceptor,
            IMqttServerApplicationMessageInterceptor messageInterceptor,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime appLifetime,
            IMqttRepository repo)
        {
            _logger = logger;
            _interceptor = interceptor;
            _messageInterceptor = messageInterceptor;
            _serviceProvider = serviceProvider;
            _appLifetime = appLifetime;
            _repo = repo;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);
            
            using var scope = _serviceProvider.CreateScope();
            var serverBuilder = scope.ServiceProvider.GetRequiredService<IServerBuilder>();
            var options = serverBuilder.GetOptionsBuilder( _interceptor, _messageInterceptor);
            Server = serverBuilder.GetServer();
            await Server.StartAsync(options.Build());

            Server.StartedHandler = new MqttServerStartedHandlerDelegate(e =>
            {
                Console.WriteLine("Mqtt Broker start " + e);
            });
            Server.StoppedHandler = new MqttServerStoppedHandlerDelegate(e =>
            {
                Console.WriteLine("Mqtt Broker stop " + e);
            });

            Server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(e =>
            {
                var vehicleId = e.TopicFilter.Topic
                    .Replace("platooning/", "").Replace("/#", "");
                
                _logger.LogInformation("Client subscribed " + e.ClientId + " topic " + e.TopicFilter.Topic + "Vehicle Id " +
                                  vehicleId);
                try
                {
                    var audit = new Audit
                    {
                        ClientId = e.ClientId,
                        Type = "Sub",
                        Topic = e.TopicFilter.Topic,
                        Payload = JsonConvert.SerializeObject(e.TopicFilter, Formatting.Indented)
                    };
                    _repo.AddAudit(audit);
                    var sub = _repo.GetSubscribeByTopic(e.ClientId,e.TopicFilter.Topic,e.TopicFilter.QualityOfServiceLevel.ToString());
                    if (sub != null) {

                        _logger.LogInformation($"There is a subcribe like ClientID {e.ClientId}.");
                        return;
                    }
                    var subClient = new Subscribe
                    {
                        Topic = e.TopicFilter.Topic,
                        Enable = true,
                        ClientId = e.ClientId,
                        QoS = e.TopicFilter.QualityOfServiceLevel.ToString()
                    };
                    _repo.AddSubscribe(subClient);
                    _repo.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    var log = new Log
                    {
                        Exception = exception.StackTrace
                    };
                    _repo.AddLogAsync(log);
                    _logger.LogError("Error = MqttServerClientSubscribedHandlerDelegate ", exception.StackTrace);
                }
            });

            Server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(e =>
            {
                try
                {
                    var clientId = e.ClientId;
                    var topicFilter = e.TopicFilter;
                    _logger.LogInformation($"[{DateTime.Now}] Client '{clientId}' un-subscribed to {topicFilter}.");
                    try
                    {
                        var sub = _repo.GetSubscribeById(e.ClientId);
                        var subscribes = sub as List<Subscribe> ?? sub.ToList();
                        if (!subscribes.Any()) return;
                        subscribes.ForEach(a=>a.Enable=false);
                        _repo.SaveChanges();
                    }
                    catch (Exception exception)
                    {
                        var log = new Log
                        {
                            Exception = exception.StackTrace
                        };
                        _repo.AddLogAsync(log);
                        _logger.LogError("Error = MqttServerClientSubscribedHandlerDelegate ", exception.StackTrace);

                        Console.WriteLine(exception);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Client get error " + ex.Message);
                }
            });
            
            Server.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                try
                {
                    var payload = Functions.GetPayload(e.ApplicationMessage.Payload);
                    var tree = e.ApplicationMessage.Topic.Split('/');
                    var audit = new Audit
                    {
                        ClientId = e.ClientId,
                        Type = "Pub",
                        Topic = e.ApplicationMessage.Topic,
                        Payload = JsonConvert.SerializeObject(payload, Formatting.Indented)
                    };
                    _repo.AddAudit(audit);
                    if (e.ClientId == null)
                    {
                        var log = new Log
                        {
                            Exception = new string("Broker publish message itself " +
                                                   JsonConvert.SerializeObject(payload, Formatting.Indented) + " " +
                                                   e.ClientId)
                        };
                        _repo.AddLogAsync(log);
                        return;
                    }
                    
                    if (tree.First().Contains("platoon"))
                    {
                        var log = new Log
                        {
                            Exception = new string("Mqtt broker just only response with starting \"platoon\" " +
                                                   JsonConvert.SerializeObject(payload, Formatting.Indented) + " " +
                                                   e.ClientId)
                        };
                        _repo.AddLogAsync(log);
                        return;
                    }


                    if (payload.Maneuver == Maneuver.CreatePlatoon)
                    {
                        var vehPla = e.ApplicationMessage.Topic.Replace("platooning/message/", "");
                        var platoonId = vehPla.Split("/").Last();
                        var platoonList = _repo.GetPlatoon().ToList();
                        var pla = platoonList
                            .FirstOrDefault(f => f.Enable && f.PlatoonRealId == platoonId);
                        if (pla == null)
                        {
                            var platoon = new Platoon()
                            {
                                Enable = true,
                                ClientId = e.ClientId,
                                IsLead = true,
                                VechicleId = vehPla.Split("/").First(),
                                PlatoonRealId = vehPla.Split("/").Last()
                            };
                            _repo.AddPlatoon(platoon);
                            Console.WriteLine($"[{DateTime.Now}] Creating new Platoon Client Id " + e.ClientId +
                                              " platooning Id" + platoon.PlatoonRealId + " payload "  + audit.Payload);
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now}] Platoon is already created Client Id " + e.ClientId +
                                              " platooning Id" + platoonId + " payload "  + audit.Payload);
                        }
                    }
                    else if (payload.Maneuver == Maneuver.JoinRequest)
                    {
                        var followingVec = e.ApplicationMessage.Topic.Replace("platooning/message/", "");
                        var isFollowing =_repo.GetPlatoon().FirstOrDefault(f => f.IsFollower && f.VechicleId == followingVec && f.Enable);
                        if (isFollowing != null) return;
                        var platoonLead = _repo.GetPlatoon().FirstOrDefault(f => f.IsLead && f.Enable);
                        if (platoonLead != null)
                        {
                            var platoon = new Platoon()
                            {
                                Enable = false,
                                ClientId = e.ClientId,
                                IsLead = false,
                                IsFollower = true,
                                VechicleId = followingVec,
                                PlatoonRealId = platoonLead.PlatoonRealId
                            };
                            _repo.AddPlatoon(platoon);
                            Console.WriteLine($"[{DateTime.Now}] Join Platoon Client Id " + e.ClientId +
                                              " platooning Id" + platoon.PlatoonRealId + " payload "  + audit.Payload);
                            var message = new BitArray(_dataLenght);
                            message.Set(0, false);
                            message.Set(1, false);
                            message.Set(2, true);

                            Server.PublishAsync("platooning/" + platoonLead.ClientId + "/" + followingVec,
                                Encoding.ASCII.GetString(Functions.BitArrayToByteArray(message)));
                        }
                    }
                    else if (payload.Maneuver == Maneuver.JoinAccepted)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Join accepted Client Id " + e.ClientId + " payload " +
                                          audit.Payload);
                        var splitTopic = e.ApplicationMessage.Topic.Split("/");
                        var followvehicleId = splitTopic[1];
                        var leadVehicle = splitTopic[2];
                        var plattonId = splitTopic[3];
                        var platoonfollow = _repo.GetPlatoon()
                            .FirstOrDefault(f => f.IsFollower && f.ClientId == followvehicleId);

                        if (platoonfollow != null)
                        {
                            platoonfollow.Enable = true;
                            platoonfollow.PlatoonRealId = plattonId;
                            _repo.UpdatePlatoon(platoonfollow);
                        }
                        else
                        {
                            var platoonlead = _repo.GetPlatoon()
                                .FirstOrDefault(f => f.IsLead && f.Enable && f.ClientId == leadVehicle);
                            if (platoonlead != null)
                            {
                                var platoon = new Platoon()
                                {
                                    Enable = true,
                                    ClientId = e.ClientId,
                                    IsLead = false,
                                    IsFollower = true,
                                    VechicleId = followvehicleId,
                                    PlatoonRealId = platoonlead.PlatoonRealId
                                };
                                _repo.AddPlatoon(platoon);
                            }
                        }
                    }
                    else
                    {
                        var log = new Log
                        {
                            Exception = new string("Unknown Maneuver " +
                                                   JsonConvert.SerializeObject(payload, Formatting.Indented) + " " +
                                                   e.ClientId)
                        };
                        _repo.AddLogAsync(log);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    var log = new Log
                    {
                        Exception = exception.StackTrace
                    };
                    _repo.AddLogAsync(log);
                }

                _repo.SaveChanges();
                OnDataReceived(e.ApplicationMessage.Payload);
            });
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            // Perform post-startup activities here
        }

        private void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            // Perform on-stopping activities here
        }

        private void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
            // Perform post-stopped activities here
        }
        
        public void OnDataReceived(byte[] e)
        {
            DataReceived?.Invoke(this, e);
        }
    }
}
