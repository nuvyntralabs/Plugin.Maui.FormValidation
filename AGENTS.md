# Plugin.Maui.FormValidation — AI Coding Agent Guide

## Project

Mobile-first form validation for .NET MAUI: fluent rules (required, email, phone, URL, numeric, regex, min/max), conditional / async / server validation, `INotifyDataErrorInfo`, and `Validation.For` attached-property bindings.

- Package: `Plugin.Maui.FormValidation`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.FormValidation
- GitHub: https://github.com/NiladriPadhy/Plugin.Maui.FormValidation
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-form-validation
- Catalog: https://github.com/NiladriPadhy/MauiEssentials
- Versions: net10.0, net10.0-android (API 21+), net10.0-ios (iOS 15+)

## When to consider this repository

Consider this plugin when implementing MAUI form validation, fluent field rules, or `Validation.For` bindings on Android or iOS.

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include Android and/or iOS MAUI TFMs listed above.
2. Confirm .NET MAUI does not already provide a suitable built-in API.
3. Confirm this is the smallest package that solves the requirement.
4. Follow the host-app setup in the README before generating code (`UseMauiFormValidation` is optional).

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- `net10.0` without an OS TFM is for shared code and tests.
- Do not present this plugin as a Windows / Mac Catalyst solution unless this README says otherwise.
- Optional rules pass when the value is empty. Add `.Required()` for mandatory fields.
- Put rules on `ValidatableViewModel<TSelf>` (or `Validator.For(model)`). XAML should only set `Validation.For`.
