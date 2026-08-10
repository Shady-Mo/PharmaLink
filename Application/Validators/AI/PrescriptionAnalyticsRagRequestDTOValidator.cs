using Application.DTOs.AI.RAG;
using FluentValidation;

namespace Application.Validators.AI;

public class PrescriptionAnalyticsRagRequestDTOValidator : AbstractValidator<PrescriptionAnalyticsRagRequestDTO>
{
    public PrescriptionAnalyticsRagRequestDTOValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
                .WithMessage("السؤال مطلوب لإجراء البحث والتحليل في الـ RAG.")
            .MinimumLength(3)
                .WithMessage("يجب أن يكون السؤال مكوناً من 3 حروف على الأقل.")
            .MaximumLength(1000)
                .WithMessage("يجب ألا يتجاوز السؤال 1000 حرف.");

        RuleFor(x => x.StartDate)
            .Must((dto, startDate) => !startDate.HasValue || !dto.EndDate.HasValue || startDate.Value <= dto.EndDate.Value)
                .WithMessage("تاريخ البداية يجب أن يكون قبل أو يطابق تاريخ النهاية.");
    }
}
