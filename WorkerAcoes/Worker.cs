using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WorkerAcoes.Data;
using WorkerAcoes.Models;
using WorkerAcoes.Validators;

namespace WorkerAcoes
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly AcoesRepository _repository;
        private readonly TelemetryConfiguration _telemetryConfig;
        private readonly string _queueName;
        private readonly int _intervaloMensagemWorkerAtivo;

        public Worker(ILogger<Worker> logger, IConfiguration configuration,
            AcoesRepository repository,
            TelemetryConfiguration telemetryConfig)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
            _telemetryConfig = telemetryConfig;
            _queueName = _configuration["RabbitMQ:Queue"];
            _intervaloMensagemWorkerAtivo =
                Convert.ToInt32(configuration["IntervaloMensagemWorkerAtivo"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Queue = {_queueName}");
            _logger.LogInformation("Aguardando mensagens...");

            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_configuration["RabbitMQ:Connection"])
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: _queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;
            channel.BasicConsume(queue: _queueName,
                autoAck: true,
                consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    $"Worker ativo em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(_intervaloMensagemWorkerAtivo, stoppingToken);
            }
        }

        private void Consumer_Received(
            object sender, BasicDeliverEventArgs e)
        {
            var dadosAcao = Encoding.UTF8.GetString(e.Body.ToArray());
            _logger.LogInformation(
                $"[Nova mensagem | {DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                dadosAcao);

            var inicio = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();

            watch.Stop();
            TelemetryClient client = new (_telemetryConfig);
            client.TrackDependency(
                "RabbitMQ", $"Consumer {_queueName}", dadosAcao, inicio, watch.Elapsed, true);

            ProcessarAcao(dadosAcao);
        }

        private void ProcessarAcao(string dados)
        {
            Acao acao;            
            try
            {
                acao = JsonSerializer.Deserialize<Acao>(dados,
                    new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch
            {
                acao = null;
            }

            var dadosValidos = acao is not null;
            if (dadosValidos)
            {
                var validationResult = new AcaoValidator().Validate(acao);

                dadosValidos = validationResult.IsValid;

                if (!validationResult.IsValid)
                    validationResult.Errors.ToList().ForEach(f =>
                        _logger.LogError(f.ErrorMessage));
            }
            
            if (dadosValidos)
            {
                _repository.Save(acao);
                _logger.LogInformation("Ação registrada com sucesso!");
            }
            else
            {
                _logger.LogError("Dados inválidos para a Ação");
            } 
        }
    }
}