using System.Globalization;
using System.Text.Json;

namespace itoscon.Services;

public interface IStaticText
{
    string this[string key] { get; }
}

// อ่าน th.json และ en.json ครั้งเดียวตอนเริ่มโปรแกรม แล้วเลือกภาษาตามวัฒนธรรมของคำขอ
public class StaticText : IStaticText
{
    private readonly Dictionary<string, Dictionary<string, string>> _data = new();

    public StaticText()
    {
        foreach (var lang in new[] { "th", "en" })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Text", $"{lang}.json");
            var json = File.ReadAllText(path);
            _data[lang] = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                          ?? new Dictionary<string, string>();
        }
    }

    public string this[string key]
    {
        get
        {
            // ภาษาที่เลือก -> ถ้าไม่มีให้ใช้ไทย -> ถ้าไม่มีอีกให้คืน key
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "en" : "th";
            if (_data[lang].TryGetValue(key, out var text)) return text;
            if (_data["th"].TryGetValue(key, out var fallback)) return fallback;
            return key;
        }
    }
}