using FluentValidation;
using Application.DTOs.PrescriptionReview.Requests;

namespace Application.Validators.PrescriptionReview;

public class UpdatePrescriptionReviewValidator : AbstractValidator<UpdatePrescriptionReviewDTO>
{
    public UpdatePrescriptionReviewValidator()
    {
        RuleFor(x => x.Medicines)
            .NotEmpty()
                .WithMessage("At least one medicine must remain after editing. Use Reject instead if the prescription is invalid.");

        RuleForEach(x => x.Medicines).ChildRules(m =>
        {
            m.RuleFor(x => x.MedicineName)
                .NotEmpty().WithMessage("Medicine name is required.")
                .MaximumLength(500);

            m.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be at least 1.");

            m.RuleFor(x => x.Strength)
                .MaximumLength(100).When(x => x.Strength is not null);

            m.RuleFor(x => x.DosageForm)
                .MaximumLength(100).When(x => x.DosageForm is not null);

            m.RuleFor(x => x.Frequency)
                .MaximumLength(200).When(x => x.Frequency is not null);

            m.RuleFor(x => x.Duration)
                .MaximumLength(200).When(x => x.Duration is not null);
        });
    }
}
