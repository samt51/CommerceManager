using System.Security.Claims;
using CommerceManager.Api.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserManagementService _userManagementService;

    public UsersController(
        UserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _userManagementService.GetUsersAsync(
            cancellationToken);

        return Ok(users);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleResponse>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var roles = await _userManagementService.GetRolesAsync(
            cancellationToken);

        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.CreateAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _userManagementService.UpdateAsync(
            id,
            currentUserId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.ResetPasswordAsync(
            id,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }
}
