using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper.Contrib.Extensions;
using WorkerAcoes.Models;

namespace WorkerAcoes.Data
{
    public class AcoesRepository
    {
        private readonly IConfiguration _configuration;

        public AcoesRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Save(Acao acao)
        {
            var conexao = new SqlConnection(
                _configuration.GetConnectionString("BaseAcoes"));

            var historico = new HistoricoAcao()
            {
                Codigo = acao.Codigo,
                CodReferencia = $"{acao.Codigo}{DateTime.Now:yyyyMMddHHmmss}",
                DataReferencia = DateTime.Now,
                Valor = acao.Valor
            };
            conexao.Insert<HistoricoAcao>(historico);
        }
    }
}