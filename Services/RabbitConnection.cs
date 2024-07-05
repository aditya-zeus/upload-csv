using System.Runtime.CompilerServices;
using System.Text;
using api.Services;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Task1.Services
{
    public class RabbitConnection : IRabbitConnection
    {
        private IConnection? _connection;
        private readonly object _lock = new object();
        private string? queue_name;
        private readonly FileOperationStorage fos = new FileOperationStorage();

        public RabbitConnection()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            // _connection = factory.CreateConnection();
            RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
                _connection = factory.CreateConnection();
                using var channel = CreateChannel();
                queue_name = channel.QueueDeclare().QueueName;
                channel.Close();
                await Task.CompletedTask;
            });
        }

        public RabbitConnection(string _queue_name)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            queue_name = _queue_name;

            RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
                using var channel = CreateChannel();
                channel.QueueDeclare(queue: queue_name,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
                
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: true);
                channel.Close();
                await Task.CompletedTask;
            });
        }

        public IModel CreateChannel()
        {
            lock (_lock)
            {
                return _connection!.CreateModel();
            }
        }

        public void Dispose()
        {
            _connection!.Dispose(); // Release resources
        }

        public void BasicPublish(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
                using var channel = CreateChannel();
                channel.BasicPublish(exchange: "",
                        routingKey: queue_name,
                        basicProperties: null,
                        body: body);
                channel.Dispose();
                await Task.CompletedTask;
            });
        }

        public void BasicSubscribe()
        {
            RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
                using (var channel = CreateChannel())
                {
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine($" [x] Received {message}");
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    };
                    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                    channel.BasicConsume(queue: queue_name,
                                        autoAck: false,
                                        consumer: consumer);

                    Console.WriteLine("[*] Enter any key to exit BasicSubscribe...");
                    Console.ReadKey();
                    channel.Dispose();
                    await Task.CompletedTask;
                }
            });
            Console.WriteLine("[-] Subscriber exited successfully!");
        }

        public void SaveToDb() {
            RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () => {
                using (var channel = CreateChannel())
                {
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        int res = await fos.SaveToDb(message);
                        if(res == 0) {
                            Console.WriteLine("Republishing message...");
                            BasicPublish(message);
                        }
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    };
                    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                    channel.BasicConsume(queue: queue_name,
                                        autoAck: false,
                                        consumer: consumer);

                    Console.WriteLine("[*] Enter any key to exit SaveToDb...");
                    Console.ReadKey();
                    channel.Dispose();
                    await Task.CompletedTask;
                }
            });
            Console.WriteLine("[-] Subscriber exited successfully!");
        }
    }
}