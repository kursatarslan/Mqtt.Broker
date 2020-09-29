﻿using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace Mqtt.FollowerClient
{
    internal class Program
    {
        private const string clientId = "followingVehicle";


        public static IManagedMqttClient client =
            new MqttFactory().CreateManagedMqttClient(new MqttNetLogger("followingVehicle"));

        private static void Main(string[] args)
        {
            
            _ = ConnectAsync();
            do {
                while (!Console.KeyAvailable) {
                    if (Console.ReadKey(true).Key == ConsoleKey.S)
                    {
                        _ = SubscribeAsync("platooning/" + clientId + "/#");
                        Console.WriteLine("Client SubscribeAsync as  " + "platooning/" + clientId + "/#");
                    }else if (Console.ReadKey(true).Key == ConsoleKey.P)
                    {
                        var message = new BitArray(64);
                        message.Set(0, false);
                        message.Set(1, false);
                        message.Set(2, true);
                        //string message = HelperFunctions.RandomString(5,true);
                        _ = PublishAsync("platooning/message/" + clientId ,
                            Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                        Console.WriteLine("Client Publish joining spesific platoon at the broker as  " + "platooning/message/" + clientId + "  payload => " +
                                          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                    }
                }       
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
        //public event EventHandler<MqttClientConnectedEventArgs> Connected;
        //public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
        //public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;

        private static byte[] GenerateMessage()
        {
            //0x111 
            return new byte[4];
        }

        

        private static async Task ConnectAsync()
        {
            //const string mqttUri = "mqttbroker.westeurope.azurecontainer.io";
            const string mqttUri = "localhost";
            var mqttUser = "test";
            var mqttPassword = "test";
            var mqttPort = 1883;
            Console.WriteLine($"MQTT Server:{mqttUri} Username:{mqttUser} ClientID:{clientId}");
            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithCredentials(mqttUser, mqttPassword)
                .WithTcpServer(mqttUri, mqttPort)
                .WithKeepAlivePeriod(new TimeSpan(0, 0, 30))
                .WithCleanSession();

            var options = messageBuilder
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(2))
                .WithClientOptions(options)
                .Build();

            await client.StartAsync(managedOptions);

            client.UseConnectedHandler(e => { Console.WriteLine("Connected successfully with MQTT Brokers."); });
            client.UseDisconnectedHandler(e =>
            {
                new MqttClientDisconnectedHandlerDelegate(e => MqttClient_Disconnected(e));
                Console.WriteLine("Disconnected from MQTT Brokers.Client Was Connected " + e.ClientWasConnected);
            });
            client.UseApplicationMessageReceivedHandler(e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;

                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        var payload = HelperFunctions.GetPayload(e.ApplicationMessage.Payload);
                        Console.WriteLine($"Topic: {topic}. Message Received: {JsonConvert.SerializeObject(payload, Formatting.Indented)}");
                        var platoonId = topic.Replace("platooning/" + clientId + "/", "").Split("/").Last();
                        if (payload.Maneuver == 2 )
                        { 
                            
                            _ = SubscribeAsync("platooning/broadcast/" + platoonId + "/#");
                            Console.WriteLine("Client SubscribeAsync as  " + "platooning/broadcast/" + platoonId + "/#");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, ex);
                }
            });
        }
        
        private static async void MqttClient_Disconnected(MqttClientDisconnectedEventArgs e)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            try
            {
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reconnect failed {0}", ex.Message);
            }
        }

        public static async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                .WithRetainFlag(retainFlag)
                .Build());
        }

        public static async Task SubscribeAsync(string topic, int qos = 1)
        {
            await client.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                .Build());
        }
    }
}