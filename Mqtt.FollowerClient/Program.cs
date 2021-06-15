using System;
using System.Collections;
using System.IO;
using System.Linq;
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

namespace Mqtt.FollowerClient
{
  internal class Program
  {
    private const string clientId = "followingVehicle1";
    public static IConfigurationRoot configuration;


    private const int bitcount = 72 * 8;
    public static IManagedMqttClient client =
        new MqttFactory().CreateManagedMqttClient(new MqttNetLogger(clientId));

    private const string OUTGOING_TOPIC = "platooning/message";

    private static void sendBitArrayMessage(BitArray message)
    {
      // _ = PublishAsync(OUTGOING_TOPIC,
      //     Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
      var byteArray = HelperFunctions.BitArrayToByteArray(message);
      Console.WriteLine("Base64:" + Convert.ToBase64String(byteArray));
      var payload = HelperFunctions.GetPayload(byteArray);

      Console.WriteLine("Payload:TimeStamp:[" + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + "] " + byteArray.Length + "=>" + JsonConvert.SerializeObject(payload, Formatting.Indented));


      _ = PublishAsyncByteArray(OUTGOING_TOPIC, byteArray);
    }
    private static void Main(string[] args)
    {
      var builder = new ConfigurationBuilder()
     .SetBasePath(Path.Combine(AppContext.BaseDirectory))
     .AddJsonFile("appsettings.json", optional: true);
      configuration = builder.Build();


      _ = ConnectAsync();
      ConsoleKey key = ConsoleKey.Enter;
      do
      {
        while (!Console.KeyAvailable)
        {
          key = Console.ReadKey(true).Key;
          if (key == ConsoleKey.S)
          {
            _ = SubscribeAsync("platooning/" + clientId + "/#");
            Console.WriteLine("Client SubscribeAsync as  " + "platooning/" + clientId + "/#");
          }
          else if (key == ConsoleKey.D)
          {
            SendDissolve();
          }
          else if (key == ConsoleKey.M)
          {
            SendManuever();
          }
          //JOIN REQUEST
          else if (key == ConsoleKey.P)
          {
            var message = new BitArray(bitcount);

            //StatationId 0-31 , set to 2
            message.Set(30, true);

            //Manuever 324-327 set to 11
            message.Set(324, true);
            message.Set(325, false);
            message.Set(326, true);
            message.Set(327, true);

            sendBitArrayMessage(message);
          }
        }
      } while (key != ConsoleKey.Escape);
    }
    //public event EventHandler<MqttClientConnectedEventArgs> Connected;
    //public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
    //public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;

    private static void SendDissolve()
    {
      var message = new BitArray(bitcount);
      //StatationId 0-31 , set to 2
      message.Set(30, true);

      //Dissolve Status  344 -351
      message.Set(351, true);

      sendBitArrayMessage(message);
    }

    private static void SendManuever()
    {
      var message = new BitArray(bitcount);
      //StatationId 0-31 , set to 2
      message.Set(30, true);

      //Manuever 324-327 set to 6 
      message.Set(324, false);
      message.Set(325, true);
      message.Set(326, true);
      message.Set(327, false);

      sendBitArrayMessage(message);

    }
    private static byte[] GenerateMessage()
    {
      //0x111 
      return new byte[4];
    }
    private static async Task ConnectAsync()
    {
      string mqttUri = configuration["mqttServerIp"];
      //const string mqttUri = "localhost";
      var mqttUser = configuration["mqttUser"];
      var mqttPassword = configuration["mqttPassword"];
      var mqttPort = Convert.ToInt32(configuration["mqttPort"]);
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
          Console.WriteLine("TimeStamp:[" + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + "] Message Received Base64: " + Convert.ToBase64String(e.ApplicationMessage.Payload));

          var payload = HelperFunctions.GetPayload(e.ApplicationMessage.Payload);
          Console.WriteLine($"Topic: {topic}. Message Received: {JsonConvert.SerializeObject(payload, Formatting.Indented)}");
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

    public static async Task PublishAsyncByteArray(string topic, byte[] payload, bool retainFlag = true, int qos = 1)
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