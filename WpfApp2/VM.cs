using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnitsNet.Units;
using WpfApp2.Annotations;

namespace WpfApp2
{
    public class VM : INotifyPropertyChanged
    {
        private double? value = 7.78;

        public double? Value
        {
            get => value;
            set
            {
                this.value = value;
                OnPropertyChanged();
            }
        }

        public LengthUnit Unit => LengthUnit.Meter;

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}