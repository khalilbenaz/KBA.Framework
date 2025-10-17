using KBA.Framework.Domain.Common;
using KBA.PermissionService.DTOs;
using KBA.PermissionService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBA.PermissionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionServiceLogic _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionServiceLogic permissionService,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtenir toutes les permissions (hiérarchiques)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionDto>>> GetAll()
    {
        var permissions = await _permissionService.GetAllAsync();
        return Ok(permissions);
    }

    /// <summary>
    /// Rechercher des permissions avec pagination
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PermissionDto>>> Search([FromQuery] PermissionSearchParams searchParams)
    {
        var result = await _permissionService.SearchAsync(searchParams);
        return Ok(result);
    }

    /// <summary>
    /// Obtenir une permission par ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionDto>> GetById(Guid id)
    {
        var permission = await _permissionService.GetByIdAsync(id);
        if (permission == null)
            return NotFound();

        return Ok(permission);
    }

    /// <summary>
    /// Obtenir une permission par nom
    /// </summary>
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionDto>> GetByName(string name)
    {
        var permission = await _permissionService.GetByNameAsync(name);
        if (permission == null)
            return NotFound();

        return Ok(permission);
    }

    /// <summary>
    /// Obtenir les permissions d'un groupe
    /// </summary>
    [HttpGet("group/{groupName}")]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionDto>>> GetByGroup(string groupName)
    {
        var permissions = await _permissionService.GetByGroupAsync(groupName);
        return Ok(permissions);
    }

    /// <summary>
    /// Créer une nouvelle permission (admin seulement)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionDto>> Create([FromBody] CreatePermissionDto dto)
    {
        try
        {
            var permission = await _permissionService.CreateAsync(dto);
            _logger.LogInformation("Permission created: {PermissionName}", permission.Name);
            return CreatedAtAction(nameof(GetById), new { id = permission.Id }, permission);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Supprimer une permission (admin seulement)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _permissionService.DeleteAsync(id);
            _logger.LogInformation("Permission deleted: {PermissionId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Vérifier si une permission est accordée à un utilisateur
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(PermissionCheckResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionCheckResult>> CheckPermission([FromBody] CheckPermissionDto dto)
    {
        var result = await _permissionService.CheckPermissionAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Accorder une permission (admin seulement)
    /// </summary>
    [HttpPost("grant")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> GrantPermission([FromBody] GrantPermissionDto dto)
    {
        try
        {
            var result = await _permissionService.GrantPermissionAsync(dto);
            _logger.LogInformation(
                "Permission granted: {Permission} to {Provider}:{Key}",
                dto.PermissionName,
                dto.ProviderName,
                dto.ProviderKey
            );
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Révoquer une permission (admin seulement)
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> RevokePermission([FromBody] RevokePermissionDto dto)
    {
        var result = await _permissionService.RevokePermissionAsync(dto);
        _logger.LogInformation(
            "Permission revoked: {Permission} from {Provider}:{Key}",
            dto.PermissionName,
            dto.ProviderName,
            dto.ProviderKey
        );
        return Ok(result);
    }

    /// <summary>
    /// Obtenir toutes les permissions d'un utilisateur
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<PermissionGrantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionGrantDto>>> GetUserPermissions(
        Guid userId,
        [FromQuery] Guid? tenantId = null)
    {
        var permissions = await _permissionService.GetUserPermissionsAsync(userId, tenantId);
        return Ok(permissions);
    }

    /// <summary>
    /// Obtenir toutes les permissions d'un rôle
    /// </summary>
    [HttpGet("role/{roleId}")]
    [ProducesResponseType(typeof(List<PermissionGrantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionGrantDto>>> GetRolePermissions(
        Guid roleId,
        [FromQuery] Guid? tenantId = null)
    {
        var permissions = await _permissionService.GetRolePermissionsAsync(roleId, tenantId);
        return Ok(permissions);
    }
}
