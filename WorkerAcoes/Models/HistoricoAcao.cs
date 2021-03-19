using System;
using Dapper.Contrib.Extensions;

namespace WorkerAcoes.Models
{
    [Table("HistoricoAcoes")]
    public class HistoricoAcao
    {
        public string CodReferencia { get; set; }
        public string Codigo { get; set; }
        public DateTime? DataReferencia { get; set; }
        public double? Valor { get; set; }
    }
}