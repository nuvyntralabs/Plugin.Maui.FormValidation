namespace Plugin.Maui.FormValidation.Tests;

public sealed class ValidatableViewModelTests
{
    [Fact]
    public async Task ValidateAsync_populates_INotifyDataErrorInfo()
    {
        var viewModel = new SignUpViewModel();

        var result = await viewModel.ValidateAsync();

        Assert.False(result.IsValid);
        Assert.True(viewModel.HasErrors);
        Assert.Contains("required", viewModel.GetErrors(nameof(SignUpViewModel.Email)).Cast<string>().First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetProperty_does_not_validate_until_touched()
    {
        var viewModel = new SignUpViewModel();
        var raised = false;
        viewModel.ErrorsChanged += (_, _) => raised = true;

        viewModel.Email = "not-an-email";

        Assert.False(raised);
        Assert.False(viewModel.HasErrors);

        await viewModel.ValidatePropertyAsync(nameof(SignUpViewModel.Email));
        Assert.True(viewModel.HasErrors);

        var cleared = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.ErrorsChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SignUpViewModel.Email) && !viewModel.HasErrors)
            {
                cleared.TrySetResult(true);
            }
        };

        viewModel.Email = "ada@example.com";
        await cleared.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public async Task Conditional_company_name_validates_only_for_business()
    {
        var viewModel = new SignUpViewModel
        {
            Email = "ada@example.com",
            IsBusiness = false
        };

        var result = await viewModel.ValidateAsync();
        Assert.True(result.IsValid);

        viewModel.IsBusiness = true;
        result = await viewModel.ValidateAsync();
        Assert.False(result.IsValid);
        Assert.NotNull(result.FirstError(nameof(SignUpViewModel.CompanyName)));
    }

    [Fact]
    public async Task ClearValidation_removes_errors()
    {
        var viewModel = new SignUpViewModel();
        await viewModel.ValidateAsync();
        Assert.True(viewModel.HasErrors);

        viewModel.ClearValidation();
        Assert.False(viewModel.HasErrors);
        Assert.True(viewModel.LastResult.IsValid);
    }
}
