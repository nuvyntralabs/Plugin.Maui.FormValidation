namespace Plugin.Maui.FormValidation.Tests;

public sealed class RuleTests
{
    [Fact]
    public void Required_fails_on_null_and_whitespace()
    {
        var model = new SignUpModel();
        var result = Validator.For(model).Rule(x => x.Email).Required().Validate();

        Assert.False(result.IsValid);
        Assert.Equal("required", result.Errors[0].Code);
    }

    [Fact]
    public void Required_passes_when_present()
    {
        var model = new SignUpModel { Email = "ada@example.com" };
        var result = Validator.For(model).Rule(x => x.Email).Required().Validate();

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("ada@example.com", true)]
    [InlineData("not-an-email", false)]
    [InlineData("ada@", false)]
    [InlineData("", true)]
    public void Email_validates_format_and_skips_empty(string email, bool expected)
    {
        var model = new SignUpModel { Email = email };
        var result = Validator.For(model).Rule(x => x.Email).Email().Validate();

        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("+15551234567", true)]
    [InlineData("555-123-4567", true)]
    [InlineData("(555) 123-4567", true)]
    [InlineData("123", false)]
    [InlineData("", true)]
    public void Phone_accepts_common_mobile_formats(string phone, bool expected)
    {
        var model = new SignUpModel { Phone = phone };
        var result = Validator.For(model).Rule(x => x.Phone).Phone().Validate();

        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com/path", true)]
    [InlineData("www.example.com", true)]
    [InlineData("not-a-url", false)]
    [InlineData("ftp://example.com", false)]
    public void Url_requires_http_or_https(string website, bool expected)
    {
        var model = new SignUpModel { Website = website };
        var result = Validator.For(model).Rule(x => x.Website).Url().Validate();

        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("18", true)]
    [InlineData("18.5", true)]
    [InlineData("abc", false)]
    public void Numeric_parses_strings(string age, bool expected)
    {
        var model = new SignUpModel { Age = age };
        var result = Validator.For(model).Rule(x => x.Age).Numeric().Validate();

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void Min_and_max_compare_numbers()
    {
        var model = new SignUpModel { Years = 15 };
        var result = Validator.For(model).Rule(x => x.Years).Min(18).Max(99).Validate();

        Assert.False(result.IsValid);
        Assert.Equal("min", result.Errors[0].Code);

        model.Years = 120;
        result = Validator.For(model).Rule(x => x.Years).Min(18).Max(99).Validate();
        Assert.Equal("max", result.Errors[0].Code);

        model.Years = 30;
        result = Validator.For(model).Rule(x => x.Years).Min(18).Max(99).Validate();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Min_on_numeric_string_compares_parsed_value()
    {
        var model = new SignUpModel { Age = "12" };
        var result = Validator.For(model).Rule(x => x.Age).Numeric().Min(18).Validate();

        Assert.False(result.IsValid);
        Assert.Equal("min", result.Errors[0].Code);
    }

    [Fact]
    public void MinLength_and_regex_run_on_strings()
    {
        var model = new SignUpModel { Password = "ab", Zip = "12" };
        var result = Validator
            .For(model)
            .Rule(x => x.Password).MinLength(8)
            .Rule(x => x.Zip).Regex(@"^\d{5}$")
            .Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "minlength");
        Assert.Contains(result.Errors, error => error.Code == "regex");
    }

    [Fact]
    public void Length_and_inclusive_between_work()
    {
        var model = new SignUpModel { Username = "ab", Years = 200 };
        var result = Validator
            .For(model)
            .Rule(x => x.Username).Length(3, 20)
            .Rule(x => x.Years).InclusiveBetween(18, 99)
            .Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "length");
        Assert.Contains(result.Errors, error => error.Code == "between");
    }

    [Fact]
    public void EqualTo_compares_another_property()
    {
        var model = new SignUpModel { Password = "secret12", ConfirmPassword = "nope" };
        var result = Validator
            .For(model)
            .Rule(x => x.ConfirmPassword)
            .EqualTo(x => x.Password, "Passwords do not match")
            .Validate();

        Assert.False(result.IsValid);
        Assert.Equal("Passwords do not match", result.Errors[0].Message);

        model.ConfirmPassword = "secret12";
        result = Validator.For(model).Rule(x => x.ConfirmPassword).EqualTo(x => x.Password).Validate();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void WithMessage_overrides_the_last_rule()
    {
        var model = new SignUpModel();
        var result = Validator
            .For(model)
            .Rule(x => x.Email)
            .Required()
            .WithMessage("Email please")
            .Validate();

        Assert.Equal("Email please", result.FirstError("Email"));
    }

    [Fact]
    public void Cascade_stop_returns_first_error_only()
    {
        var model = new SignUpModel();
        var result = Validator
            .For(model)
            .Rule(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .Required()
            .Email()
            .Validate();

        Assert.Single(result.Errors);
        Assert.Equal("required", result.Errors[0].Code);
    }
}
