using FluentValidation;
using WeatherService.Models;

namespace WeatherService.Controllers.Validators;

public class GetCurrentWeatherQueryValidator : AbstractValidator<GetCurrentWeatherQuery>
{
    public GetCurrentWeatherQueryValidator()
    {
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .Matches(@"^\d{5}$").WithMessage("Zip code must be a 5-digit number.");
        RuleFor(x => x.Units)
            .IsInEnum().WithMessage("Units must be a valid temperature unit.");
    }
}
