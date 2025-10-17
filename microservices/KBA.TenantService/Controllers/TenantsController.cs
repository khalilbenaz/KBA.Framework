using KBA.TenantService.DTOs;
using KBA.TenantService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBA.TenantService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantServiceLogic _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantServiceLogic tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TenantDto>>> GetAll()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(tenants);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantDto>> GetById(Guid id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();

        return Ok(tenant);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantDto dto)
    {
        try
        {
            var tenant = await _tenantService.CreateAsync(dto);
            _logger.LogInformation("Tenant créé: {TenantId}", tenant.Id);
            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantDto>> Update(Guid id, [FromBody] UpdateTenantDto dto)
    {
        try
        {
            var tenant = await _tenantService.UpdateAsync(id, dto);
            _logger.LogInformation("Tenant mis à jour: {TenantId}", id);
            return Ok(tenant);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _tenantService.DeleteAsync(id);
            _logger.LogInformation("Tenant supprimé: {TenantId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
