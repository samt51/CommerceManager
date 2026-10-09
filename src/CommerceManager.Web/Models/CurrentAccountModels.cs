using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Web.Models;

public sealed class CurrentAccountRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Kind { get; set; }

    public string? Phone { get; set; }

    public decimal Balance { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string KindText => Kind == 2 ? "Tedarikçi" : "Müşteri";

    public string BalanceText =>
        Balance > 0
            ? "Borçlu"
            : Balance < 0
                ? "Alacaklı"
                : "Dengede";
}

public sealed class CurrentAccountPage
{
    public int Total { get; set; }

    public decimal Receivable { get; set; }

    public decimal Payable { get; set; }

    public List<CurrentAccountRow> Items { get; set; } = [];
}

public sealed class CurrentAccountEntryRow
{
    public Guid Id { get; set; }

    public int Direction { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string DirectionText => Direction == 2 ? "Alacak" : "Borç";
}

public sealed class CurrentAccountDetails
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Kind { get; set; }

    public string? Phone { get; set; }

    public string? TaxNumber { get; set; }

    public decimal Balance { get; set; }

    public List<CurrentAccountEntryRow> Entries { get; set; } = [];

    public string KindText => Kind == 2 ? "Tedarikçi" : "Müşteri";
}

public sealed class CreateCurrentAccountViewModel
{
    [Required(ErrorMessage = "Cari adı gerekli.")]
    [MaxLength(200, ErrorMessage = "Cari adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Cari adı")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 2, ErrorMessage = "Cari tipi seçin.")]
    [Display(Name = "Tip")]
    public int Kind { get; set; } = 1;

    [Required(ErrorMessage = "Telefon gerekli.")]
    [MaxLength(30, ErrorMessage = "Telefon en fazla 30 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string Phone { get; set; } = string.Empty;
}

public sealed class AddCurrentAccountEntryViewModel
{
    public Guid CurrentAccountId { get; set; }

    [Range(1, 2, ErrorMessage = "Hareket tipi seçin.")]
    [Display(Name = "Hareket")]
    public int Direction { get; set; } = 1;

    [Range(0.01, 999999999, ErrorMessage = "Tutar 0'dan büyük olmalı.")]
    [Display(Name = "Tutar")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Açıklama gerekli.")]
    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;
}
