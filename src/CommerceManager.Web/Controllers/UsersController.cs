using System.Net.Http.Json;
using CommerceManager.Web.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UsersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("api");

        var users = await client.GetFromJsonAsync<List<UserListItemViewModel>>(
            "api/users",
            cancellationToken) ?? [];

        var roles = await client.GetFromJsonAsync<List<RoleViewModel>>(
            "api/users/roles",
            cancellationToken) ?? [];

        return View(new UsersIndexViewModel
        {
            Users = users,
            Roles = roles
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        return View(new CreateUserViewModel
        {
            Roles = await GetRolesAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateUserViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = await GetRolesAsync(cancellationToken);
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("api");

        var response = await client.PostAsJsonAsync(
            "api/users",
            new
            {
                model.UserName,
                model.Email,
                model.FullName,
                model.Password,
                model.RoleId
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrorAsync(
                response,
                "Kullanıcı oluşturulamadı.");

            model.Password = string.Empty;
            model.Roles = await GetRolesAsync(cancellationToken);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Kullanıcı başarıyla oluşturuldu.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var users = await GetUsersAsync(cancellationToken);

        var user = users.SingleOrDefault(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        return View(new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            Roles = await GetRolesAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditUserViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = await GetRolesAsync(cancellationToken);
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("api");

        var response = await client.PutAsJsonAsync(
            $"api/users/{model.Id}",
            new
            {
                model.UserName,
                model.Email,
                model.FullName,
                model.RoleId,
                model.IsActive
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrorAsync(
                response,
                "Kullanıcı güncellenemedi.");

            model.Roles = await GetRolesAsync(cancellationToken);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Kullanıcı başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        CancellationToken cancellationToken)
    {
        var users = await GetUsersAsync(cancellationToken);

        var user = users.SingleOrDefault(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        return View(new ResetUserPasswordViewModel
        {
            UserId = user.Id,
            FullName = user.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetUserPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("api");

        var response = await client.PostAsJsonAsync(
            $"api/users/{model.UserId}/reset-password",
            new
            {
                model.NewPassword,
                model.ConfirmPassword
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrorAsync(
                response,
                "Şifre sıfırlanamadı.");

            model.NewPassword = string.Empty;
            model.ConfirmPassword = string.Empty;

            return View(model);
        }

        TempData["SuccessMessage"] =
            $"{model.FullName} kullanıcısının şifresi başarıyla değiştirildi.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<UserListItemViewModel>> GetUsersAsync(
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("api");

        return await client.GetFromJsonAsync<List<UserListItemViewModel>>(
            "api/users",
            cancellationToken) ?? [];
    }

    private async Task<List<RoleViewModel>> GetRolesAsync(
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("api");

        return await client.GetFromJsonAsync<List<RoleViewModel>>(
            "api/users/roles",
            cancellationToken) ?? [];
    }

    private async Task AddApiErrorAsync(
        HttpResponseMessage response,
        string fallbackMessage)
    {
        try
        {
            var error =
                await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

            ModelState.AddModelError(
                string.Empty,
                error?.Message ?? fallbackMessage);
        }
        catch
        {
            ModelState.AddModelError(
                string.Empty,
                fallbackMessage);
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}
