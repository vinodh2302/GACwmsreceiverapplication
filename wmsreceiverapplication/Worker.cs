using RabbitMQ.Client;
using System.Text;

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
                HostName = Environment.GetEnvironmentVariable("RABBITMQ__HOSTNAME") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: QueueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
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

                    var content = new StringContent(message, Encoding.UTF8, "application/json");
                    try
                    {
                        var response = await _httpClient.PostAsync("http://your-api:5000/api/messages", content, stoppingToken);
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
