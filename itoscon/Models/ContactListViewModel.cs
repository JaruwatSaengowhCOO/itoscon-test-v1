using Microsoft.AspNetCore.Mvc.Rendering;

namespace itoscon.Models;

public class ContactListViewModel
{
    // ---- ตัวกรอง (รับจาก query string) ----
    public string? Search { get; set; }
    public int? DivisionId { get; set; }
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }
    public int? RoleId { get; set; }

    // ---- เรียงลำดับ: name, lastName, team, salary ----
    public string Sort { get; set; } = "name";
    public bool Desc { get; set; }

    // ---- แบ่งหน้า ----
    public int PageSize { get; set; } = 10;
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    // ---- ผลลัพธ์และรายการ dropdown ----
    public List<ContactListItem> Items { get; set; } = new();
    public List<SelectListItem> Divisions { get; set; } = new();
    public List<SelectListItem> Departments { get; set; } = new();
    public List<SelectListItem> Teams { get; set; } = new();
    public List<SelectListItem> Roles { get; set; } = new();

    public bool HasFilter =>
        !string.IsNullOrWhiteSpace(Search) || DivisionId is not null ||
        DepartmentId is not null || TeamId is not null || RoleId is not null;

    // route values ของตัวกรองปัจจุบัน (ไม่รวมค่าว่าง)
    private Dictionary<string, string> BaseRoute()
    {
        var d = new Dictionary<string, string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) d[key] = value;
        }
        Add("search", Search);
        Add("divisionId", DivisionId?.ToString());
        Add("departmentId", DepartmentId?.ToString());
        Add("teamId", TeamId?.ToString());
        Add("roleId", RoleId?.ToString());
        Add("sort", Sort);
        d["desc"] = Desc ? "true" : "false";
        d["pageSize"] = PageSize.ToString();
        return d;
    }

    // ลิงก์หัวคอลัมน์: คลิกครั้งแรกเรียง น้อย→มาก, คลิกซ้ำสลับเป็น มาก→น้อย, กลับหน้าแรกเสมอ
    public Dictionary<string, string> SortRoute(string column)
    {
        var d = BaseRoute();
        d["sort"] = column;
        d["desc"] = (Sort == column && !Desc) ? "true" : "false";
        d["page"] = "1";
        return d;
    }

    public Dictionary<string, string> PageRoute(int page)
    {
        var d = BaseRoute();
        d["page"] = page.ToString();
        return d;
    }
}
