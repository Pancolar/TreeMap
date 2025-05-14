using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    public partial class GraphingData : ObservableObject
    {
        public Bitmap bm; // Used during drawing
        [ObservableProperty]
        private BitmapImage bmimg; // Bound to UI as ImageSource

        public double BitmapHeight;
        public double BitmapWidth;

        [ObservableProperty]
        private ExplorerItem graphRootFolder;

        //STATE
        [ObservableProperty]
        bool isGraphing = false;
        [ObservableProperty]
        bool graphInvalid = false;
    }
}
