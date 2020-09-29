using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Server;
using MQTTnet;

namespace HeatApp.Services
{
    public class MqttServer : IHostedService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private IMqttServer mqttServer;

        public MqttServer(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            this.configuration = configuration;
            this.scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int configPort = configuration.GetSection("ServerPort").GetValue<int>("ServerPort");
            int mqttPort = configPort > 0 ? configPort : 1883;

            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(mqttPort);

            mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.StartAsync(optionsBuilder.Build());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            mqttServer.StopAsync();
            return Task.CompletedTask;
        }
    }

}
