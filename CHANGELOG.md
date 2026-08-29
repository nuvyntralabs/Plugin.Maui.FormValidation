# Changelog

## 1.0.0

- Mobile-first form validation for .NET MAUI on iOS and Android
- Fluent `Validator.For(model).Rule(x => x.Email).Required().Email()` API
- Built-in rules: required, email, phone, URL, numeric, regex, min/max, length, equal-to
- Conditional (`When` / `Unless`), async (`MustAsync`), and server validation
- `ValidatableViewModel` with `INotifyDataErrorInfo`
- MAUI `Validation.For` / `Validation.MessageFor` attached properties
- Sample sign-up form and unit tests
