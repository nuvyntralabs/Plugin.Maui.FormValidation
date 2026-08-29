using Plugin.Maui.FormValidation;

namespace Plugin.Maui.FormValidation.Sample;

public sealed class SignUpViewModel : ValidatableViewModel<SignUpViewModel>
{
    string email = "";
    string phone = "";
    string website = "";
    string age = "";
    string username = "";
    string password = "";
    string confirmPassword = "";
    string companyName = "";
    bool isBusiness;
    string status = "Fill the form, then tap Create account.";

    public SignUpViewModel()
    {
        Validator
            .Rule(x => x.Email)
                .Required()
                .Email()
                .Server(ValidateEmailOnServerAsync)
            .Rule(x => x.Phone)
                .Phone()
            .Rule(x => x.Website)
                .Url()
            .Rule(x => x.Age)
                .Required()
                .Numeric()
                .Min(18)
                .Max(99)
            .Rule(x => x.Username)
                .Required()
                .MinLength(3)
                .MustAsync(IsUsernameAvailableAsync, "Username is taken. Try another.")
            .Rule(x => x.Password)
                .Required()
                .MinLength(8)
                .Regex("[A-Z]", "Password needs an uppercase letter.")
            .Rule(x => x.ConfirmPassword)
                .Required()
                .EqualTo(x => x.Password, "Passwords do not match.")
            .Rule(x => x.CompanyName)
                .When(x => x.IsBusiness)
                .Required("Company name is required for a business account.");
    }

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public string Phone
    {
        get => phone;
        set => SetProperty(ref phone, value);
    }

    public string Website
    {
        get => website;
        set => SetProperty(ref website, value);
    }

    public string Age
    {
        get => age;
        set => SetProperty(ref age, value);
    }

    public string Username
    {
        get => username;
        set => SetProperty(ref username, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public string ConfirmPassword
    {
        get => confirmPassword;
        set => SetProperty(ref confirmPassword, value);
    }

    public string CompanyName
    {
        get => companyName;
        set => SetProperty(ref companyName, value);
    }

    public bool IsBusiness
    {
        get => isBusiness;
        set
        {
            if (SetProperty(ref isBusiness, value))
            {
                _ = ValidatePropertyAsync(nameof(CompanyName));
            }
        }
    }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public async Task SubmitAsync()
    {
        Status = "Checking the form…";
        var result = await ValidateAsync();
        Status = result.IsValid
            ? "Account is ready. Every rule passed — including the simulated server check."
            : $"{result.Errors.Count} field(s) need attention.";
    }

    static async Task<ServerValidationResult> ValidateEmailOnServerAsync(string value, CancellationToken cancellationToken)
    {
        await Task.Delay(350, cancellationToken);
        return value.EndsWith("@taken.example", StringComparison.OrdinalIgnoreCase)
            ? ServerValidationResult.Fail("This email is already registered.")
            : ServerValidationResult.Ok();
    }

    static async Task<bool> IsUsernameAvailableAsync(string value, CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken);
        return !string.Equals(value, "admin", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(value, "taken", StringComparison.OrdinalIgnoreCase);
    }
}
