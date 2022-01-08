using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnitsNet;
using UnitsNet.Units;
using WpfApp2.Annotations;

namespace WpfApp2
{
    public class VM : INotifyPropertyChanged
    {
        private IQuantity? value = Length.FromMeters(7.78);

        public IQuantity? Value
        {
            get => value;
            set
            {
                this.value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}