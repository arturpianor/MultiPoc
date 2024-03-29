﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiPoc.Model;
using MultiPoc.RpcServer.RemoteBusiness;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MultiPoc.RpcServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

                channel.BasicQos(0, 1, false);

                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);
                Console.Write(" [x] Awaiting RPC requests...");

                consumer.Received += (model, ea) =>
                {
                    Console.WriteLine("[N] New Message received...");

                    string response = null;
                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body);
                        var calculatorRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<CalculatorRequest>(message);
                        //response = new CalculatorRequestBusiness().Calculate(calculatorRequest).ToString();
                        //calculatorRequest.Expression = (calculatorRequest.Value1.ToString() + calculatorRequest.Operation.ToString() + calculatorRequest.Value2.ToString());

                        DataTable dt = new DataTable();
                        response = dt.Compute(calculatorRequest.Expression, "").ToString();

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" [.] " + e.Message);
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                };
                Console.WriteLine(" Press [ENTER] to exit.");
                Console.ReadLine();
            }
        }
    }
}
