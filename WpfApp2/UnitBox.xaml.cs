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
        // Change handling:
        // SourceValue changed (not null) - If source unit is not null then update target value by converting to target unit
        // SourceValue changed (null) - Set target value to null
        // SourceUnit changed (not null) - If source value is not null then update target value
        // SourceUnit changed (null) - Set target unit and available units to null
        // Difference for targets is that updating the unit will not change the source value (because when the user swaps unit in the UI we change the displayed (target) value instead)

        private static readonly ConcurrentDictionary<Type, IReadOnlyList<AbbreviatedUnit>> availableUnitsCache = new();

        public static readonly DependencyProperty SourceValueProperty = DependencyProperty.Register(
            "SourceValue", typeof(double?), typeof(UnitBox), 
            new PropertyMetadata(default(double?), SourceValueChanged));

        public static readonly DependencyProperty SourceUnitProperty = DependencyProperty.Register(
            "SourceUnit", typeof(Enum), typeof(UnitBox), 
            new PropertyMetadata(default(Enum), SourceUnitChanged));

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

        public double? SourceValue
        {
            get => (double?)GetValue(SourceValueProperty);
            set => SetValue(SourceValueProperty, value);
        }

        public Enum SourceUnit
        {
            get => (Enum)GetValue(SourceUnitProperty);
            set => SetValue(SourceUnitProperty, value);
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

        private static void SourceUnitChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => ExecuteExclusiveUpdate<Enum>(o, e, SourceUnitChangedSync);

        private static void SourceUnitChangedSync(UnitBox unitBox, Enum oldValue, Enum newValue)
        {
            if (newValue == null)
            {
                unitBox.TargetUnit = null;
                unitBox.AvailableUnits = null;
            }
            else
            {
                if (newValue.GetType() != unitBox.TargetUnit?.GetType())
                {
                    // It's important this is set before the target unit
                    unitBox.AvailableUnits = GetAbbreviatedUnitsFromCache(newValue.GetType());

                    // Unit/quantity type changed so no need to convert anything
                    unitBox.TargetUnit = newValue;
                    
                    // We have to set the values to match if the unit type has changed because we can't convert between differing unit types
                    unitBox.TargetValue = unitBox.SourceValue;
                }
                else
                {
                    // Just convert to the target unit, available units should already be configured
                    Debug.Assert(unitBox.TargetUnit != null);
                    Debug.Assert(unitBox.AvailableUnits != null);

                    if (unitBox.SourceValue is { } sourceValue)
                        unitBox.TargetValue = UnitConverter.Convert(sourceValue, newValue, unitBox.TargetUnit);
                }
            }
        }

        private static void SourceValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => ExecuteExclusiveUpdate<double?>(o, e, SourceValueChangedSync);

        private static void SourceValueChangedSync(UnitBox unitBox, double? oldValue, double? newValue)
        {
            if (newValue == null)
                unitBox.TargetValue = null;
            else if (unitBox.SourceUnit is { } sourceUnit)
            {
                Debug.Assert(unitBox.TargetUnit != null);
                unitBox.TargetValue = UnitConverter.Convert(newValue.Value, sourceUnit, unitBox.TargetUnit);
            }
        }

        private static void TargetValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => ExecuteExclusiveUpdate<double?>(o, e, TargetValueChangedSync);

        private static void TargetValueChangedSync(UnitBox unitBox, double? oldValue, double? newValue)
            => unitBox.UpdateSourceValue();

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

        private void UpdateSourceValue()
        {
            if (TargetUnit is not { } targetUnit)
                return;

            Debug.Assert(SourceUnit !=null);
            if (TargetValue is { } targetValue)
                SourceValue = UnitConverter.Convert(targetValue, targetUnit, SourceUnit);
            else
                SourceValue = null;
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