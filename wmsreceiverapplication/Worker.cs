using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;
using WMSSystems.Models;

namespace wmsreceiverapplication
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IConnection _connection;
        private IModel _channel;

        private const string QueueName = "OrderProcessing";
        private readonly HttpClient _httpClient;



        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            //var factory = new ConnectionFactory()
            //{
            //    HostName = "localhost",
            //    Port = 5672,
            //    UserName = "guest",
            //    Password = "guest"
            //};
            var factory = new ConnectionFactory
            {
                Port = 5672,
                HostName = "rabbitmq",
                UserName =  "guest",
                Password = "guest"
            };

            IConnection connection = null;
            int retries = 10;
            while (retries > 0)
            {
                try
                {
                    connection = factory.CreateConnection();
                    Console.WriteLine("Connected to RabbitMQ!");
                    break;
                }
                catch (BrokerUnreachableException)
                {
                    retries--;
                    Console.WriteLine("RabbitMQ not ready, retrying...");
                    Thread.Sleep(5000);
                }
            }
            _logger.LogInformation("Came 12");
            if(connection != null)
            {
                _channel = connection.CreateModel();

                _channel.QueueDeclare(queue: QueueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
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
