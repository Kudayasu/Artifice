using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artifice
{
    public class StepViewModel : INotifyPropertyChanged
    {
        private int _stepIndex;
        public int StepIndex
        {
            get => _stepIndex;
            set
            {
                _stepIndex = value;
                OnPropertyChanged(nameof(StepIndex));
            }
        }

        public void NextStep()
        {
            StepIndex++;
        }

        public void PreviousStep()
        {
            StepIndex--;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
