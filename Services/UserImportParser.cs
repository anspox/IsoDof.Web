using System.Text;
using ClosedXML.Excel;
using IsoDof.Web.Models;

namespace IsoDof.Web.Services;

public interface IUserImportParser
{
    /// <summary>CSV veya XLSX içeriğini kullanıcı satırlarına çevirir.</summary>
    List<UserImportRow> Parse(Stream stream, string fileName);
}

public class UserImportParser : IUserImportParser
{
    // Başlık eşleştirme: normalize edilmiş başlık -> alan
    private static readonly Dictionary<string, string> HeaderMap = new()
    {
        ["adsoyad"] = "name", ["ad soyad"] = "name", ["isim"] = "name", ["adisoyadi"] = "name",
        ["ad"] = "name", ["fullname"] = "name", ["name"] = "name", ["personel"] = "name",
        ["eposta"] = "email", ["e-posta"] = "email", ["email"] = "email", ["e-mail"] = "email", ["mail"] = "email",
        ["departman"] = "dept", ["department"] = "dept", ["bolum"] = "dept", ["birim"] = "dept",
        ["rol"] = "role", ["role"] = "role", ["yetki"] = "role",
        ["sicil"] = "sicil", ["sicilno"] = "sicil", ["sicil no"] = "sicil", ["personelno"] = "sicil", ["personel no"] = "sicil",
        ["sifre"] = "pass", ["şifre"] = "pass", ["parola"] = "pass", ["password"] = "pass",
    };

    public List<UserImportRow> Parse(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".xlsx" or ".xlsm"
            ? ParseExcel(stream)
            : ParseCsv(stream);
    }

    private static string Norm(string s) =>
        s.Trim().ToLowerInvariant()
         .Replace("ı", "i").Replace("İ", "i").Replace("ş", "s").Replace("ğ", "g")
         .Replace("ü", "u").Replace("ö", "o").Replace("ç", "c");

    private static Dictionary<int, string> MapHeaders(IEnumerable<string> headerCells)
    {
        var map = new Dictionary<int, string>();
        var i = 0;
        foreach (var cell in headerCells)
        {
            var key = Norm(cell ?? "");
            if (HeaderMap.TryGetValue(key, out var field))
                map[i] = field;
            i++;
        }
        return map;
    }

    private static UserImportRow BuildRow(int rowNumber, Dictionary<int, string> headerMap, IReadOnlyList<string> cells)
    {
        var row = new UserImportRow { RowNumber = rowNumber };
        foreach (var (idx, field) in headerMap)
        {
            if (idx >= cells.Count) continue;
            var val = cells[idx]?.Trim();
            if (string.IsNullOrEmpty(val)) continue;
            switch (field)
            {
                case "name": row.FullName = val; break;
                case "email": row.Email = val; break;
                case "dept": row.Department = val; break;
                case "role": row.Role = val; break;
                case "sicil": row.SicilNo = val; break;
                case "pass": row.Password = val; break;
            }
        }
        return row;
    }

    private List<UserImportRow> ParseExcel(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var rows = ws.RangeUsed()?.RowsUsed().ToList() ?? new();
        var result = new List<UserImportRow>();
        if (rows.Count < 2) return result;

        var headerCells = rows[0].Cells().Select(c => c.GetString()).ToList();
        var headerMap = MapHeaders(headerCells);
        var colCount = headerCells.Count;

        for (var r = 1; r < rows.Count; r++)
        {
            var cells = new List<string>();
            for (var c = 1; c <= colCount; c++)
                cells.Add(rows[r].Cell(c).GetString());

            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            result.Add(BuildRow(r + 1, headerMap, cells));
        }
        return result;
    }

    private List<UserImportRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                        .Where(l => l.Length > 0).ToList();
        var result = new List<UserImportRow>();
        if (lines.Count < 2) return result;

        var delimiter = lines[0].Count(ch => ch == ';') >= lines[0].Count(ch => ch == ',') ? ';' : ',';

        var headerCells = SplitCsvLine(lines[0], delimiter);
        var headerMap = MapHeaders(headerCells);

        for (var i = 1; i < lines.Count; i++)
        {
            var cells = SplitCsvLine(lines[i], delimiter);
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            result.Add(BuildRow(i + 1, headerMap, cells));
        }
        return result;
    }

    private static List<string> SplitCsvLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (ch == delimiter && !inQuotes)
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(ch);
        }
        cells.Add(sb.ToString());
        return cells;
    }
}
