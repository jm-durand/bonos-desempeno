# Bonos de desempeño — Prueba técnica Full Stack

Módulo de bonos de desempeño para RRHH: se busca un empleado por área y número, se le registra un bono por periodo (monto fijo o porcentaje del sueldo) con un motivo, y el sistema calcula bruto, retención y neto. Incluye listado paginado desde el servidor, anulación, eliminación lógica y los dos puntos extra (pago con distribución por proyecto y pruebas unitarias).

**Stack:** SQL Server 2019+ · .NET 10 / ASP.NET Core Web API / EF Core · Angular 22 (standalone, signals).

## Cómo levantar el proyecto

### 1. Base de datos

Ejecutar los scripts de `/database` en orden sobre la instancia local:

| Script | Contenido |
|---|---|
| `01_schema_seed.sql` | Proyecto base: tablas existentes y datos semilla (no se modificó) |
| `02_bonus.sql` | Tabla `Bonus`, constraints e índices |
| `03_bonus_payment_detail.sql` | Tabla `BonusPaymentDetail` (punto extra 1) |

### 2. Backend

La connection string está en `backend/src/BonusTest.Api/appsettings.json` y apunta a `Server=localhost`; ajústala a tu instancia (p. ej. `localhost\SQLEXPRESS`).

```bash
cd backend
dotnet run --project src/BonusTest.Api    # API en http://localhost:5106 (Swagger en /swagger)
dotnet test                               # pruebas unitarias (punto extra 2)
```

### 3. Frontend

Requiere Node 22.22.3+ (exigencia del CLI de Angular 22).

```bash
cd frontend
npm ci
npx ng serve    # http://localhost:4200/bonuses
```

## Decisiones técnicas

### Base de datos

- **Los montos calculados (bruto, retención, neto) se persisten** en `Bonus`, no se recalculan al consultar. Dependen de `MonthlySalary` y `TaxRate`, que son atributos mutables del empleado: si mañana cambia el sueldo, el bono ya registrado no debe cambiar. Un bono es una foto al momento del registro, como la línea de una factura. El mismo criterio aplica a `BonusPaymentDetail`, que guarda proyecto, horas y monto usados en la distribución.
- **Regla "un bono activo por empleado y periodo" garantizada con un índice único filtrado** (`WHERE BonusStateCode <> '0203' AND IsDeleted = 0`). La validación en la capa Application da el mensaje amable (409), pero sola tiene una condición de carrera ante requests concurrentes; el índice la cierra de forma atómica en la BD. El filtro es clave: un índice único normal impediría re-registrar un bono tras anular el del mismo periodo, que es un flujo válido.
- **FK compuesta** `(DepartmentCode, EmployeeNumber)` hacia `Employee`, porque esa es la PK completa de la tabla referenciada.
- **`Period CHAR(7)`** (`YYYY-MM`) con CHECK de formato y mes 01–12: el dato de negocio es un mes y viaja idéntico entre front, API y BD sin conversiones; su orden lexicográfico coincide con el cronológico, así que comparar y ordenar funciona igual que con fechas.
- **Borrado lógico con `IsDeleted BIT`**, separado del estado: el estado (Registrado/Pagado/Anulado) es un concepto de negocio visible; el borrado es técnico — la fila deja de existir para el sistema pero queda en la tabla.
- Auditoría: `CreatedBy/At` obligatorios; `ModifiedBy/At` nulos hasta la primera modificación (distingue "nunca tocado" de "modificado").

### Backend

- Se respetaron las capas del proyecto base siguiendo el patrón del módulo de catálogos: `BonusService` (Application) concentra todas las reglas de negocio, `BonusRepository` (Infrastructure) el acceso a datos, y el controlador solo traduce HTTP ↔ servicio (lee el header `UserCode` y delega).
- **Paginación en la BD**: `Skip/Take` sobre `IQueryable` antes de materializar, que EF traduce a `OFFSET/FETCH`; el total sale de un `CountAsync` sobre la misma consulta filtrada.
- **El cálculo vive en `BonusCalculator`**, una clase estática pura (sin dependencias): es la pieza que cubren las pruebas unitarias. Redondeo con `Math.Round(v, 2, MidpointRounding.AwayFromZero)` — el default de .NET es banker's rounding y daría 0.125 → 0.12 en lugar del 0.13 que exige el enunciado.
- **Distribución del pago** (extra 1): los primeros n−1 proyectos se redondean a 2 decimales y el último recibe `total − asignado`, de modo que la suma cuadra con el bruto por construcción. Detalle + cambio de estado a Pagado viajan en un único `SaveChanges`, que EF Core envuelve en una transacción implícita: no puede quedar un bono pagado sin detalle ni al revés.
- Las transiciones de estado (pagar/anular/eliminar solo desde Registrado) se validan en un único helper, así la regla vive en un solo lugar.
- El `UserCode` llega como parámetro explícito al servicio: el header es un mecanismo de transporte, no un concepto de dominio; si mañana hubiera autenticación real, solo cambia de dónde sale ese string.

### Frontend

- **Sin librería de UI**: el proyecto base ya trae tokens y clases (`.card`, `.btn`); para una tabla, dos modales y una paginación, una librería agrega peso y fricción sin aportar. Los modales usan el **`<dialog>` nativo** (`showModal()`: foco atrapado, Esc y backdrop gratis) y la confirmación de acciones es un componente reutilizable sobre el mismo elemento.
- **La notificación global es un popover** (`popover="manual"`): un `<dialog>` modal vive en el *top layer* del navegador, por encima de cualquier `z-index`; la única forma de mostrar un aviso sobre él es entrar también al top layer. Los avisos de éxito se autocierran a los 4 s; los de error quedan hasta cerrarlos.
- Modal de registro con Reactive Forms: el validador del valor es dinámico según el tipo (tope 100 si es porcentaje, tope sueldo si es monto fijo) y la vista previa de bruto/retención/neto es un `computed` sobre `valueChanges` — replica el redondeo del backend solo como información; la autoridad del cálculo es la API.
- Al Buscar, los filtros se copian a una instantánea aplicada: cambiar de página respeta lo buscado aunque se editen los inputs sin volver a Buscar. Si se elimina la última fila de una página, el listado retrocede una página automáticamente.
- Los errores 409 los muestra el interceptor del proyecto base; los componentes no repiten ese manejo.

## Endpoints

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/api/v1/Bonus` | Listado paginado con filtros (área, empleado, periodo, estado) |
| GET | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Busca el empleado (solo activos) |
| POST | `/api/v1/Bonus` | Registra un bono |
| POST | `/api/v1/Bonus/{bonusId}/pay` | Paga y distribuye por proyecto (extra 1) |
| PUT | `/api/v1/Bonus/{bonusId}/cancel` | Anula (0201 → 0203) |
| DELETE | `/api/v1/Bonus/{bonusId}` | Eliminación lógica |

Todas las operaciones de escritura leen el usuario del header `UserCode`.

## Pendientes y límites de alcance

Todo lo obligatorio y los dos puntos extra están implementados. Lo que sigue no es funcionalidad faltante, sino lo que dejaría anotado para una siguiente iteración:

**Fuera del alcance de la prueba**

- Pruebas de integración de `BonusService`: las unitarias cubren el cálculo y la distribución, que es donde vive el riesgo de negocio; probar el servicio completo requiere dobles del repositorio o una BD de pruebas.
- Paginación con salto a página numerada: el listado cumple lo pedido (10 filas, total y navegación llamando a la API); saltar a una página específica solo aporta con volúmenes grandes.

**Diseño definido por el enunciado**

- La identidad del usuario llega en el header `UserCode`, tal como lo especifica la prueba. En producción esto sería un token de autenticación real; el diseño ya lo facilita: el header se traduce a un parámetro en el borde HTTP y la capa de negocio no sabe de dónde salió.
