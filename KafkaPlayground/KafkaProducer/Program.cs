using Confluent.Kafka;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KafkaProducer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
             if (args.Length != 2)
            {
                Console.WriteLine("Usage: .. brokerList topicName");
                return;
            }

            string brokerList = args[0];
            string topicName = args[1];

            var config = new ProducerConfig { BootstrapServers = brokerList };

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine($"Producer {producer.Name} producing on topic {topicName}.");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine("To create a kafka message with UTF-8 encoded key and value:");
                Console.WriteLine("> key value<Enter>");
                Console.WriteLine("To create a kafka message with a null key and UTF-8 encoded value:");
                Console.WriteLine("> value<enter>");
                Console.WriteLine("Ctrl-C to quit.\n");

                var cancelled = false;
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cancelled = true;
                };

                while (!cancelled)
                {
                    Console.Write("> ");

                    string text;
                    try
                    {
                        text = Console.ReadLine();
                    }
                    catch (IOException)
                    {
                        // IO exception is thrown when ConsoleCancelEventArgs.Cancel == true.
                        break;
                    }
                    if (text == null)
                    {
                        // Console returned null before 
                        // the CancelKeyPress was treated
                        break;
                    }

                    string key = null;
                    string val = text;

                    // split line if both key and value specified.
                    int index = text.IndexOf(" ");
                    if (index != -1)
                    {
                        key = text.Substring(0, index);
                        val = text.Substring(index + 1);
                    }

                    try
                    {
                        // Note: Awaiting the asynchronous produce request below prevents flow of execution
                        // from proceeding until the acknowledgement from the broker is received (at the 
                        // expense of low throughput).
                        var deliveryReport = await producer.ProduceAsync(
                            topicName, new Message<string, string> { Key = key, Value = val });

                        Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset}");
                    }
                    catch (ProduceException<string, string> e)
                    {
                        Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
                    }
                }

                // Since we are producing synchronously, at this point there will be no messages
                // in-flight and no delivery reports waiting to be acknowledged, so there is no
                // need to call producer.Flush before disposing the producer.
            }
        }

        public static void Maini(string[] args)
        {
            var brokerList = "localhost:9092";
            var topicName = "TestTopic";

            var config = new ProducerConfig
            {
                BootstrapServers = brokerList,
                QueueBufferingMaxKbytes = 10000,
                QueueBufferingMaxMessages = 10000,
                LingerMs = 60
            };

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine($"Producer {producer.Name} producing on topic {topicName}.");
                Console.WriteLine("-----------------------------------------------------------------------");

                
                var i = 0;

                while (i < double.MaxValue)
                {
                    try
                    {
                        i++;
                        var val = $"Message {i}";
                        producer.BeginProduce(
                            topicName, new Message<string, string> { Key = null, Value = val }, ProcessDeliveryReport);

                        
                    }
                    catch (ProduceException<string, string> e)
                    {
                        Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
                    }
                }
            }
        }
        
        private static void ProcessDeliveryReport( DeliveryReport<string, string> report )
        {
            // If this message was successfully delivered then make a note of the offset value, if it failed, DES it
            if (!report.Error.IsError )
            {
                Console.Write($"Delivery Error {report.Error.Reason} \n " +
                              $"Offset {report.TopicPartitionOffset.Offset.ToString()} \n " +
                              $"Reason:  {report.Error.Reason}");
            }
        }
    }
    
}
