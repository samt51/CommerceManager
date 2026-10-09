using CommerceManager.Application.Ledger;
using CommerceManager.Domain.Ledger;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class CurrentAccountStore(AppDbContext db) : ICurrentAccountStore
{
    public async Task<LedgerResult> AddAccountAsync(
        string name,
        CurrentAccountKind kind,
        string? phone,
        string? taxNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new LedgerResult(false, "Cari adı gerekli.");

        if (name.Trim().Length > 200)
            return new LedgerResult(false, "Cari adı en fazla 200 karakter olabilir.");

        if (!Enum.IsDefined(kind))
            return new LedgerResult(false, "Cari tipi geçersiz.");

        if (string.IsNullOrWhiteSpace(phone))
            return new LedgerResult(false, "Telefon gerekli.");

        if (phone.Trim().Length > 30)
            return new LedgerResult(false, "Telefon en fazla 30 karakter olabilir.");

        db.CurrentAccounts.Add(
            CurrentAccount.Create(name, kind, phone, taxNumber));

        await db.SaveChangesAsync(cancellationToken);

        return new LedgerResult(true, null);
    }

    public async Task<LedgerResult> AddEntryAsync(
        Guid currentAccountId,
        LedgerDirection direction,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var exists = await db.CurrentAccounts.AnyAsync(
            x => x.Id == currentAccountId,
            cancellationToken);

        if (!exists)
            return new LedgerResult(false, "Cari bulunamadı.");

        if (!Enum.IsDefined(direction))
            return new LedgerResult(false, "Hareket tipi geçersiz.");

        if (amount <= 0)
            return new LedgerResult(false, "Tutar 0'dan büyük olmalı.");

        if (string.IsNullOrWhiteSpace(description))
            return new LedgerResult(false, "Açıklama gerekli.");

        if (description.Trim().Length > 500)
            return new LedgerResult(false, "Açıklama en fazla 500 karakter olabilir.");

        db.CurrentAccountEntries.Add(
            CurrentAccountEntry.Create(
                currentAccountId,
                direction,
                amount,
                description,
                DateTimeOffset.UtcNow));

        await db.SaveChangesAsync(cancellationToken);

        return new LedgerResult(true, null);
    }
}
