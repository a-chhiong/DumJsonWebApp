namespace Application.Validators;

public interface IValidator<T> where T : class
{
    ValidationResult Validate(T input);
}