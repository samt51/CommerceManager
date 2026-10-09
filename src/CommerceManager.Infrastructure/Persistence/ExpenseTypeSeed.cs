using CommerceManager.Domain.Cash;

namespace CommerceManager.Infrastructure.Persistence;

internal static class ExpenseTypeSeed
{
    public static readonly ExpenseType BenzinMazot = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111101",
        "Benzin & Mazot");

    public static readonly ExpenseType AracHarcligi = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111102",
        "Araç harçlığı");

    public static readonly ExpenseType Yemek = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111103",
        "Yemek");

    public static readonly ExpenseType Kira = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111104",
        "Kira");

    public static readonly ExpenseType Elektrik = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111105",
        "Elektrik");

    public static readonly ExpenseType Su = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111106",
        "Su");

    public static readonly ExpenseType Dogalgaz = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111107",
        "Doğalgaz");

    public static readonly ExpenseType Telefon = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111108",
        "Telefon / internet");

    public static readonly ExpenseType Kargo = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111109",
        "Kargo");

    public static readonly ExpenseType Personel = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111110",
        "Personel");

    public static readonly ExpenseType Mutfak = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111111",
        "Mutfak / market");

    public static readonly ExpenseType Bakim = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111112",
        "Bakım onarım");

    public static readonly ExpenseType Vergi = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111113",
        "Vergi / SGK");

    public static readonly ExpenseType Diger = Create(
        "8f1c2a10-6b4e-4a1d-9c11-111111111114",
        "Diğer");

    private static ExpenseType Create(string id, string name)
    {
        var type = ExpenseType.Create(name);
        typeof(ExpenseType).GetProperty(nameof(ExpenseType.Id))!
            .SetValue(type, Guid.Parse(id));
        return type;
    }
}
