using System.Net;
using System.Net.Http.Json;
using CommerceManager.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProfileController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("api");

        var response = await client.GetAsync(
            "api/profile/me",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] =
                "Profil bilgileri alınırken bir hata oluştu.";

            return RedirectToAction("Index", "Home");
        }

        var model = await response.Content
            .ReadFromJsonAsync<ProfileViewModel>(
                cancellationToken: cancellationToken);

        if (model is null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ProfileViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("api");

        var response = await client.PutAsJsonAsync(
            "api/profile/me",
            new
            {
                model.FullName,
                model.Email
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>(
                    cancellationToken: cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                error?.Message ?? "Profil güncellenemedi.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Profil bilgileriniz başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("api");

        var response = await client.PostAsJsonAsync(
            "api/profile/change-password",
            new
            {
                model.CurrentPassword,
                model.NewPassword,
                model.ConfirmPassword
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>(
                    cancellationToken: cancellationToken);

            ModelState.AddModelError(
                string.Empty,
                error?.Message ?? "Şifre değiştirilemedi.");

            model.CurrentPassword = string.Empty;
            model.NewPassword = string.Empty;
            model.ConfirmPassword = string.Empty;

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Şifreniz başarıyla değiştirildi.";

        return RedirectToAction(nameof(ChangePassword));
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}
