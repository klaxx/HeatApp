using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeatApp.Models;
using MQTTnet.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace HeatApp.Services
{
    public class MqttService : IHostedService
    {
        private IMqttClient client;
        private static System.Timers.Timer boilerTimer;
        private static System.Timers.Timer reconnectTimer;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private IMqttClientOptions mqttOptions;

        public MqttService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            this.configuration = configuration;
            this.scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
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

            mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("HomeServer")
                .WithTcpServer(mqttServer, mqttPort > 0 ? mqttPort : 1883)
                .WithCredentials(user, password)
                .Build();
            client = new MqttFactory().CreateMqttClient();
            client.ConnectAsync(mqttOptions);

            client.UseDisconnectedHandler(e =>
            {
                if (boilerUnit)
                {
                    boilerTimer.Stop();
                }
                reconnectTimer.Start();
            });

            client.UseConnectedHandler(e =>
            {
                //await client.SubscribeAsync("/heating/#");
                reconnectTimer.Stop();
                if (boilerUnit)
                {
                    boilerTimer.Start();
                }
            });
            //client.UseApplicationMessageReceivedHandler(e =>
            //{
            //    string topic = e.ApplicationMessage.Topic;
            //    string message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            //    //ParseMessage(topic, message, DateTime.Now);
            //    //Console.WriteLine($"+ Topic = {topic}");
            //    //Console.WriteLine($"+ Payload = {message}");
            //});
            return Task.CompletedTask;
        }

        private void ReconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            client.ConnectAsync(mqttOptions);
        }

        private void BoilerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var commandService = scope.ServiceProvider.GetRequiredService<CommandService>();
                this.client.PublishAsync("/heating/boiler/command", commandService.GetKettleAction());
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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

        //        //private async void SaveValveLog(int addr, StateMessage state)
        //        //{
        //        //    ValveLog vl = new ValveLog(addr, state);
        //        //    var last = db.ValveLog.SingleOrDefault(v => v.Addr == vl.Addr && v.Time == vl.Time);
        //        //    if (last == null)
        //        //    {
        //        //        db.ValveLog.Add(vl);
        //        //        await db.SaveChangesAsync();
        //        //    }
        //        //}

        //public void ValveMessage(Queue<string> queue, string message)
        //{
        //    string strId = queue.Dequeue();
        //    int addr = int.Parse(strId);
        //    string action = queue.Dequeue();
        //    Console.WriteLine(action);
        //    switch (action)
        //    {
        //        case "state":
        //            StateMessage state = JsonConvert.DeserializeObject<StateMessage>(message);
        //            //SaveValveLog(addr, state);
        //            Console.WriteLine(state.ToString());
        //            break;

        //    }
        //}
    }
}
