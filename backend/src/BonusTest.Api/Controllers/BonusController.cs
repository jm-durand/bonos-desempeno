using Microsoft.AspNetCore.Mvc;
using BonusTest.Application.Bonuses;

namespace BonusTest.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BonusController(BonusService bonusService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] BonusFilterRequest filter, CancellationToken cancellationToken) =>
        Ok(await bonusService.GetPagedAsync(filter, cancellationToken));

    [HttpGet("employee/{departmentCode}/{employeeNumber:int}")]
    public async Task<IActionResult> SearchEmployee(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        Ok(await bonusService.SearchEmployeeAsync(departmentCode, employeeNumber, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterBonusRequest request,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        var bonusId = await bonusService.RegisterAsync(request, userCode, cancellationToken);
        return Ok(new { bonusId });
    }

    [HttpPut("{bonusId:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int bonusId,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        await bonusService.CancelAsync(bonusId, userCode, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{bonusId:int}")]
    public async Task<IActionResult> Delete(
        int bonusId,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        await bonusService.DeleteAsync(bonusId, userCode, cancellationToken);
        return NoContent();
    }
}
