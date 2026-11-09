using FluentValidation;
using Fundoo.User.Api.Contracts;

namespace Fundoo.User.Api.Validation;

public sealed class RegisterRequestValidator
    : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters.");

        RuleFor(request => request.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Please provide a valid email address.")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must contain at least 8 characters.");
    }
}