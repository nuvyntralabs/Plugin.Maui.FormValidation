namespace Plugin.Maui.FormValidation.Tests;

public sealed class ConditionalAndAsyncTests
{
    [Fact]
    public void When_skips_rules_unless_condition_matches()
    {
        var model = new SignUpModel { IsBusiness = false, CompanyName = null };
        var builder = Validator
            .For(model)
            .Rule(x => x.CompanyName)
            .When(x => x.IsBusiness)
            .Required();

        Assert.True(builder.Validate().IsValid);

        model.IsBusiness = true;
        Assert.False(builder.Validate().IsValid);
    }

    [Fact]
    public void Unless_is_the_inverse_of_when()
    {
        var model = new SignUpModel { IsBusiness = true, CompanyName = null };
        var result = Validator
            .For(model)
            .Rule(x => x.CompanyName)
            .Unless(x => !x.IsBusiness)
            .Required()
            .Validate();

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Must_uses_a_custom_predicate()
    {
        var model = new SignUpModel { Username = "admin" };
        var result = Validator
            .For(model)
            .Rule(x => x.Username)
            .Must(value => !string.Equals(value, "admin", StringComparison.OrdinalIgnoreCase), "Reserved")
            .Validate();

        Assert.False(result.IsValid);
        Assert.Equal("Reserved", result.Errors[0].Message);
    }

    [Fact]
    public async Task MustAsync_runs_asynchronous_predicates()
    {
        var model = new SignUpModel { Username = "taken" };
        var result = await Validator
            .For(model)
            .Rule(x => x.Username)
            .MustAsync(async (value, cancellationToken) =>
            {
                await Task.Delay(10, cancellationToken);
                return value != "taken";
            }, "Username is taken")
            .ValidateAsync();

        Assert.False(result.IsValid);
        Assert.Equal("must", result.Errors[0].Code);
    }

    [Fact]
    public async Task Server_accepts_structured_results()
    {
        var model = new SignUpModel { Email = "ada@taken.example" };
        var result = await Validator
            .For(model)
            .Rule(x => x.Email)
            .Server(async (email, cancellationToken) =>
            {
                await Task.Delay(5, cancellationToken);
                return email?.EndsWith("@taken.example", StringComparison.OrdinalIgnoreCase) == true
                    ? ServerValidationResult.Fail("Email is already registered")
                    : ServerValidationResult.Ok();
            })
            .ValidateAsync();

        Assert.False(result.IsValid);
        Assert.Equal("server", result.Errors[0].Code);
        Assert.Equal("Email is already registered", result.Errors[0].Message);
    }

    [Fact]
    public async Task Server_string_overload_treats_null_as_success()
    {
        var model = new SignUpModel { Email = "ada@example.com" };
        var result = await Validator
            .For(model)
            .Rule(x => x.Email)
            .Server((_, _) => Task.FromResult<string?>(null))
            .ValidateAsync();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Sync_validate_skips_async_rules()
    {
        var model = new SignUpModel { Email = "ada@example.com" };
        var result = Validator
            .For(model)
            .Rule(x => x.Email)
            .Required()
            .Server((_, _) => Task.FromResult(ServerValidationResult.Fail("should not run")))
            .Validate();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void For_without_instance_requires_validate_argument()
    {
        var builder = Validator.For<SignUpModel>().Rule(x => x.Email).Required();

        Assert.Throws<InvalidOperationException>(() => builder.Validate());
        Assert.False(builder.Validate(new SignUpModel()).IsValid);
    }
}
