using DelugeSync.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        private HttpProfileSetting httpProfile;
        private NetworkCredential httpCredentials = new NetworkCredential();
        private string localSaveLocation;
        private bool createSubDirectories;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            #region Rabbit
            _connectionFactory = new ConnectionFactory()
            {
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                HostName = _configuration["RabbitMQ:HostName"],
                VirtualHost = _configuration["RabbitMQ:VirtualHost"],
                Port = int.Parse(_configuration["RabbitMQ:Port"]),
                DispatchConsumersAsync = true,
            };
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_configuration["RabbitMQ:Queue"].ToString(),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.BasicQos(0, 1, false);
            #endregion

            localSaveLocation = _configuration["General:LocalSaveLocation"].ToString();
            createSubDirectories = _configuration.GetValue<bool>("General:CreateSubDirectories");
            if (!Directory.Exists(localSaveLocation)) Directory.CreateDirectory(localSaveLocation);

            #region Http Profile(s)
            if (_configuration["DownloadProfiles:HTTP:Enabled"].ToLower().ToString() == "true") {
                httpProfile = _configuration.GetSection("DownloadProfiles:HTTP").Get<HttpProfileSetting>();
                httpCredentials = new NetworkCredential(httpProfile.UserName, httpProfile.Password);
                DownloadService.IdleConnectionSeconds = httpProfile.ConnectionIdleTimeout;
                DownloadService.DownloadChunks = httpProfile.DownloadChunks;
                DownloadService.MaxConnections = httpProfile.MaxConnections;
                //map the file profiles to model
                var fileProfiles = _configuration.GetSection("DownloadProfiles:HTTP:FileProfiles").GetChildren();
                foreach (var section in fileProfiles)
                {
                    httpProfile.FileProfiles.Add(new FileProfileSetting { searchCriteria = section.Key, saveLocationRelative = section.Value });
                    //create the folders
                    var filePath = localSaveLocation + $"/{section.Value}";
                    if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                }
            }
            #endregion

            //initialise loggers
            DelugeMessage._logger = _logger;
            DownloadService._logger = _logger;

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                _logger.LogInformation("Listening for rabbit events");
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (sender, e) => await OnReceiveEventAsync(sender, e);
                _channel.BasicConsume(queue: "deluge-queue", autoAck: false, consumer: consumer);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + "\n" + ex.StackTrace);
            }
        }
        private async Task OnReceiveEventAsync(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var channel = ((AsyncEventingBasicConsumer)sender).Model;
                var body = e.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(body);
                _logger.LogInformation($"Received message:\n" +
                $"{JsonConvert.SerializeObject(messageString)}\n" +
                $"from queue at: {DateTime.Now.ToString()}");

                var message = JsonConvert.DeserializeObject<DelugeMessage>(messageString);
                bool foundProfile = false;
                //check what kind of message - this is going to be more sophisticated
                httpProfile.FileProfiles.ForEach(x => {
                    if (message.TorrentPath.ToLower().Contains(x.searchCriteria))
                    {
                        foundProfile = true;
                        this.StartDownload(message.GetUrl(x.searchCriteria, httpProfile.BaseUrl).ToString(), x, channel, e);
                    }
                });

                //need to reject the message from rabbit if it doesn't exist in the profiles
                if (foundProfile == false) channel.BasicReject(e.DeliveryTag, false);
                //if (message.TorrentPath.ToLower().Contains("sonarr"))
                //{
                //    this.StartDownload(message.GetUrl("sonarr", httpProfile.BaseUrl).ToString(), channel, e);
                //}
                //else if (message.TorrentPath.ToLower().Contains("radarr"))
                //{
                //    this.StartDownload(message.GetUrl("sonarr", httpProfile.BaseUrl).ToString(), channel, e);
                //}
                //else
                //{
                //    some other file, not sonarr/ radarr
                //    _logger.LogInformation("Message not found in profiles, discarding it");
                //    channel.BasicReject(e.DeliveryTag, false);
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + "\n" + ex.StackTrace);
                //throw ex;
            }

        }

        private void StartDownload(string url,FileProfileSetting fileProfile, IModel channel, BasicDeliverEventArgs eventArgs)
        {
            var filename = DelugeMessage.GetFilenameFromDownloadUrl(url, localSaveLocation, fileProfile.searchCriteria, createSubDirectories);
            var result = DownloadService.Download(fileUrl: url, destinationFolderPath: filename, numberOfParallelDownloads: httpProfile.DownloadChunks, credentials: httpCredentials);
            if (result == null)
            {
                _logger.LogError($"download has failed");
                channel.BasicReject(deliveryTag: eventArgs.DeliveryTag, true);
            } else
            {
                _logger.LogInformation($"Download completed in {result.TimeTaken.Seconds}s");
                _logger.LogInformation($"File Path: {result.FilePath}");
                _logger.LogInformation($"Parallel: {result.ParallelDownloads}");
                _logger.LogInformation($"Size: {result.Size} bytes");
                channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _connection.Close();
            _logger.LogInformation("RabbitMQ connection is closed.");
        }
    }
}
