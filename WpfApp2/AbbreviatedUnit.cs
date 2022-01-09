using System;

namespace WpfApp2
{
    internal readonly struct AbbreviatedUnit
    {
        public AbbreviatedUnit(string abbreviation, Enum unit)
        {
            Abbreviation = abbreviation;
            Unit = unit;
        }

        public string Abbreviation { get; }

        public Enum Unit { get; }
    }
}