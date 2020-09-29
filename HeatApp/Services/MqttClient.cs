using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Options;
using Microsoft.Extensions.Configuration;
using MQTTnet.Client.Subscribing;
using Microsoft.AspNetCore.Mvc;
using HeatApp.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HeatApp.Services
{
    public class MqttClient : IHostedService
    {
        private IMqttClient client;
        private static System.Timers.Timer boilerTimer;
        private static System.Timers.Timer reconnectTimer;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private IMqttClientOptions mqttOptions;

        public MqttClient(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            this.configuration = configuration;
            this.scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            bool boilerUnit = this.configuration.GetSection("BoilerUnit").GetValue<bool>("UseBoiler");
            if (boilerUnit)
            {
                boilerTimer = new System.Timers.Timer(60000);
                boilerTimer.Elapsed += BoilerTimer_Elapsed;
                boilerTimer.AutoReset = true;
            }

            reconnectTimer = new System.Timers.Timer(30000);
            reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            reconnectTimer.AutoReset = true;


            string mqttServer = configuration.GetSection("MqttClient").GetValue<string>("ServerAddress");
            int mqttPort = configuration.GetSection("MqttClient").GetValue<int>("ServerPort");
            string mqttClientId = configuration.GetSection("MqttClient").GetValue<string>("ClientId");
            string user = configuration.GetSection("MqttClient").GetValue<string>("User");
            string password = configuration.GetSection("MqttClient").GetValue<string>("Password");
            MqttClientOptionsBuilder mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(mqttClientId)
                .WithTcpServer(mqttServer, mqttPort > 0 ? mqttPort : 1883);
            if (!string.IsNullOrEmpty(user))
            {
                mqttClientOptionsBuilder.WithCredentials(user, password);
            }
            mqttOptions = mqttClientOptionsBuilder.Build();
            //mqttOptions = new MqttClientOptionsBuilder()
            //    .WithClientId(mqttClientId)
            //    .WithTcpServer(mqttServer, mqttPort > 0 ? mqttPort : 1883)
            //    .WithCredentials(user, password)
            //    .Build();
            client = new MqttFactory().CreateMqttClient();
            client.UseDisconnectedHandler(e =>
            {
                if (boilerUnit)
                {
                    boilerTimer.Stop();
                }
                reconnectTimer.Start();
            });

            client.UseConnectedHandler(async e =>
            {
                await SubscribeTopics();
                reconnectTimer.Stop();
                if (boilerUnit)
                {
                    boilerTimer.Start();
                }
            });
            await client.ConnectAsync(mqttOptions);
        }

        private async Task SubscribeTopics()
        {
            MqttClientSubscribeOptions options = new MqttClientSubscribeOptions();
            options.TopicFilters = new System.Collections.Generic.List<TopicFilter> { new TopicFilter() { Topic = GetValvesTopic() + "/+/state" } };
            await client.SubscribeAsync(options);
            client.UseApplicationMessageReceivedHandler(async e => { await HandleMessageReceived(e.ApplicationMessage); });
        }

        private void ReconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            client.ConnectAsync(mqttOptions);
        }

        private async Task HandleMessageReceived(MqttApplicationMessage applicationMessage)
        {
            await ParseMessage(applicationMessage.Topic, applicationMessage.ConvertPayloadToString());
        }

        private void BoilerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var commandService = scope.ServiceProvider.GetRequiredService<CommandService>();
                this.client.PublishAsync(GetBoilerTopic() + "/command", commandService.GetKettleAction());
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private string GetValvesTopic()
        {
            string topic = configuration.GetSection("MqttClient").GetValue<string>("ValvesTopic");
            if (topic == null)
            {
                return "/hr20";
            }
            return topic.EndsWith("/") ? topic.TrimEnd('/') : topic;
        }

        private string GetBoilerTopic()
        {
            string topic = configuration.GetSection("BoilerUnit").GetValue<string>("BoilerUnit");
            if (string.IsNullOrEmpty(topic))
            {
                return "/heating/boiler";
            }
            return topic.EndsWith("/") ? topic.TrimEnd('/') : topic;
        }

        public void SendValveMessage(int addr, string action, string payload)
        {
            string topic = GetValvesTopic() + "/set/" + addr.ToString() + "/" + action;
            client.PublishAsync(topic, payload);
            MqttApplicationMessage message = new MqttApplicationMessage();

        }

        private async Task ParseMessage(string topic, string message)
        {
            if (topic.StartsWith(GetValvesTopic()))
            {
                topic = topic.Remove(0, GetValvesTopic().Length);
                await ValveMessageAsync(topic, message);
            }

            //topic = topic.TrimStart('/');
            //string[] parsedTopic = topic.Split("/");
            //Queue<string> queue = new Queue<string>(parsedTopic);
            //string subscription = queue.Dequeue();
            //switch (subscription)
            //{
            //    case Getval:
            //        ValveMessage(queue, message);
            //        break;
            //    default:
            //        break;
            //}


        }

        //public void ParseMessage(string topic, string message, DateTime time)
        //{
        //    if (topic.StartsWith("/"))
        //    {
        //        topic = topic.Substring(1);
        //    }
        //    string[] parsedTopic = topic.Split("/");
        //    Queue<string> queue = new Queue<string>(parsedTopic);
        //    string subscription = queue.Dequeue();

        //    switch (subscription)
        //    {
        //        case "heating":
        //            ValveMessage(queue, message);
        //            break;
        //        default:
        //            Console.WriteLine("Unknown subscription type : " + subscription);
        //            break;
        //    }
        //}



        private async Task ValveMessageAsync(string topic, string message)
        {
            topic = topic.TrimStart('/');
            string[] parsedTopic = topic.Split("/");
            Queue<string> queue = new Queue<string>(parsedTopic);

            string strId = queue.Dequeue();
            int addr = int.Parse(strId);
            string action = queue.Dequeue();
            switch (action)
            {
                case "state":
                    StateMessage state = JsonConvert.DeserializeObject<StateMessage>(message);
                    await SaveValveLogAsync(addr, state);
                    break;

            }
        }

        private async Task SaveValveLogAsync(int addr, StateMessage state)
        {
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                HeatAppContext db = scope.ServiceProvider.GetRequiredService<HeatAppContext>();
                ValveLog vl = new ValveLog(addr, state);
                var last = db.ValveLog.SingleOrDefault(v => v.Addr == vl.Addr && v.Time == vl.Time);
                if (last == null)
                {
                    db.ValveLog.Add(vl);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
