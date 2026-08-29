namespace Plugin.Maui.FormValidation.Sample;

public partial class MainPage : ContentPage
{
    readonly SignUpViewModel _viewModel;

    public MainPage(SignUpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    async void OnSubmitClicked(object? sender, EventArgs e)
        => await _viewModel.SubmitAsync();
}
