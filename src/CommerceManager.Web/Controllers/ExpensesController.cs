using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class ExpensesController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? expenseTypeId,
        CancellationToken cancellationToken)
    {
        ViewBag.ExpenseTypeId = expenseTypeId;

        try
        {
            var page = await httpClientFactory.CreateClient("api")
                .GetFromJsonAsync<ExpensePage>(
                    expenseTypeId is null
                        ? "api/expenses"
                        : $"api/expenses?expenseTypeId={expenseTypeId}",
                    cancellationToken);

            return View(page ?? new ExpensePage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Giderler alınırken API bağlantısı kurulamadı.";
            return View(new ExpensePage());
        }
    }

    [HttpGet]
    public IActionResult Create() =>
        RedirectToAction(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateExpenseViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid ||
            !TurkishMoney.TryParse(model.AmountText, out var amount))
        {
            TempData["FormError"] = "Gider tipi, tutar ve açıklama gerekli. Tutar örnek: 1.000,50";
            TempData["OpenModal"] = "expense";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    "api/expenses",
                    new
                    {
                        model.ExpenseTypeId,
                        Amount = amount,
                        model.Description
                    },
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                TempData["FormError"] = "Gider kaydedilemedi.";
                TempData["OpenModal"] = "expense";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (HttpRequestException)
        {
            TempData["FormError"] = "Gider kaydedilirken API bağlantısı kurulamadı.";
            TempData["OpenModal"] = "expense";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Gider kasadan düşüldü.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateType(
        CreateExpenseTypeViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["TypeError"] = "Gider tipi gerekli.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    "api/expenses/types",
                    new { model.Name },
                    cancellationToken);

            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "TypeError"] =
                response.IsSuccessStatusCode
                    ? "Gider tipi eklendi."
                    : "Gider tipi kaydedilemedi.";
        }
        catch (HttpRequestException)
        {
            TempData["TypeError"] = "Gider tipi kaydedilirken API bağlantısı kurulamadı.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<CreateExpenseViewModel> BuildCreateModelAsync(
        CancellationToken cancellationToken)
    {
        var page = await httpClientFactory.CreateClient("api")
            .GetFromJsonAsync<ExpensePage>("api/expenses", cancellationToken);

        return new CreateExpenseViewModel
        {
            Types = page?.Types ?? []
        };
    }
}
