using System.Security.Claims;
using CommerceManager.Api.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly UserProfileService _profileService;

    public ProfileController(UserProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMe(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var profile = await _profileService.GetAsync(
            userId,
            cancellationToken);

        return profile is null
            ? NotFound()
            : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _profileService.UpdateAsync(
            userId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _profileService.ChangePasswordAsync(
            userId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }
}
