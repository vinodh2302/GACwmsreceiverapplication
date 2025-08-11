using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WMSSystems.Models;

namespace wmsreceiverapplication
{
    public class CustomerProcessingWorker : BackgroundService
    {
        private readonly ILogger<CustomerProcessingWorker> _logger;
        private IConnection _connection;
        private IModel _channel;
        private const string QueueName = "CustomerProcessing";
        private readonly HttpClient _httpClient;
        public CustomerProcessingWorker(ILogger<CustomerProcessingWorker> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            int retries = 10;
            while (retries > 0)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _logger.LogInformation("Connected to RabbitMQ for CustomerProcessing queue");
                    break;
                }
                catch (BrokerUnreachableException)
                {
                    retries--;
                    _logger.LogWarning("RabbitMQ not ready, retrying in 5 seconds...");
                    Thread.Sleep(5000);
                }
            }

            if (_connection != null)
            {
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _channel.BasicGet(QueueName, autoAck: true);
                if (result != null)
                {
                    var message = Encoding.UTF8.GetString(result.Body.ToArray());
                    _logger.LogInformation($"Fetched: {message}");
                    var customer = JsonSerializer.Deserialize<Customer>(message);

                    if (customer == null)
                    {
                        _logger.LogWarning("Received message could not be deserialized to Product.");
                        continue;
                    }
                    var content = new StringContent(
                       JsonSerializer.Serialize(customer),
                       Encoding.UTF8,
                       "application/json"
                     );

                    //var content = new StringContent(message, Encoding.UTF8, "application/json");
                    try
                    {
                        var response = await _httpClient.PostAsync("http://wmsapi:80/api/Customer/customers", content, stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Message forwarded successfully.");
                        }
                        else
                        {
                            _logger.LogWarning($"API returned status code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to forward message to API");
                    }
                }

                await Task.Delay(1000, stoppingToken); // Poll every second
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
