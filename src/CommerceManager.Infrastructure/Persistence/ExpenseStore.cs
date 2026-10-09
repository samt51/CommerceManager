using CommerceManager.Application.Cash;
using CommerceManager.Domain.Cash;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class ExpenseStore(AppDbContext db) : IExpenseStore
{
    public async Task<LedgerCommandResult> AddTypeAsync(
        string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new LedgerCommandResult(false, "Gider tipi gerekli.");

        var normalized = name.Trim();

        if (normalized.Length > 100)
            return new LedgerCommandResult(false, "Gider tipi en fazla 100 karakter olabilir.");

        var exists = await db.ExpenseTypes.AnyAsync(
            x => x.Name == normalized,
            cancellationToken);

        if (exists)
            return new LedgerCommandResult(false, "Bu gider tipi zaten var.");

        db.ExpenseTypes.Add(ExpenseType.Create(normalized));
        await db.SaveChangesAsync(cancellationToken);

        return new LedgerCommandResult(true, null);
    }

    public async Task<LedgerCommandResult> AddExpenseAsync(
        Guid expenseTypeId,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var typeExists = await db.ExpenseTypes.AnyAsync(
            x => x.Id == expenseTypeId && x.IsActive,
            cancellationToken);

        if (!typeExists)
            return new LedgerCommandResult(false, "Gider tipi bulunamadı.");

        if (amount <= 0)
            return new LedgerCommandResult(false, "Tutar 0'dan büyük olmalı.");

        if (string.IsNullOrWhiteSpace(description))
            return new LedgerCommandResult(false, "Açıklama gerekli.");

        if (description.Trim().Length > 500)
            return new LedgerCommandResult(false, "Açıklama en fazla 500 karakter olabilir.");

        var occurredAt = DateTimeOffset.UtcNow;
        var expense = Expense.Create(
            expenseTypeId,
            amount,
            description,
            occurredAt);

        db.Expenses.Add(expense);
        db.CashMovements.Add(
            CashMovement.Create(
                CashDirection.Out,
                amount,
                description.Trim(),
                occurredAt,
                expense.Id));

        await db.SaveChangesAsync(cancellationToken);

        return new LedgerCommandResult(true, null);
    }
}

public sealed class ExpenseReader(AppDbContext db) : IExpenseReader
{
    public async Task<ExpensePage> ListAsync(
        Guid? expenseTypeId,
        CancellationToken cancellationToken)
    {
        var types = await ListTypesAsync(cancellationToken);

        var query =
            from expense in db.Expenses.AsNoTracking()
            join type in db.ExpenseTypes.AsNoTracking()
                on expense.ExpenseTypeId equals type.Id
            select new { expense, type };

        if (expenseTypeId is not null)
            query = query.Where(x => x.expense.ExpenseTypeId == expenseTypeId);

        var rows = await query
            .OrderByDescending(x => x.expense.OccurredAt)
            .Take(200)
            .Select(x => new ExpenseRow(
                x.expense.Id,
                x.type.Name,
                x.expense.Amount,
                x.expense.Description,
                x.expense.OccurredAt))
            .ToListAsync(cancellationToken);

        var total = expenseTypeId is null
            ? await db.Expenses.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0
            : await db.Expenses
                .Where(x => x.ExpenseTypeId == expenseTypeId)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        return new ExpensePage(total, types, rows);
    }

    public async Task<IReadOnlyList<ExpenseTypeRow>> ListTypesAsync(
        CancellationToken cancellationToken)
    {
        return await db.ExpenseTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ExpenseTypeRow(x.Id, x.Name))
            .ToListAsync(cancellationToken);
    }
}

public sealed class CashStore(AppDbContext db) : ICashStore
{
    public async Task<LedgerCommandResult> AddInflowAsync(
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            return new LedgerCommandResult(false, "Tutar 0'dan büyük olmalı.");

        if (string.IsNullOrWhiteSpace(description))
            return new LedgerCommandResult(false, "Açıklama gerekli.");

        if (description.Trim().Length > 500)
            return new LedgerCommandResult(false, "Açıklama en fazla 500 karakter olabilir.");

        db.CashMovements.Add(
            CashMovement.Create(
                CashDirection.In,
                amount,
                description,
                DateTimeOffset.UtcNow));

        await db.SaveChangesAsync(cancellationToken);

        return new LedgerCommandResult(true, null);
    }
}

public sealed class CashReader(AppDbContext db) : ICashReader
{
    public async Task<CashPage> GetAsync(CancellationToken cancellationToken)
    {
        var items = await db.CashMovements
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(200)
            .Select(x => new CashMovementRow(
                x.Id,
                (int)x.Direction,
                x.Amount,
                x.Description,
                x.OccurredAt))
            .ToListAsync(cancellationToken);

        var totalIn = await db.CashMovements
            .Where(x => x.Direction == CashDirection.In)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        var totalOut = await db.CashMovements
            .Where(x => x.Direction == CashDirection.Out)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        return new CashPage(totalIn - totalOut, totalIn, totalOut, items);
    }
}
