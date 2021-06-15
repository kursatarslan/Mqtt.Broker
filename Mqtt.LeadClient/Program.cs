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
using Newtonsoft.Json;

namespace Mqtt.LeadClient
{
  internal class Program
  {
    private const string leadvehicle = "leadVehicle1";
    private const string platoonId = "platoon1";
    private const int bitcount = 1400 * 8;
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
      do
      {
        while (!Console.KeyAvailable)
        {
          //SUBSCRIBE
          if (Console.ReadKey(true).Key == ConsoleKey.S)
          {
            _ = SubscribeAsync(@$"platooning/{leadvehicle}/#");
            Console.WriteLine("Client SubscribeAsync as  " + "platooning/" + leadvehicle + "/#");
          }
          //CREATE PLATOON
          else if (Console.ReadKey(true).Key == ConsoleKey.P)
          {
            var message = new BitArray(bitcount);
            //StationId 0-31

            message.Set(31, true);

            //MyPlatoonId 288-319
            message.Set(319, true);

            message.Set(320, true);
            message.Set(321, true);
            message.Set(322, true);

            Console.WriteLine("StationId: " + HelperFunctions.ToBitString(message, 0, 32));
            Console.WriteLine("MyPlatoonId: " + HelperFunctions.ToBitString(message, 288, 320));
            Console.WriteLine("Manuever: " + HelperFunctions.ToBitString(message, 320, 328));
            Console.WriteLine("DissolveStatus: " + HelperFunctions.ToBitString(message, 344, 352));
            //string message = HelperFunctions.RandomString(5,true);
            _ = PublishAsync("platooning/message",
                Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));

          }
          else if (Console.ReadKey(true).Key == ConsoleKey.D)
          {
            SendDissolve();
          }
          else if (Console.ReadKey(true).Key == ConsoleKey.M)
          {
            SendManuever();
          }
          else if (Console.ReadKey(true).Key == ConsoleKey.K)
          {
            var count = 0;
            while (count++ < 1000)
            {
              SendManuever();
            }

          }
        }
      } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
    }
    //public event EventHandler<MqttClientConnectedEventArgs> Connected;
    //public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
    //public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;
    private static void SendDissolve()
    {
      var message = new BitArray(bitcount);
      //StationId 0-31
      message.Set(31, true);

      //MyPlatoonId 288-319
      message.Set(319, true);

      //Dissolve Status
      message.Set(351, true);


      Console.WriteLine("StationId: " + HelperFunctions.ToBitString(message, 0, 32));
      Console.WriteLine("MyPlatoonId: " + HelperFunctions.ToBitString(message, 288, 320));
      Console.WriteLine("Manuever: " + HelperFunctions.ToBitString(message, 320, 328));
      Console.WriteLine("DissolveStatus: " + HelperFunctions.ToBitString(message, 344, 352));
      //string message = HelperFunctions.RandomString(5,true);
      _ = PublishAsync("platooning/message",
          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
      Console.WriteLine("TimeStamp:[" + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + "]  SendDissolve Client Publish as  " + "platooning/message");

      // Console.WriteLine("Client Publish as  " + "platooning/message" + "  payload => " +
      //                   Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
    }

    private static void SendManuever()
    {
      var message = new BitArray(bitcount);
      //StationId 0-31
      message.Set(31, true);

      //MyPlatoonId 288-319
      message.Set(319, true);

      //Muanuever 320-322
      message.Set(320, true);
      message.Set(321, true);
      message.Set(322, false);
      //string message = HelperFunctions.RandomString(5,true);

      Console.WriteLine("StationId: " + HelperFunctions.ToBitString(message, 0, 32));
      Console.WriteLine("MyPlatoonId: " + HelperFunctions.ToBitString(message, 288, 320));
      Console.WriteLine("Manuever: " + HelperFunctions.ToBitString(message, 320, 328));
      Console.WriteLine("DissolveStatus: " + HelperFunctions.ToBitString(message, 344, 352));

      _ = PublishAsync("platooning/message",
          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
      Console.WriteLine("TimeStamp:[" + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + "] SendManuever Client Publish as  " + "platooning/message");

      // Console.WriteLine("Client Publish as  " + "platooning/message" + "  payload => " +
      //                   Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
    }

    private static byte[] GenerateMessage()
    {
      //0x111 
      return new byte[4];
    }



    private static async Task ConnectAsync()
    {
      //const string mqttUri = "mqttbroker.westeurope.azurecontainer.io";
      string mqttUri = configuration["mqttServerIp"];
      var mqttUser = configuration["mqttUser"]; ;
      var mqttPassword = configuration["mqttPassword"]; ;
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

          Console.WriteLine("TimeStamp:[" + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + "] Message Received");
          var topic = e.ApplicationMessage.Topic;

          if (!string.IsNullOrWhiteSpace(topic))
          {
            var stringpayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            var bitArray = new BitArray(e.ApplicationMessage.Payload);
            var payload = HelperFunctions.GetPayload(e.ApplicationMessage.Payload);
            //  var py = HelperFunctions.ToBitString(new BitArray(e.ApplicationMessage.Payload), 0, bitcount);

            // Console.WriteLine("Received StationId: " + HelperFunctions.ToBitString(bitArray, 0, 32));
            // Console.WriteLine("Received MyPlatoonId: " + HelperFunctions.ToBitString(bitArray, 288, 320));
            // Console.WriteLine("Received Manuever: " + HelperFunctions.ToBitString(bitArray, 320, 328));
            // Console.WriteLine("Received DissolveStatus: " + HelperFunctions.ToBitString(bitArray, 344, 352));
            Console.WriteLine($"Topic: {topic}. Message Received: {JsonConvert.SerializeObject(payload, Formatting.Indented)}");
            //JOIN ACCEPTED
            var key = ConsoleKey.A;
            if (key == ConsoleKey.A)
            {
              Console.WriteLine("Manuever:" + payload.Maneuver);
              if (payload.Maneuver == 1)
              {
                payload.Maneuver = 2;
                var message = new BitArray(bitcount);
                //StationId 0-31
                message.Set(31, true);
                //MyPlatoonId 288-319
                message.Set(319, true);

                //Muanuever 320-322
                message.Set(320, false);
                message.Set(321, true);
                message.Set(322, false);


                Console.WriteLine("StationId: " + HelperFunctions.ToBitString(message, 0, 32));
                Console.WriteLine("MyPlatoonId: " + HelperFunctions.ToBitString(message, 288, 320));
                Console.WriteLine("Manuever: " + HelperFunctions.ToBitString(message, 320, 328));
                Console.WriteLine("DissolveStatus: " + HelperFunctions.ToBitString(message, 344, 352));
                Console.WriteLine("Message Sent: " + Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));

                var pubtopic = "platooning/message";
                _ = PublishAsync(
                          pubtopic,
                          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
              }
            }
            //JOIN REJECTED
            else if (key == ConsoleKey.R)
            {
              if (payload.Maneuver == 1)
              {
                payload.Maneuver = 3;
                var message = new BitArray(bitcount);
                //StationId 0-31
                message.Set(31, true);

                //MyPlatoonId 288-319
                message.Set(319, true);

                //Muanuever 320-322
                message.Set(320, false);
                message.Set(321, true);
                message.Set(322, true);


                Console.WriteLine("StationId: " + HelperFunctions.ToBitString(message, 0, 32));
                Console.WriteLine("MyPlatoonId: " + HelperFunctions.ToBitString(message, 288, 320));
                Console.WriteLine("Manuever: " + HelperFunctions.ToBitString(message, 320, 328));
                Console.WriteLine("DissolveStatus: " + HelperFunctions.ToBitString(message, 344, 352));

                _ = PublishAsync(
                          "platooning/message",
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
          .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
          .WithRetainFlag(retainFlag)
          .Build());
    }

    public static async Task SubscribeAsync(string topic, int qos = 1)
    {
      await client.SubscribeAsync(new MqttTopicFilterBuilder()
          .WithTopic(topic)
          .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
          .Build());
    }
  }
}