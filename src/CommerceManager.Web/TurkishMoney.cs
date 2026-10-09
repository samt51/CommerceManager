using System.Globalization;

namespace CommerceManager.Web;

public static class TurkishMoney
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("tr-TR");

    public static string Format(decimal amount)
    {
        var culture = (CultureInfo)Culture.Clone();
        culture.NumberFormat.NumberGroupSeparator = "";

        return amount.ToString("N2", culture) + " ₺";
    }

    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim().Replace("₺", "").Replace(" ", "");

        if (value.Contains(',') && value.Contains('.'))
            value = value.Replace(".", "").Replace(',', '.');
        else if (value.Contains(','))
            value = value.Replace(',', '.');

        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amount) && amount > 0;
    }
}
