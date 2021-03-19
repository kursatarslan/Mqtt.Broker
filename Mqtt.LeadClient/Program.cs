using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Mqtt.LeadClient
{
    internal class Program
    {
        private const string leadvehicle = "leadVehicle1";
        private const string platoonId = "platoon1";
        private const int bitcount = 64;
        public static IConfigurationRoot configuration; 


        public static IManagedMqttClient client =
            new MqttFactory().CreateManagedMqttClient(new MqttNetLogger("MyCustomId"));

        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("appsettings.json", optional: true);
            configuration = builder.Build();

            _ = ConnectAsync();
            do {
                while (!Console.KeyAvailable) {
                    if (Console.ReadKey(true).Key == ConsoleKey.S)
                    {
                        _ = SubscribeAsync(@$"platooning/{leadvehicle}/#");
                        Console.WriteLine("Client SubscribeAsync as  " + "platooning/" + leadvehicle + "/#");
                    }else if (Console.ReadKey(true).Key == ConsoleKey.P)
                    {
                        var message = new BitArray(bitcount);
                        message.Set(0, true);
                        message.Set(1, true);
                        message.Set(2, true);
                        //string message = HelperFunctions.RandomString(5,true);
                        _ = PublishAsync("platooning/message/" + leadvehicle + "/" + platoonId,
                            Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                        Console.WriteLine("Client Publish as  " + "platooning/message/" + leadvehicle + "/" + platoonId + "  payload => " +
                                          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                    }
                    else if (Console.ReadKey(true).Key == ConsoleKey.K)
                    {
                        var count = 0;
                        while (count++ < 1000)
                        {
                            var message = new BitArray(bitcount);
                            message.Set(0, true);
                            message.Set(1, true);
                            message.Set(2, false);
                            //string message = HelperFunctions.RandomString(5,true);
                            _ = PublishAsync("platooning/broadcast/" + platoonId,
                                Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                            Console.WriteLine("Client Publish as  " + "platooning/broadcast/"+ platoonId + "  payload => " +
                                              Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));

                        }

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
            string mqttUri = configuration["mqttServerIp"];
            var mqttUser = configuration["mqttUser"];;
            var mqttPassword = configuration["mqttPassword"];;
            var mqttPort = Convert.ToInt32(configuration["mqttPort"]);
            Console.WriteLine($"MQTT Server:{mqttUri} Username:{mqttUser} ClientID:{leadvehicle}");
            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(leadvehicle)
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
            client.UseApplicationMessageReceivedHandler(e =>
            {
                Console.WriteLine("Connected UseApplicationMessageReceivedHandler with MQTT Brokers." + e.ApplicationMessage);
            });

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
                        var stringpayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                        var bitArray = new BitArray(e.ApplicationMessage.Payload);
                        var payload = HelperFunctions.GetPayload(e.ApplicationMessage.Payload);
                        var py = HelperFunctions.ToBitString(new BitArray(e.ApplicationMessage.Payload), 0, bitcount);
                        Console.WriteLine($"Topic: {topic}. Message Received: {py}");
                        var followingVehicle = topic.Split('/');

                        var key = ConsoleKey.A;
                        if (key == ConsoleKey.A)
                        {
                            if (payload.Maneuver == 1 && !string.IsNullOrWhiteSpace(followingVehicle[2]))
                            {
                                payload.Maneuver = 2;
                                var message = new BitArray(bitcount);
                                message.Set(0, false);
                                message.Set(1, true);
                                message.Set(2, false);
                                var pubtopic = "platooning/" + followingVehicle[2] + "/" + leadvehicle + "/" +
                                               platoonId;
                                _ = PublishAsync(
                                    pubtopic,
                                    Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                                Console.WriteLine($"Publish Topic: {pubtopic}. Message Received: {Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message))}");
                            }
                        }else if (key == ConsoleKey.R)
                        {
                            if (payload.Maneuver == 1 && !string.IsNullOrWhiteSpace(followingVehicle[2]))
                            {
                                payload.Maneuver = 3;
                                var message = new BitArray(bitcount);
                                message.Set(0, false);
                                message.Set(1, true);
                                message.Set(2, true);
                                _ = PublishAsync(
                                    "platooning/" + followingVehicle[2] + "/" + leadvehicle + "/" + platoonId,
                                    Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                            }
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