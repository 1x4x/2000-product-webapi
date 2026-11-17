using System.Globalization;
using System.Text;
using ProductAPI.Models;

namespace ProductAPI.Services;

public static class ProductCsvService
{
    public const string Header = "Sku,Name,Description,CategoryId,SupplierId,PurchasePrice,RetailPrice,ResellerPrice,Currency,TargetStockQuantity";

    public static string Export(IEnumerable<Product> products)
    {
        var sb = new StringBuilder().AppendLine(Header);
        foreach (var p in products)
            sb.AppendLine(string.Join(",", new[]
            {
                Escape(p.Sku), Escape(p.Name), Escape(p.Description ?? ""),
                p.CategoryId?.ToString() ?? "", p.SupplierId?.ToString() ?? "",
                p.PurchasePrice.ToString(CultureInfo.InvariantCulture),
                p.RetailPrice.ToString(CultureInfo.InvariantCulture),
                p.ResellerPrice.ToString(CultureInfo.InvariantCulture),
                Escape(p.Currency), p.TargetStockQuantity.ToString(CultureInfo.InvariantCulture)
            }));
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.IndexOfAny(new[] {',','"','\r','\n'}) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    public static List<string[]> Parse(string csv)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (c == '"')
            {
                if (quoted && i + 1 < csv.Length && csv[i + 1] == '"') { value.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { fields.Add(value.ToString()); value.Clear(); }
            else if ((c == '\n' || c == '\r') && !quoted)
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                fields.Add(value.ToString()); value.Clear();
                if (fields.Any(x => x.Length > 0)) records.Add(fields.ToArray());
                fields.Clear();
            }
            else value.Append(c);
        }
        if (quoted) throw new FormatException("Unterminated quoted CSV field.");
        fields.Add(value.ToString());
        if (fields.Any(x => x.Length > 0)) records.Add(fields.ToArray());
        return records;
    }
}
