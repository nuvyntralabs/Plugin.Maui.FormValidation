namespace Plugin.Maui.FormValidation.Tests;

sealed class SignUpModel
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Age { get; set; }
    public int Years { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? Username { get; set; }
    public string? CompanyName { get; set; }
    public bool IsBusiness { get; set; }
    public string? Zip { get; set; }
}

sealed class SignUpViewModel : ValidatableViewModel<SignUpViewModel>
{
    string? email;
    string? phone;
    string? companyName;
    bool isBusiness;

    public SignUpViewModel()
    {
        Validator
            .Rule(x => x.Email).Required().Email()
            .Rule(x => x.Phone).Phone()
            .Rule(x => x.CompanyName).When(x => x.IsBusiness).Required();
    }

    public string? Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public string? Phone
    {
        get => phone;
        set => SetProperty(ref phone, value);
    }

    public string? CompanyName
    {
        get => companyName;
        set => SetProperty(ref companyName, value);
    }

    public bool IsBusiness
    {
        get => isBusiness;
        set => SetProperty(ref isBusiness, value);
    }
}
