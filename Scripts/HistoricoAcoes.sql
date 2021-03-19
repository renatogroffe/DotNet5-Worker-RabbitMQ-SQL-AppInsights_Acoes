CREATE DATABASE BaseAcoes
GO

USE BaseAcoes
GO

CREATE TABLE dbo.HistoricoAcoes(
    Id INT IDENTITY(1,1) NOT NULL,
    CodReferencia VARCHAR(50) NOT NULL,
    Codigo VARCHAR(10) NOT NULL,
    DataReferencia DATETIME NOT NULL,
    Valor NUMERIC (10,4) NOT NULL,
    CONSTRAINT PK_HistoricoAcoes PRIMARY KEY (Id),
    CONSTRAINT UK_HistoricoAcoes_CodReferencia UNIQUE (CodReferencia)    
);