using DelugeSync.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DelugeSync
{
    internal class Service : BackgroundService
    {
        private readonly ILogger<Service>_logger;
        private ConnectionFactory _factory;
        private readonly HttpProfileSetting _httpProfile;
        private readonly GeneralSettings _generalSettings;
        private Worker _worker;

        private NetworkCredential httpCredentials = new NetworkCredential();

        private IConnection _connection;
        private IModel _channel;

        public Service(ILogger<Service> logger, ConnectionFactory factory, HttpProfileSetting httpProfile, GeneralSettings generalSettings, Worker worker)
        {
            _logger = logger;
            _factory = factory;
            _httpProfile = httpProfile;
            _generalSettings = generalSettings;
            _worker = worker;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            System.Threading.Thread.Sleep(_generalSettings.StartUpDelay * 1000);
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            var rabbitArgs = new Dictionary<string, object>();
            _channel.QueueDeclare(
                queue: _generalSettings.RabbitQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: rabbitArgs);
            _channel.BasicQos(0, 1, false);

            if (!Directory.Exists(_generalSettings.LocalSaveLocation)) Directory.CreateDirectory(_generalSettings.LocalSaveLocation);
            if (!Directory.Exists(_generalSettings.TempSaveLocation)) Directory.CreateDirectory(_generalSettings.TempSaveLocation);

            ConfigureDownloadService();
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Listening for rabbit events");
                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (sender, e) => await _worker.OnReceiveEventAsync(sender, e);
                    _channel.BasicConsume(queue: "deluge-queue", autoAck: false, consumer: consumer);
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Closing RabbitMQ connection.");
            _connection.Close();
            return base.StopAsync(cancellationToken);
        }

        public Task ConfigureDownloadService()
        {
            httpCredentials = new NetworkCredential(_httpProfile.UserName, _httpProfile.Password);
            DownloadService.IdleConnectionSeconds = _httpProfile.ConnectionIdleTimeout;
            DownloadService.DownloadChunks = _httpProfile.DownloadChunks;
            DownloadService.MaxConnections = _httpProfile.MaxConnections;
            DownloadService.NagleAlgorithm = _httpProfile.NagleAlgorithm;
            DownloadService.BetaOptions = _httpProfile.BetaOptions;

            var fileProfiles = _configuration.GetSection("DownloadProfiles:HTTP:FileProfiles").GetChildren();
            foreach (var section in fileProfiles)
            {
                //create the folders
                var filePath = _generalSettings.LocalSaveLocation + $"/{section.Value}";
                var tempPath = _generalSettings.TempSaveLocation + $"/{section.Value}";
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
            }

            DelugeMessage._logger = _logger;
            DownloadService._logger = _logger;
            return Task.CompletedTask;
        }
    }
}
