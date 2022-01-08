using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using UnitsNet;
using UnitsNet.Units;

namespace WpfApp2
{
    public partial class UnitBox
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<AbbreviatedUnit>> availableUnitsCache = new();

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(IQuantity), typeof(UnitBox),
            new PropertyMetadata(default(IQuantity), SourceChanged));

        public static readonly DependencyProperty TargetValueProperty = DependencyProperty.Register(
            "TargetValue", typeof(double?), typeof(UnitBox),
            new PropertyMetadata(default(double?), TargetValueChanged));

        public static readonly DependencyProperty TargetUnitProperty = DependencyProperty.Register(
            "TargetUnit", typeof(Enum), typeof(UnitBox),
            new PropertyMetadata(default(Enum), TargetUnitChanged));

        public static readonly DependencyProperty AvailableUnitsProperty = DependencyProperty.Register(
            "AvailableUnits", typeof(IReadOnlyList<AbbreviatedUnit>), typeof(UnitBox),
            new PropertyMetadata(default(IReadOnlyList<AbbreviatedUnit>)));

        private bool updating;

        public UnitBox()
        {
            InitializeComponent();
        }

        // This forces us to box the value unfortunately. We could introduce a basic struct to hold a value and unit instead but probably overkill here.
        public IQuantity Source
        {
            get => (IQuantity)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private double? TargetValue
        {
            get => (double?)GetValue(TargetValueProperty);
            set => SetValue(TargetValueProperty, value);
        }

        private Enum TargetUnit
        {
            get => (Enum)GetValue(TargetUnitProperty);
            set => SetValue(TargetUnitProperty, value);
        }

        private IReadOnlyList<AbbreviatedUnit> AvailableUnits
        {
            get => (IReadOnlyList<AbbreviatedUnit>)GetValue(AvailableUnitsProperty);
            set => SetValue(AvailableUnitsProperty, value);
        }

        private static void ExecuteExclusiveUpdate<T>(DependencyObject o, DependencyPropertyChangedEventArgs e,
            ValueChangedDelegate<T> callback)
        {
            UnitBox unitBox = (UnitBox)o;

            if (unitBox.updating)
                return;

            try
            {
                unitBox.updating = true;

                callback(unitBox, (T)e.OldValue, (T)e.NewValue);
            }
            finally
            {
                unitBox.updating = false;
            }
        }

        private static void SourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => ExecuteExclusiveUpdate<IQuantity>(o, e, SourceChangedSync);

        private static void SourceChangedSync(UnitBox unitBox, IQuantity oldValue, IQuantity newValue)
        {
            if (newValue == null)
            {
                unitBox.TargetUnit = null;
                unitBox.TargetValue = 0;
                unitBox.AvailableUnits = null;
            }
            else
            {
                if (newValue.Unit.GetType() != unitBox.TargetUnit?.GetType())
                {
                    // It's important this is set before the target unit
                    unitBox.AvailableUnits = GetAbbreviatedUnitsFromCache(newValue.Unit.GetType());
                    
                    // Unit/quantity type changed so no need to convert anything
                    unitBox.TargetUnit = newValue.Unit;
                    unitBox.TargetValue = newValue.Value;
                }
                else
                {
                    // Just convert to the target unit, available units should already be configured
                    Debug.Assert(unitBox.TargetUnit != null);
                    Debug.Assert(unitBox.AvailableUnits != null);
                    unitBox.TargetValue = newValue.As(unitBox.TargetUnit);
                }
            }
        }

        private static void TargetValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => ExecuteExclusiveUpdate<double?>(o, e, TargetValueChangedSync);

        private static void TargetValueChangedSync(UnitBox unitBox, double? oldValue, double? newValue)
            => unitBox.UpdateSource();

        private static void TargetUnitChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => ExecuteExclusiveUpdate<Enum>(o, e, TargetUnitChangedSync);

        private static void TargetUnitChangedSync(UnitBox unitBox, Enum oldValue, Enum newValue)
        {
            if (unitBox.TargetValue is { } targetValue &&
                oldValue != null &&
                newValue != null &&
                oldValue.GetType() == newValue.GetType())
                unitBox.TargetValue = UnitConverter.Convert(targetValue, oldValue, newValue);
        }

        private void UpdateSource()
        {
            if (TargetUnit is not { } targetUnit)
                return;

            if (TargetValue is { } targetValue)
                Source = Quantity.From(targetValue, targetUnit);
            else
                Source = null;
        }

        private static IReadOnlyList<AbbreviatedUnit> GetAbbreviatedUnitsFromCache(Type unitType)
            => availableUnitsCache.GetOrAdd(unitType, CalculateAbbreviatedUnits);

        private static IReadOnlyList<AbbreviatedUnit> CalculateAbbreviatedUnits(Type unitType)
        {
            if (unitType == typeof(LengthUnit))
                return LoadAbbreviations(LengthUnit.Meter,
                    LengthUnit.Millimeter,
                    LengthUnit.Foot,
                    LengthUnit.Inch);

            return LoadAbbreviations(Enum.GetValues(unitType).Cast<Enum>().ToArray());
        }

        private static IReadOnlyList<AbbreviatedUnit> LoadAbbreviations(params Enum[] values)
            => values.Select(v =>
                {
                    string abbreviation = UnitAbbreviationsCache.Default.GetDefaultAbbreviation(v.GetType(),
                        (int)(object)v,
                        CultureInfo.CurrentUICulture);

                    return new AbbreviatedUnit(abbreviation, v);
                }).OrderBy(v => v.Abbreviation)
                .ToList();

        private delegate void ValueChangedDelegate<in T>(UnitBox unitBox, T oldValue, T newValue);
    }
}