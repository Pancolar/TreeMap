using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    public partial class Selection : ObservableObject
    {
        public ObservableCollection<SelectionRectangle> RectangleCollection = new();
    }
}
