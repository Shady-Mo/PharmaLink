namespace Application.Errors;

public static class PharmacyBranchScheduleErrors
{
    public static readonly Error InvalidSchedule = new(
        "BranchSchedule.Invalid",
        "يجب أن يحتوي الجدول على 7 أيام فريدة (الأحد حتى السبت).",
        StatusCodes.Status400BadRequest);

    public static Error InvalidTimeFormat(DayOfWeek day, string part) => new(
        "BranchSchedule.InvalidTime",
        $"تنسيق وقت {part} غير صحيح ليوم {day}. يرجى استخدام تنسيق HH:mm (مثال: 09:00).",
        StatusCodes.Status400BadRequest);
}
