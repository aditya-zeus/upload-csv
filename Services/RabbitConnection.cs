using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Task1.Services
{
    public class RabbitConnection : IRabbitConnection
    {
        private readonly IConnection _connection;
        private readonly object _lock = new object();
        private readonly string queue_name;
        private readonly FileOperationStorage fos = new FileOperationStorage();

        public RabbitConnection()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();

            using var channel = CreateChannel();
            queue_name = channel.QueueDeclare().QueueName;
            channel.Close();
        }

        public RabbitConnection(string _queue_name)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            queue_name = _queue_name;

            using var channel = CreateChannel();
            channel.QueueDeclare(queue: queue_name,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            channel.Close();
        }

        public IModel CreateChannel()
        {
            lock (_lock)
            {
                return _connection.CreateModel();
            }
        }

        public void Dispose()
        {
            _connection.Dispose(); // Release resources
        }

        public void BasicPublish(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            using var channel = CreateChannel();
            channel.BasicPublish(exchange: "",
                     routingKey: queue_name,
                     basicProperties: null,
                     body: body);
        }

        public void BasicSubscribe()
        {
            using (var channel = CreateChannel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($" [x] Received {message}");
                    Thread.Sleep(10);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(queue: queue_name,
                                     autoAck: false,
                                     consumer: consumer);

                Console.WriteLine("[*] Enter any key to exit BasicSubscribe...");
                Console.ReadKey();
            }
            Console.WriteLine("[-] Subscriber exited successfully!");
        }

        public void SaveToDb() {
            using (var channel = CreateChannel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    fos.SaveToDb(message);
                    Thread.Sleep(100);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(queue: queue_name,
                                     autoAck: false,
                                     consumer: consumer);

                Console.WriteLine("[*] Enter any key to exit SaveToDb...");
                Console.ReadKey();
            }
            Console.WriteLine("[-] Subscriber exited successfully!");
        }
    }
}