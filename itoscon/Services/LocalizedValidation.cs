using System.ComponentModel.DataAnnotations;

namespace itoscon.Services;

// ตัวกลางสำหรับ attribute ที่ไม่มี DI: ตั้งค่าครั้งเดียวตอนเริ่มโปรแกรม
public static class Loc
{
    public static IStaticText? Current { get; set; }

    public static string Text(string key) => Current?[key] ?? key;
}

// ErrorMessage ของ attribute ทุกตัวเป็น key ใน JSON เช่น "err.firstNameRequired"
public class LRequiredAttribute : RequiredAttribute
{
    public override string FormatErrorMessage(string name) =>
        ErrorMessage is null ? base.FormatErrorMessage(name) : Loc.Text(ErrorMessage);
}

public class LStringLengthAttribute : StringLengthAttribute
{
    public LStringLengthAttribute(int maximumLength) : base(maximumLength) { }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage is null ? base.FormatErrorMessage(name) : Loc.Text(ErrorMessage);
}

public class LRegularExpressionAttribute : RegularExpressionAttribute
{
    public LRegularExpressionAttribute(string pattern) : base(pattern) { }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage is null ? base.FormatErrorMessage(name) : Loc.Text(ErrorMessage);
}

public class LEmailAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null) return true; // ให้ Required เป็นคนตรวจค่าว่าง
        return new EmailAddressAttribute().IsValid(value);
    }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage is null ? base.FormatErrorMessage(name) : Loc.Text(ErrorMessage);
}

public class LRangeAttribute : RangeAttribute
{
    public LRangeAttribute(double minimum, double maximum) : base(minimum, maximum) { }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage is null ? base.FormatErrorMessage(name) : Loc.Text(ErrorMessage);
}