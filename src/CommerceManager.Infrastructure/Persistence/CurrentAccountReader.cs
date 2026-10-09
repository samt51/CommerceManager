using CommerceManager.Application.Ledger;
using CommerceManager.Domain.Ledger;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class CurrentAccountReader(AppDbContext db) : ICurrentAccountReader
{
    public async Task<CurrentAccountPage> ListAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var query = db.CurrentAccounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Phone != null && x.Phone.Contains(term)));
        }

        var accounts = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var ids = accounts.Select(x => x.Id).ToArray();

        var balances = await db.CurrentAccountEntries
            .AsNoTracking()
            .Where(x => ids.Contains(x.CurrentAccountId))
            .GroupBy(x => x.CurrentAccountId)
            .Select(g => new
            {
                Id = g.Key,
                Balance = g.Sum(x =>
                    x.Direction == LedgerDirection.Debit
                        ? x.Amount
                        : -x.Amount)
            })
            .ToListAsync(cancellationToken);

        var items = accounts
            .Select(account =>
            {
                var balance = balances
                    .SingleOrDefault(x => x.Id == account.Id)
                    ?.Balance ?? 0;

                return new CurrentAccountRow(
                    account.Id,
                    account.Name,
                    (int)account.Kind,
                    account.Phone,
                    balance,
                    account.CreatedAt);
            })
            .ToList();

        return new CurrentAccountPage(
            items.Count,
            items.Where(x => x.Balance > 0).Sum(x => x.Balance),
            items.Where(x => x.Balance < 0).Sum(x => -x.Balance),
            items);
    }

    public async Task<CurrentAccountDetails?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await db.CurrentAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (account is null)
            return null;

        var entries = await db.CurrentAccountEntries
            .AsNoTracking()
            .Where(x => x.CurrentAccountId == id)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new CurrentAccountEntryRow(
                x.Id,
                (int)x.Direction,
                x.Amount,
                x.Description,
                x.OccurredAt))
            .ToListAsync(cancellationToken);

        var balance = entries.Sum(x =>
            x.Direction == (int)LedgerDirection.Debit
                ? x.Amount
                : -x.Amount);

        return new CurrentAccountDetails(
            account.Id,
            account.Name,
            (int)account.Kind,
            account.Phone,
            account.TaxNumber,
            balance,
            entries);
    }
}
