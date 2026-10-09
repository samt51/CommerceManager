using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class CurrentAccountsController(IHttpClientFactory httpClientFactory)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("api");
        var url = "api/current-accounts";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        try
        {
            var result = await client.GetFromJsonAsync<CurrentAccountPage>(
                url,
                cancellationToken);

            ViewBag.Search = search;
            return View(result ?? new CurrentAccountPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Cariler alınırken API bağlantısı kurulamadı.";
            ViewBag.Search = search;
            return View(new CurrentAccountPage());
        }
    }

    [HttpGet]
    public IActionResult Create() =>
        RedirectToAction(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCurrentAccountViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["FormError"] = "Cari adı ve telefon gerekli.";
            TempData["OpenModal"] = "cari";
            return RedirectToAction(nameof(Index));
        }

        var client = httpClientFactory.CreateClient("api");

        try
        {
            var response = await client.PostAsJsonAsync(
                "api/current-accounts",
                new
                {
                    model.Name,
                    model.Kind,
                    model.Phone
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                TempData["FormError"] = "Cari kaydedilemedi.";
                TempData["OpenModal"] = "cari";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (HttpRequestException)
        {
            TempData["FormError"] = "Cari kaydedilirken API bağlantısı kurulamadı.";
            TempData["OpenModal"] = "cari";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Cari eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("api");

        try
        {
            var account = await client.GetFromJsonAsync<CurrentAccountDetails>(
                $"api/current-accounts/{id}",
                cancellationToken);

            if (account is null)
                return NotFound();

            ViewBag.Entry = new AddCurrentAccountEntryViewModel
            {
                CurrentAccountId = id
            };

            return View(account);
        }
        catch (HttpRequestException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEntry(
        AddCurrentAccountEntryViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id = model.CurrentAccountId });

        var client = httpClientFactory.CreateClient("api");

        try
        {
            var response = await client.PostAsJsonAsync(
                $"api/current-accounts/{model.CurrentAccountId}/entries",
                new
                {
                    model.Direction,
                    model.Amount,
                    model.Description
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                TempData["EntryError"] = "Hareket kaydedilemedi.";
            }
            else
            {
                TempData["SuccessMessage"] = "Hareket eklendi.";
            }
        }
        catch (HttpRequestException)
        {
            TempData["EntryError"] = "Hareket kaydedilirken API bağlantısı kurulamadı.";
        }

        return RedirectToAction(nameof(Details), new { id = model.CurrentAccountId });
    }

    private async Task AddApiErrorAsync(
        HttpResponseMessage response,
        string fallbackMessage)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            ModelState.AddModelError(string.Empty, error?.Message ?? fallbackMessage);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, fallbackMessage);
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}
