using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaPDF.Desktop.Views.Controls;

public sealed partial class NumberStepper : UserControl
{
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<NumberStepper, double>(
        nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumberStepper, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumberStepper, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> IncrementProperty =
        AvaloniaProperty.Register<NumberStepper, double>(nameof(Increment), 1);

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<NumberStepper, int>(nameof(DecimalPlaces), 2);

    public static readonly DirectProperty<NumberStepper, string> TextValueProperty =
        AvaloniaProperty.RegisterDirect<NumberStepper, string>(nameof(TextValue), control => control.TextValue,
            (control, value) => control.TextValue = value);

    private string _textValue = "0.00";
    private bool _updatingText;

    public NumberStepper() => InitializeComponent();

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public string TextValue
    {
        get => _textValue;
        set
        {
            if (SetAndRaise(TextValueProperty, ref _textValue, value) && !_updatingText)
            {
                ApplyText(value);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            UpdateText();
        }
        else if (change.Property == MinimumProperty || change.Property == MaximumProperty)
        {
            Value = Clamp(Value);
        }
    }

    private void OnIncreaseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Step(Increment);

    private void OnDecreaseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Step(-Increment);

    private void OnTextLostFocus(object? sender, RoutedEventArgs e) => UpdateText();

    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyText(TextValue);
            UpdateText();
            e.Handled = true;
        }
    }

    private void Step(double delta)
    {
        var places = Math.Clamp(DecimalPlaces, 0, 6);
        Value = Clamp(Math.Round(Value + delta, places));
    }

    private void ApplyText(string text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            if (double.IsFinite(value))
            {
                Value = Clamp(value);
            }
        }
    }

    private double Clamp(double value)
    {
        var maximum = Math.Max(Minimum, Maximum);
        return Math.Clamp(value, Minimum, maximum);
    }

    private void UpdateText()
    {
        if (_updatingText)
        {
            return;
        }

        _updatingText = true;
        var places = Math.Clamp(DecimalPlaces, 0, 6);
        TextValue = Value.ToString($"F{places}", CultureInfo.CurrentCulture);
        _updatingText = false;
    }
}
