using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using TreeMap.Model;

namespace TreeMap
{
    // ClassA.cs
    //public partial class ClassA : ObservableRecipient
    //{
    //    [ObservableProperty]
    //    [NotifyPropertyChangedRecipients]
    //    private int someProperty;
    //}

    //// ClassB.cs
    //public partial class ClassB : ObservableRecipient
    //{

    //    private int someProperty2;
    //}
    // Assuming your ViewModelBase implements INotifyPropertyChanged

    public partial class InputAViewModel : ObservableRecipient
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private string inputA = string.Empty;
    }


    public partial class InputBViewModel : ObservableRecipient
    {
        //[ObservableProperty]
        //private string inputB = string.Empty;

        [RelayCommand(CanExecute = nameof(CanZoomIn))]
        private void ZoomIn()
        {
            //DO SOMETHING
        }
        private bool CanZoomIn()
        {
            return true;
        }

        public void Receive(PropertyChangedMessage<string> msg)
        {
            if (msg.Sender.GetType() == typeof(InputAViewModel) && msg.PropertyName == nameof(InputAViewModel.InputA))
            {
                ZoomInCommand.NotifyCanExecuteChanged();
            }
        }

        // Other properties and methods in InputBViewModel
    }
}
