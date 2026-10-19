/* =====================================================================
   Prueba técnica - Bonos de desempeño
   Script 2: tabla Bonus.
   Los montos (bruto, retención, neto) se calculan al registrar y se
   persisten: dependen de MonthlySalary y TaxRate, que son atributos
   mutables del empleado, y un bono registrado es un hecho histórico
   que no debe cambiar si el sueldo o la tasa cambian después.
   ===================================================================== */

USE BonusTest;
GO

CREATE TABLE dbo.Bonus (
    BonusId        INT IDENTITY(1,1) NOT NULL,
    DepartmentCode CHAR(5)           NOT NULL,
    EmployeeNumber INT               NOT NULL,
    Period         CHAR(7)           NOT NULL,  -- formato YYYY-MM; su orden lexicográfico coincide con el cronológico
    BonusTypeId    INT               NOT NULL,  -- 1 = monto fijo, 2 = porcentaje del sueldo
    BonusValue     DECIMAL(16,2)     NOT NULL,  -- el monto fijo o el porcentaje ingresado, según BonusTypeId
    ReasonCode     CHAR(6)           NOT NULL,
    GrossAmount    DECIMAL(16,2)     NOT NULL,
    TaxAmount      DECIMAL(16,2)     NOT NULL,
    NetAmount      DECIMAL(16,2)     NOT NULL,
    BonusStateCode CHAR(4)           NOT NULL,  -- 0201 Registrado, 0202 Pagado, 0203 Anulado
    IsDeleted      BIT               NOT NULL CONSTRAINT DF_Bonus_IsDeleted DEFAULT (0),
    CreatedBy      VARCHAR(50)       NOT NULL,  -- usuario de negocio (header UserCode), no el login de SQL
    CreatedAt      DATETIME2(0)      NOT NULL,
    ModifiedBy     VARCHAR(50)       NULL,      -- NULL hasta la primera modificación (anular, eliminar, pagar)
    ModifiedAt     DATETIME2(0)      NULL,

    CONSTRAINT PK_Bonus PRIMARY KEY (BonusId),

    CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
        REFERENCES dbo.Employee (DepartmentCode, EmployeeNumber),
    CONSTRAINT FK_Bonus_BonusType FOREIGN KEY (BonusTypeId)
        REFERENCES dbo.BonusType (BonusTypeId),
    CONSTRAINT FK_Bonus_BonusReason FOREIGN KEY (ReasonCode)
        REFERENCES dbo.BonusReason (ReasonCode),
    CONSTRAINT FK_Bonus_State FOREIGN KEY (BonusStateCode)
        REFERENCES dbo.State (StateCode),

    CONSTRAINT CK_Bonus_Period CHECK (
        Period LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]'
        AND SUBSTRING(Period, 6, 2) BETWEEN '01' AND '12'
    ),
    -- Invariante común a ambos tipos; los topes que dependen del tipo
    -- (<= sueldo, <= 100) son reglas de negocio y viven en Application.
    CONSTRAINT CK_Bonus_BonusValue CHECK (BonusValue > 0),
    -- La FK solo garantiza que el código exista; esto garantiza que sea un estado de bono (02xx).
    CONSTRAINT CK_Bonus_StateIsBonusState CHECK (BonusStateCode LIKE '02%')
);
GO

/* Regla: un empleado no puede tener más de un bono activo por periodo
   (activo = no anulado y no eliminado). La validación en Application da
   el mensaje amable; este índice único FILTRADO la garantiza de forma
   atómica ante requests concurrentes. El filtro es clave: un índice
   único normal impediría registrar un bono nuevo tras anular el
   anterior del mismo periodo, que es un flujo válido. */
CREATE UNIQUE NONCLUSTERED INDEX UX_Bonus_ActivePeriod
    ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period)
    WHERE BonusStateCode <> '0203' AND IsDeleted = 0;
GO

/* Soporta el listado paginado, que filtra por periodo y estado (y
   siempre sobre filas no eliminadas). Las búsquedas por empleado ya las
   cubre UX_Bonus_ActivePeriod. Con el volumen de prueba el optimizador
   escaneará igual; el índice está dimensionado para el volumen real del
   caso de uso (años de bonos de una planilla). */
CREATE NONCLUSTERED INDEX IX_Bonus_Listing
    ON dbo.Bonus (Period, BonusStateCode)
    WHERE IsDeleted = 0;
GO
