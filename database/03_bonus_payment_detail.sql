/* =====================================================================
   Prueba técnica - Bonos de desempeño
   Script 3 (punto extra 1): detalle del pago del bono por proyecto.
   Guarda la foto de la distribución al momento de pagar: proyecto,
   horas consideradas y monto asignado. Si las horas del empleado
   cambian después, el pago registrado no se altera.
   ===================================================================== */

USE BonusTest;
GO

CREATE TABLE dbo.BonusPaymentDetail (
    BonusPaymentDetailId INT IDENTITY(1,1) NOT NULL,
    BonusId              INT           NOT NULL,
    ProjectName          VARCHAR(100)  NOT NULL,
    Hours                DECIMAL(8,2)  NOT NULL,  -- horas consideradas en la distribución
    Amount               DECIMAL(16,2) NOT NULL,  -- parte del bruto asignada al proyecto

    CONSTRAINT PK_BonusPaymentDetail PRIMARY KEY (BonusPaymentDetailId),
    CONSTRAINT FK_BonusPaymentDetail_Bonus FOREIGN KEY (BonusId)
        REFERENCES dbo.Bonus (BonusId),
    CONSTRAINT CK_BonusPaymentDetail_Amount CHECK (Amount >= 0)
);
GO

/* El detalle siempre se consulta por su bono. */
CREATE NONCLUSTERED INDEX IX_BonusPaymentDetail_Bonus
    ON dbo.BonusPaymentDetail (BonusId);
GO
