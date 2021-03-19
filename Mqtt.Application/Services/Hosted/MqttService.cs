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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
                    var sub = _repo.GetSubscribeByTopic(e.ClientId, e.TopicFilter.Topic,
                        e.TopicFilter.QualityOfServiceLevel.ToString());
                    if (sub != null)
                    {

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
                finally
                {
                    _repo.SaveChangesAsync(cancellationToken);
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
                        var sub = _repo.GetSubscribeById(e.ClientId).Where(s => s.Topic == topicFilter);
                        var subscribes = sub as List<Subscribe> ?? sub.ToList();
                        if (subscribes.All(a => a.Topic != topicFilter)) return;
                        subscribes
                            .ForEach(a => a.Enable = false);
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
                finally
                {
                    _repo.SaveChangesAsync(cancellationToken);
                }
            });
            
            Server.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                try
                {
                    var payload = Functions.GetPayload(e.ApplicationMessage.Payload);
                    var payloadser = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var tree = e.ApplicationMessage.Topic.Split('/');
                    var audit = new Audit
                    {
                        ClientId = e.ClientId,
                        Type = "Pub",
                        Topic = e.ApplicationMessage.Topic,
                        Payload = payloadser
                    };
                    _repo.AddAudit(audit);
                    if (e.ClientId == null)
                    {
                        var log = new Log
                        {
                            Exception = new string("Broker publish message itself " +
                                                   payloadser + " " +
                                                   e.ClientId)
                        };
                        _repo.AddLogAsync(log);
                        //_repo.SaveChangesAsync(cancellationToken);
                        return;
                    }

                    if (!tree.First().Contains("platooning"))
                    {
                        var log = new Log
                        {
                            Exception = new string("Mqtt broker just only response with starting \"platoon\" " +
                                                   payloadser + " " +
                                                   e.ClientId)
                        };
                        _repo.AddLogAsync(log);
                        //_repo.SaveChangesAsync(cancellationToken);
                        return;
                    }

                    if (payload.PlatoonDissolveStatus)
                    {
                        Console.WriteLine(
                            $"[{DateTime.Now}] PlatoonDissolveStatus is true all platoon infor must be deleted" +
                            " Client Id " + e.ClientId + " payload " +
                            payload);

                        var platoonlist = _repo.GetPlatoon().Where(a => a.IsFollower);
                        var platoonlistArray = platoonlist as Platoon[] ?? platoonlist.ToArray();
                        foreach (var platoon in platoonlistArray)
                        {
                            Server.UnsubscribeAsync(platoon.ClientId, "platooning/broadcast/" + platoon.PlatoonRealId);
                        }
                        
                        _repo.DeletePlatoonRange(platoonlistArray);
                    }

                    if (payload.Maneuver == Maneuver.CreatePlatoon)
                    {
                        if (tree.Length != 4)
                        {
                            var log = new Log
                            {
                                Exception = new string("For creating platoon, topic must 4 length 1. \"platooning\" " +
                                                       " 2.  \"message\" " +
                                                       " 3.  \"leadvehicleID\" " +
                                                       " 4.  \"platoonID\" " +

                                                       " payload " +
                                                       payloadser + " " +
                                                       " client ID " + e.ClientId +
                                                       " topic " + e.ApplicationMessage.Topic)
                            };
                            _repo.AddLogAsync(log);
                            //_repo.SaveChangesAsync(cancellationToken);
                            return;
                        }

                        var platoonId = tree[3];
                        var platoonList = _repo.GetPlatoon().ToList();
                        var leadVehicleId = tree[2];
                        var leadVehicle = _repo.GetSubscribeById(leadVehicleId);
                        var pla = platoonList
                            .FirstOrDefault(f => f.Enable && f.PlatoonRealId == platoonId);
                        if (leadVehicle != null && pla == null)
                        {
                            var platoon = new Platoon()
                            {
                                Enable = true,
                                ClientId = e.ClientId,
                                IsLead = true,
                                VechicleId = tree[2],
                                PlatoonRealId = tree[3]
                            };
                            _repo.AddPlatoon(platoon);
                            Console.WriteLine($"[{DateTime.Now}] Creating new Platoon Client Id " + e.ClientId +
                                              " platooning Id" + platoon.PlatoonRealId + " payload " + payloadser);
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now}] Platoon is already created Client Id " + e.ClientId +
                                              " platooning Id" + platoonId + " payload " + payloadser);
                        }
                    }
                    else if (payload.Maneuver == Maneuver.JoinRequest)
                    {
                        if (tree.Length != 3)
                        {
                            var log = new Log
                            {
                                Exception = new string("For joining platoon, topic must 3 length 1. \"platooning\" " +
                                                       " 2.  \"message\" " +
                                                       " 3.  \"followingVehicleId\" " +
                                                       " 4.  \"#\" " +
                                                       " payload " +
                                                       payloadser + " " +
                                                       " client ID " + e.ClientId +
                                                       " topic " + e.ApplicationMessage.Topic)
                            };
                            _repo.AddLogAsync(log);
                            //_repo.SaveChangesAsync();
                            return;
                        }

                        var isFollowing = _repo.GetPlatoon()
                            .FirstOrDefault(f => f.IsFollower && f.VechicleId == tree[2] && f.Enable);
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
                                VechicleId = tree[2],
                                PlatoonRealId = platoonLead.PlatoonRealId
                            };
                            _repo.AddPlatoon(platoon);
                            Console.WriteLine($"[{DateTime.Now}] Join Platoon Client Id " + e.ClientId +
                                              " platooning Id" + platoon.PlatoonRealId + " payload " + payloadser);
                            var message = new BitArray(_dataLenght);
                            message.Set(0, false);
                            message.Set(1, false);
                            message.Set(2, true);

                            Server.PublishAsync("platooning/" + platoonLead.ClientId + "/" + tree[2],
                                Encoding.ASCII.GetString(Functions.BitArrayToByteArray(message)));
                        }
                    }
                    else if (payload.Maneuver == Maneuver.JoinAccepted)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Join accepted Client Id " + e.ClientId + " payload " +
                                          payloadser);
                        var followvehicleId = tree[1];
                        var leadVehicle = tree[2];
                        var plattonId = tree[3];
                        var allplatoon = _repo.GetPlatoon();

                        if (allplatoon != null)
                        {
                            var enumerable = allplatoon as Platoon[] ?? allplatoon.ToArray();
                            var  platoonfollow = enumerable.FirstOrDefault(f => f.IsFollower && f.ClientId == followvehicleId);
                            if (platoonfollow != null)
                            {
                                platoonfollow.Enable = true;
                                platoonfollow.PlatoonRealId = plattonId;
                                _repo.UpdatePlatoon(platoonfollow);
                            }else
                            {
                                var platoonlead = enumerable
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
                        
                    }
                    else if (payload.Maneuver == Maneuver.JoinRejected)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Join rejected Client Id " + e.ClientId + " payload " +
                                          payloadser);
                        var platoonfollow = _repo.GetPlatoon()
                            .FirstOrDefault(f => f.IsFollower && f.ClientId == tree[1]);

                        if (platoonfollow != null)
                        {
                            _repo.DeletePlatoon(platoonfollow);
                        }
                    }
                    else
                    {
                        var log = new Log
                        {
                            Exception = new string("Unknown Maneuver " +
                                                   payloadser+ " " +
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
                finally
                {
                    _repo.SaveChangesAsync(cancellationToken);
                }

                
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
