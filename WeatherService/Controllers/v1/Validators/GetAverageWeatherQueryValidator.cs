using FluentValidation;
using WeatherService.Models;

namespace WeatherService.Controllers.Validators;

public class GetAverageWeatherQueryValidator : AbstractValidator<GetAverageWeatherQuery>
{
    public GetAverageWeatherQueryValidator()
    {
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .Matches(@"^\d{5}$").WithMessage("Zip code must be a 5-digit number.");
        RuleFor(x => x.TimePeriod)
            .NotEmpty().WithMessage("Time period is required.")
            .Must(tp => int.TryParse(tp, out var n) && n >= 2 && n <= 5)
            .WithMessage("Time period must be an integer between 2 and 5.");
        RuleFor(x => x.Units)
            .IsInEnum().WithMessage("Units must be a valid temperature unit.");
    }
}