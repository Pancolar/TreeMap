// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.ApplicationModel.Core;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Reflection.Metadata.Ecma335;
using Windows.Storage.BulkAccess;
using Microsoft.UI.Xaml.Shapes;
using ABI.Windows.Foundation;
using System.Diagnostics.Metrics;
using Windows.System;
using Windows.UI.Input.Inking.Analysis;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.ApplicationModel.Activation;
using Microsoft.UI;
using Windows.UI;
using System.Runtime.ConstrainedExecution;
using Windows.Media.Devices;
using Windows.Devices.Input;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Windows.UI.StartScreen;
using System.Threading;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using System.Xml.Linq;
using System.Timers;
using System.Reflection;
//using static TreeMap.Model.ExplorerItem;
using Windows.UI.Core;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection.PortableExecutable;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.UI.Xaml.Documents;
using static TreeMap.ExtensionMethods;
using Microsoft.UI.Input;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.Mvvm;
using Windows.ApplicationModel.Search;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.ViewManagement;
using Microsoft.VisualBasic.FileIO;
using Windows.Services.Maps;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Etier.IconHelper;
using CommunityToolkit.Mvvm.ComponentModel;
using TreeMap.Model;
using TreeMap.ViewModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Runtime.Intrinsics.X86;
using CommunityToolkit.Mvvm.Messaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TreeMap
{
    [ObservableObject]
    public sealed partial class MainPage : Page
    {
        private AppWindow m_AppWindow;

        public MainPageViewModel ViewModel { get; } = new MainPageViewModel();

        // GOES INTO MAIN VIEW MODEL#######################################################

        [RelayCommand]
        private void ShowSelectDirectoryDialog()
        {
            ShowDialogSelectDirectory();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateTreeViewAndGraphCommand))]
        string selectedDirectoryPath;

        [RelayCommand(CanExecute = nameof(CanUpdateTreeViewAndGraph))]
        private void UpdateTreeViewAndGraph()
        {
            Init();
        }
        private bool CanUpdateTreeViewAndGraph()
        {
            // TODO Not initializing the refresh button as disabled enables the button on app launch if the directory input field has a valid path. This should be decoupled.
            if (SelectedDirectoryPath != "" && SelectedDirectoryPath != null && Directory.Exists(SelectedDirectoryPath)) { return true; }
            else { return false; }            
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowInExplorerCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteSelectedItemCommand))]
        [NotifyCanExecuteChangedFor(nameof(ZoomInToSelectionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ZoomInCommand))]
        [NotifyCanExecuteChangedFor(nameof(ZoomOutCommand))]
        [NotifyCanExecuteChangedFor(nameof(ZoomOutFullyCommand))]
        ExplorerItem selectedItem;

        [RelayCommand(CanExecute = nameof(CanShowInExplorer))]
        private void ShowInExplorer()
        {
            if (SelectedItem.Type == ExplorerItem.ExplorerItemType.Folder)
            {
                System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.GetFullPath(SelectedItem.Path));
            }
            if (System.IO.File.Exists(SelectedItem.Path))
            {
                //Clean up file path so it can be navigated OK
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", System.IO.Path.GetFullPath(SelectedItem.Path)));
            }
        }
        private bool CanShowInExplorer()
        {
            if (SelectedItem != null) { return true; }
            else { return false; }
            //return false;
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedItem))]
        private async Task DeleteSelectedItem()
        {
            deletionName.Text = SelectedItem.Name;
            deletionPath.Text = SelectedItem.Path;
            var result = await DeleteConfirmation.ShowAsync();
            // SELECTION CONFIRMED
            if (result.ToString() == "Primary")
            {
                Debug.WriteLine("confirmed deletion");
                FileSystem.DeleteFile(SelectedItem.Path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            else
            {
                Debug.WriteLine("cancelled deletion");
            }
        }
        private bool CanDeleteSelectedItem()
        {
            if (SelectedItem != null) { return true; }
            else { return false; }
        }

        [RelayCommand(CanExecute = nameof(CanZoomInToSelection))]
        private void ZoomInToSelection()
        {
            if (SelectedItem.Type == ExplorerItem.ExplorerItemType.Folder)
            {
                GraphNavigationRedraw(SelectedItem);
            }
            else { GraphNavigationRedraw(SelectedItem.Parent); }
        }
        private bool CanZoomInToSelection()
        {
            if (SelectedItem == null) { return false; }
            if (SelectedItem.Path == GraphingDataInstance.GraphRootFolder.Path)
            {   // cannot zoom into graph root
                return false;
            }
            if (SelectedItem.Parent != null)
            {   // cannot zoom into file if its parent is graph root
                if (SelectedItem.Parent.Path == GraphingDataInstance.GraphRootFolder.Path && SelectedItem.Type == ExplorerItem.ExplorerItemType.File)
                {
                    return false;
                }
            }
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanZoomIn))]
        private void ZoomIn()
        {
            ExplorerItem currentFolder = SelectedItem;
            while (currentFolder.Parent != GraphingDataInstance.GraphRootFolder)
            {
                currentFolder = currentFolder.Parent;
            }
            GraphNavigationRedraw(currentFolder);
        }
        private bool CanZoomIn()
        {
            if (SelectedItem == null) { return false; }
            if (SelectedItem.Path == GraphingDataInstance.GraphRootFolder.Path)
            {   // cannot zoom into graph root
                return false;
            }
            if (SelectedItem.Parent != null)
            {   // cannot zoom into file if its parent is graph root
                if (SelectedItem.Parent.Path == GraphingDataInstance.GraphRootFolder.Path && SelectedItem.Type == ExplorerItem.ExplorerItemType.File)
                {
                    return false;
                }
            }
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanZoomOut))]
        private void ZoomOut()
        {
            GraphNavigationRedraw(GraphingDataInstance.GraphRootFolder.Parent);
        }
        private bool CanZoomOut()
        {
            if (SelectedItem == null) { return false; }
            if (GraphingDataInstance.GraphRootFolder.Path == DirectoryDataInstance.RootFolder.Path)
            {   // cannot zoom out of root folder
                return false;
            }
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanZoomOutFully))]
        private void ZoomOutFully()
        {
            GraphNavigationRedraw(DirectoryDataInstance.RootFolder);
        }
        private bool CanZoomOutFully()
        {
            if (SelectedItem == null) { return false; }
            if (GraphingDataInstance.GraphRootFolder.Path == DirectoryDataInstance.RootFolder.Path)
            {   // cannot zoom out of root folder
                return false;
            }
            return true;
        }



        DirectoryDataFetcher directoryDataFetcherInstance = new();
        //DirectoryData directoryData = new();
        //GraphingData graphingData = new();
        [ObservableProperty]
        DirectoryData directoryDataInstance;
        [ObservableProperty]
        GraphingData graphingDataInstance;
        [ObservableProperty]
        Selection selectionInstance;

        // ################################################################################

        private ObservableCollection<ExplorerItem> DataSource = new();
        //Microsoft.UI.Xaml.Shapes.Rectangle selectionRectangle;

        ObservableCollection<FileTypeInfo> colorizedFileTypes = new();

        //Bitmap bm;

        Settings settings = Settings.Instance;

        private readonly ExtensionMethods debouncer = new ExtensionMethods();
        ObservableCollection<DriveInfoView> drives = new();

        // Mirrorring selection in TreeView
        int currentDepth = 1;
        string handledPath;
        string[] splitPath;
        bool searchNodeFound = false;
        string currentPath;
        string searchPath;
        TreeViewNode parentNode;
        BringIntoViewOptions bivo = new()
        {
            AnimationDesired = true,
            VerticalAlignmentRatio = 0.0
        };
        //MainWindow _window;

        // View Model
        bool graphResized = false; // from resize start to resize end
        bool pointerDown = false;
        private bool gettingData = false;
        PointerPointProperties pointerProperties;
        public bool GettingData
        {
            get
            {
                return this.gettingData;
            }

            set
            {
                if (value != this.gettingData)
                {
                    this.gettingData = value;
                    NotifyPropertyChanged();
                }
            }
        } // true when in GetData()

        // Model behavior
        const bool graph = true;
        const bool cushion = true;


        public MainPage()
        {
            Debug.WriteLine("Start of MainPage()");

            DataContext = ViewModel;
            GraphingDataInstance = new();
            GraphingDataInstance.PropertyChanged += GraphingData_PropertyChanged;
            DirectoryDataInstance = new();
            SelectionInstance = new();


            this.InitializeComponent();
            m_AppWindow = GetAppWindowForCurrentWindow();

            //_window = window;
            // LOAD SETTINGS
            settings.Load();

            // see AppHasLoadedEvent(), Init()
            Debug.WriteLine("End of MainPage()");
        }

        //public void benchmark(byte runs)
        //{
        //    List<TimeSpan> timesPerRun = new();
        //    Stopwatch sw = Stopwatch.StartNew();
        //    for (int i = 0; i < runs; i++)
        //    {
        //        Init();
        //        timesPerRun.Add(sw.Elapsed);
        //        sw.Reset();
        //        sw.Start();
        //    }
        //    double doubleAverageTicks = timesPerRun.Average(timeSpan => timeSpan.Ticks);
        //    long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
        //    Debug.WriteLine("Average time for " + runs + " runs: " + new TimeSpan(longAverageTicks) + " (Path: " + SelectedDirectoryPath + ")");
        //}

        public async void Init()
        {

            Debug.WriteLine("Initialize...");
            if (SelectedDirectoryPath != "" && SelectedDirectoryPath != null && Directory.Exists(SelectedDirectoryPath))
            {
                Reset();
                GettingData = true; //for loading ring

                DirectoryDataInstance = await Task.Run(() => directoryDataFetcherInstance.GetData(SelectedDirectoryPath));
                GettingData = false;

                await Task.Run(() => DirectoryDataFetcher.OrderExtensionsBySize(DirectoryDataInstance));

                IconsAndColorsManager.FetchIcons(DirectoryDataInstance);
                await Task.Run(() => IconsAndColorsManager.MapToExplorerItems(DirectoryDataInstance));

                // SETTING UP TREE VIEW WITH DATA
                DataSource.Add(DirectoryDataInstance.RootFolder);

                Graphing.FileTypeBarInit(DirectoryDataInstance, colorizedFileTypes);
                GraphingDataInstance.GraphRootFolder = DirectoryDataInstance.RootFolder;
                Graphing.GraphInit(GraphingDataInstance, DirectoryDataInstance, GraphGrid.ActualHeight, GraphGrid.ActualWidth);
                //DrawSelectionRectangle(SelectedItem);
            }
        }

        public void Reset()
        {
            // XAML UI Elements
            DataSource.Clear();
            image.Source = null;

            //TEST
            GraphingDataInstance = new();
            GraphingDataInstance.PropertyChanged += GraphingData_PropertyChanged;
            DirectoryDataInstance = new();
            SelectionInstance = new();

            colorizedFileTypes.Clear();
            SelectedItem = null;
            //selectionRectangleCanvas.Children.Clear();
            pointerProperties = null;
            PathPointedTextBlock.Text = "";
        }

        private void GraphingData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GraphingDataInstance.GraphRootFolder))
            {
                ZoomInToSelectionCommand.NotifyCanExecuteChanged();
                ZoomInCommand.NotifyCanExecuteChanged();
                ZoomOutCommand.NotifyCanExecuteChanged();
                ZoomOutFullyCommand.NotifyCanExecuteChanged();
            }
            if (e.PropertyName == nameof(GraphingDataInstance.IsGraphing))
            {
                Debug.WriteLine("isGraphing changed");
                if (!GraphingDataInstance.IsGraphing)
                {
                    Graphing.DrawSelectionRectangle(SelectedItem, SelectionInstance);
                }
            }
        }

        //############################################################################################//
        //#########################################  GRAPH  ##########################################//
        //############################################################################################//

        //private void DrawSelectionRectangle(ExplorerItem tempSelectedItem)
        //{
        //    if (selectionRectangle != null)
        //    {
        //        selectionRectangleCanvas.Children.Remove(selectionRectangle);
        //    }
        //    if (i != null)
        //    {
        //        if (i.Height < 10 || i.Width < 10)
        //        {
        //            byte stroke = 2;
        //            selectionRectangle = new()
        //            {
        //                Stroke = new SolidColorBrush(Colors.White),
        //                StrokeThickness = stroke,
        //                Height = (int)i.Height + stroke * 2,
        //                Width = (int)i.Width + stroke * 2
        //            };
        //            Canvas.SetLeft(selectionRectangle, (int)i.Left - stroke);
        //            Canvas.SetTop(selectionRectangle, (int)i.Top - stroke);
        //            selectionRectangleCanvas.Children.Add(selectionRectangle);
        //        }
        //        else
        //        {
        //            selectionRectangle = new()
        //            {
        //                Stroke = new SolidColorBrush(Colors.White),
        //                StrokeThickness = 2,
        //                Height = (int)i.Height,
        //                Width = (int)i.Width
        //            };
        //            Canvas.SetLeft(selectionRectangle, (int)i.Left);
        //            Canvas.SetTop(selectionRectangle, (int)i.Top);
        //            selectionRectangleCanvas.Children.Add(selectionRectangle);
        //        }
        //    }
        //}
        //private static void DrawSelectionRectangle(ExplorerItem tempSelectedItem, Selection selectionInstance)
        //{
        //    if (tempSelectedItem.Height < 10 || tempSelectedItem.Width < 10)
        //    {
        //        //TODO Replace 2 with stroke variable
        //        selectionInstance.RectangleCollection.Add(new SelectionRectangle(
        //            (int)tempSelectedItem.Height + 2 * 2, 
        //            (int)tempSelectedItem.Width + 2 * 2,
        //            (int)tempSelectedItem.Top - 2,
        //            (int)tempSelectedItem.Left - 2));
        //    }
        //    else
        //    {
        //        //TODO Replace 2 with stroke variable
        //        selectionInstance.RectangleCollection.Add(new SelectionRectangle(
        //            (int)tempSelectedItem.Height,
        //            (int)tempSelectedItem.Width,
        //            (int)tempSelectedItem.Top,
        //            (int)tempSelectedItem.Left));
        //    }
        //}

        //############################################################################################//
        //########################################  HELPERS  #########################################//
        //############################################################################################//

        private string CleanPath(string path)
        {
            // "C:" doesnt work so it has to be returned as "C:\"
            if (path[path.Length - 1] == ':') //case: "C:"
            {
                return path += @"\";
            }
            else if (new DirectoryInfo(path).Root.FullName == path) // case: "C:\"
            {
                return path;
            }
            else if (path[path.Length - 1] == '\\')
            {
                return path.Remove(path.Length - 1);
            }
            return path;
        }

        private ExplorerItem FindItemByPointer(PointerRoutedEventArgs e)
        {
            if (GraphingDataInstance.GraphRootFolder != null) // && !GraphingDataInstance.GraphInvalid was here as well
            {
                double pointerX = e.GetCurrentPoint(GraphGrid).Position.X;
                double pointerY = e.GetCurrentPoint(GraphGrid).Position.Y;

                ObservableCollection<ExplorerItem> pointedFile = null;
                ExplorerItem searchFolder = GraphingDataInstance.GraphRootFolder;
                bool isFile = false;
                while (!isFile)
                {
                    pointedFile = new(searchFolder.Children.Where(Children => Children.Top < pointerY && Children.Top + Children.Height > pointerY && Children.Left < pointerX && Children.Left + Children.Width > pointerX));
                    if (pointedFile.Count != 0)
                    {
                        if (pointedFile.First().Type == ExplorerItem.ExplorerItemType.File)
                        {
                            // FILE FOUND
                            isFile = true;
                        }
                        else
                        {
                            // NO FILE FOUND YET
                            searchFolder = pointedFile.First();
                        }
                    }
                    else
                    {
                        isFile = true;
                    }
                }
                if (pointedFile != null && pointedFile.Count != 0)
                {
                    return pointedFile.First();
                }
            }
            return null;
        }

        public void ExpandNode()
        {
            //Debug.WriteLine("looking for: " + searchPath);
            //Debug.WriteLine(currentDepth + " / " + splitPath.Length);
            bool bottomReached = false;
            if (currentDepth == splitPath.Length - 1)
            {
                Debug.WriteLine("bottom reached... " + splitPath[splitPath.Length - 1]);
                bottomReached = true;
            }
            if (parentNode.IsExpanded)
            {
                //Debug.WriteLine(splitPath[currentDepth-1] + " (expanded.)");
                IList<TreeViewNode> children = parentNode.Children;
                foreach (TreeViewNode childNode in children)
                {
                    ExplorerItem childNodeContent = childNode.Content as ExplorerItem;
                    string itemPath = childNodeContent.Path;
                    if (itemPath == searchPath) // if searched child was found
                    {
                        if (bottomReached)
                        {
                            Debug.WriteLine((childNode.Content as ExplorerItem).Name);
                            // select file or folder
                            mainTreeView.SelectedNode = childNode;
                            // move the parent folder of the selected item into view
                            ((mainTreeView.ContainerFromNode(childNode.Parent) as TreeViewItem).Content as Grid).StartBringIntoView(bivo);
                            // this moves the item into view if it remains invisible due to being too deep in the parent folder.
                            ((mainTreeView.ContainerFromNode(childNode) as TreeViewItem).Content as Grid).StartBringIntoView();
                            ResetSelectionVars();
                            break;
                        }
                        currentDepth++;
                        currentPath = searchPath;
                        searchPath = currentPath + "\\" + splitPath[currentDepth];
                        parentNode = childNode;
                        ExpandNode();
                        break;
                    }
                }
            }
            else
            {
                //Debug.WriteLine(splitPath[currentDepth-1] + " (expanding...)");
                //Debug.WriteLine((parentNode.Content as ExplorerItem).Name);
                parentNode.IsExpanded = true;
            }
        }

        public void ResetSelectionVars()
        {
            Debug.WriteLine("selection vars reset");
            splitPath = null;
            currentDepth = 1;
            handledPath = null;
            parentNode = null;
            searchNodeFound = false;
            currentPath = null;
            searchPath = null;
        }

        //############################################################################################//
        //###################################### EVENT HANDLERS  #####################################//
        //############################################################################################//

        // KEY PRESSES

        public void Esc_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            Debug.Write("Key up");
            if (e.Key == VirtualKey.Escape)
            {
                SelectionHandler(null, null, null);
            }
        }


        // DIRECTORY SELECTION DIALOG
        private async void BrowseForDirectory_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            //var window = (Microsoft.UI.Xaml.Application.Current as App)?.m_window as MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            //var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folderLocal = await folderPicker.PickSingleFolderAsync();
            if (folderLocal != null)
            {
                SetDirectoryAndEnableButton(folderLocal.Path);
            }
        }

        private async void ShowDialogSelectDirectory()
        {
            drivesListView.ItemsSource = null;
            //SetDirectoryAndEnableButton();
            Task.Run(() => GetDriveInfo());

            var result = await SelectDirectories.ShowAsync();
            // SELECTION CONFIRMED
            if (result.ToString() == "Primary")
            {
                if (new DirectoryInfo(SelectedDirectoryPath).Root.FullName != CleanPath(SelectedDirectoryPath))
                {   // Only save path if it is not drive root.
                    settings.Save("recentDirectories", SelectedDirectoryPath);
                }
                Init();
            }
        }

        public void GetDriveInfo()
        {
            drives.Clear();
            DriveInfo[] drivesDriveInfo = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drivesDriveInfo)
            {
                DriveInfoView div = new();
                div.Name = drive.VolumeLabel;
                div.Path = drive.Name;
                div.TotalSize = Converters.BytesFormatter(drive.TotalSize);
                div.FillPercentage = (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize;
                drives.Add(div);
            }
            DispatcherQueue.TryEnqueue(() =>
            {
                drivesListView.Items.Clear();
                drivesListView.ItemsSource = drives;
            });
        }

        private void AppHasLoadedEvent(object sender, RoutedEventArgs e)
        {
            if (DirectoryDataInstance.RootFolder == null)
            {
                ShowDialogSelectDirectory();
            }
        }

        private void SelectedDirectory_TextChanged(object sender, RoutedEventArgs e)
        {
            FolderRadioButton.IsChecked = true;
            SetDirectoryAndEnableButton();
        }

        private void SetDirectoryAndEnableButton()
        {
            // Set from ComboBox to Model
            string t;
            if (SelectedDirectoryComboBox.Text != "") { t = SelectedDirectoryComboBox.Text; }
            else if (settings.recentDirectories.Count == 0) { t = ""; } // s.recentDirectories == null || 
            else { t = settings.recentDirectories.First(); }

            if (Directory.Exists(t))
            {
                t = CleanPath(t);
                SelectDirectories.IsPrimaryButtonEnabled = true;
                SelectedDirectoryPath = t;
            }
            else
            {
                SelectDirectories.IsPrimaryButtonEnabled = false;
                SelectedDirectoryPath = "";
            }
        }

        private void SetDirectoryAndEnableButton(string path)
        {
            // Set from Model (Folder chosen in File Explorer) to ComboBox
            if (Directory.Exists(path))
            {
                path = CleanPath(path);
                SelectDirectories.IsPrimaryButtonEnabled = true;
                SelectedDirectoryComboBox.Text = path;
                SelectedDirectoryPath = path;
            }
        }

        private void FolderRadioButton_Click(object sender, RoutedEventArgs e)
        {
            SetDirectoryAndEnableButton();
        }

        private void DriveRadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (drivesListView.SelectedItem != null)
            {
                SelectDirectories.IsPrimaryButtonEnabled = true;
                SelectedDirectoryPath = (drivesListView.SelectedItem as DriveInfoView).Path;
            }
            else
            {
                drivesListView.SelectedItem = drivesListView.Items.First();
                SelectDirectories.IsPrimaryButtonEnabled = true;
                SelectedDirectoryPath = (drivesListView.SelectedItem as DriveInfoView).Path;
            }
        }

        private void DriveRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            drivesListView.ItemsSource = null;
            Task.Run(() => GetDriveInfo());
        }

        private void drivesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                DriveRadioButton.IsChecked = true;
                SelectDirectories.IsPrimaryButtonEnabled = true;
                SelectedDirectoryPath = (e.AddedItems.First() as DriveInfoView).Path;
            }
            else if (drivesListView.SelectedItem == null)
            {
                if ((bool)DriveRadioButton.IsChecked)
                {
                    SelectDirectories.IsPrimaryButtonEnabled = false;
                }
            }
        }

        // MAIN CONTROL CLICKS
        
        private void GraphNavigationRedraw(ExplorerItem newGraphRootFolder)
        {
            GraphingDataInstance.GraphRootFolder = newGraphRootFolder;
            Graphing.GraphInit(GraphingDataInstance, DirectoryDataInstance, GraphGrid.ActualHeight, GraphGrid.ActualWidth);
            //UpdateAppBarButtons();
        }

        private void AppBarSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage), settings);
        }

        private async void CopySelectedPathButton_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(SelectedItem.Path);
            Clipboard.SetContent(package);

            CopySelectedPathButton.Content = new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\ue73e"
            };
            await Task.Run(() => Thread.Sleep(1500));
            CopySelectedPathButton.Content = new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\xE8C8"
            };
        }

        // POINTER CAPTURE
        private void GraphGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ExplorerItem ItemPointedTo = FindItemByPointer(e);
            if (ItemPointedTo != null)
            {
                PathPointedTextBlock.Text = ItemPointedTo.Path;
            }
        }

        private void GraphGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SelectionHandler(sender, e, FindItemByPointer(e));
        }

        private void GraphGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ptrPt = e.GetCurrentPoint(GraphGrid);
            pointerProperties = ptrPt.Properties;
        }

        private void GraphGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            UpdatePathPointedTextBlock();
        }

        // SELECTION
        private void mainTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            SelectionHandler(sender, null, args.InvokedItem as ExplorerItem);
        }

        public void SelectionHandler(object sender, PointerRoutedEventArgs e, ExplorerItem clickedItem)
        {
            if (GraphingDataInstance.GraphRootFolder != null && clickedItem != null)
            {
                if (sender is TreeView)
                {
                    // CLICK IN TREEVIEW
                    SelectedItem = clickedItem;
                    PathPointedTextBlock.Text = "Selected: \"" + SelectedItem.Path + "\"";

                    // check if selected item is parent of GraphRootFolder
                    ExplorerItem currentFolder = SelectedItem;
                    bool itemIsCurrentlyDrawn = false;
                    while (currentFolder != null)
                    {
                        if (currentFolder.Path == GraphingDataInstance.GraphRootFolder.Path)
                        {   // this means the selected item is inside the GraphRootFolder 
                            itemIsCurrentlyDrawn = true;
                        }
                        currentFolder = currentFolder.Parent;
                    }
                    if (!itemIsCurrentlyDrawn)
                    {
                        GraphingDataInstance.GraphInvalid = true;
                        GraphNavigationRedraw(DirectoryDataInstance.RootFolder);
                    }
                }
                else if (pointerProperties != null)
                {
                    // CLICK IN GRAPH
                    if (pointerProperties.IsLeftButtonPressed)
                    {   // Select file
                        SelectedItem = clickedItem;
                    }
                    else if (pointerProperties.IsMiddleButtonPressed)
                    {   // Open Explorer at selected item
                        if (SelectedItem != null && SelectedItem != GraphingDataInstance.GraphRootFolder)
                        {
                            //AppBarOpenExplorerHere_Click(null, null);
                            ShowInExplorer();
                        }
                    }
                    else if (pointerProperties.IsRightButtonPressed)
                    {   // Select parent folder of selected item
                        if (SelectedItem != null && SelectedItem != GraphingDataInstance.GraphRootFolder)
                        {
                            SelectedItem = SelectedItem.Parent;
                        }
                    }

                    // TREEVIEW MIRRORS SELECTION IN GRAPH
                    if (settings.MirrorSelectionInTreeView)
                    {
                        ResetSelectionVars();
                        string relativePath = SelectedItem.Path.Replace(DirectoryDataInstance.RootFolder.Path, "");
                        splitPath = relativePath.Split("\\");
                        parentNode = mainTreeView.NodeFromContainer(mainTreeView.ContainerFromItem(DirectoryDataInstance.RootFolder));
                        currentPath = DirectoryDataInstance.RootFolder.Path;
                        if (splitPath.Length > 1)
                        {
                            searchPath = currentPath + "\\" + splitPath[currentDepth];
                            ExpandNode();
                        }
                        else
                        {
                            mainTreeView.SelectedNode = parentNode;
                            ((mainTreeView.ContainerFromNode(parentNode) as TreeViewItem).Content as Grid).StartBringIntoView(bivo);
                        }
                    }
                }
            }
            else // if clickedItem == null
            {
                PathPointedTextBlock.Text = "";
                mainTreeView.SelectedItem = null;
                SelectedItem = null;
            }
            Debug.WriteLine(SelectedItem.Path);
            Debug.WriteLine(SelectionInstance != null);
            Graphing.DrawSelectionRectangle(SelectedItem, SelectionInstance);
            //UpdateAppBarButtons();
            UpdatePathPointedTextBlock();
        }

        private void UpdatePathPointedTextBlock()
        {
            if (SelectedItem != null) { PathPointedTextBlock.Text = "Selected: \"" + SelectedItem.Path + "\""; }
            else { PathPointedTextBlock.Text = ""; }
        }

        private void TreeViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (searchNodeFound) { return; }
            if (args.NewValue is ExplorerItem)
            {
                if (((ExplorerItem)args.NewValue).Path == searchPath)
                {
                    //TODO, this is not updated if the whole tree down to the selected item is already expanded...
                    //bringIntoViewPanel = (sender as TreeViewItem).Content as RelativePanel;
                    searchNodeFound = true;
                }
            }
        }

        private async void TreeViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            // Event triggers once for every child in a folder.
            // The first time it fires, ExpandNode() is called. This sets the parent path to "handled".
            if (parentNode != null && currentPath != handledPath)
            {
                handledPath = currentPath;
                await Task.Run(() => Thread.Sleep(10));
                ExpandNode();
            }
            searchNodeFound = false;
        }

        // WINDOWING
        private void GraphGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GraphingDataInstance == null) { return; }
            if (DirectoryDataInstance.RootFolder != null && image.Source != null)
            {
                if (GraphingDataInstance.GraphRootFolder.Width != 0)
                {
                    GraphingDataInstance.GraphInvalid = true;
                }
            }
            if (pointerDown)
            {
                // GraphInit() is called in pointerUp()
                graphResized = true;
            }
            else
            {
                debouncer.DebounceAsync((cancellationToken) =>
                {
                    Graphing.GraphInit(GraphingDataInstance, DirectoryDataInstance, GraphGrid.ActualHeight, GraphGrid.ActualWidth);
                    SetDragRegionForCustomTitleBar();
                });
                //GraphInit();
            }
        }

        public void gridSplitterPointerUp(object sender, PointerRoutedEventArgs e)
        {
            pointerDown = false;
            ((GridSplitter)sender).ReleasePointerCapture(e.Pointer);
            if (graphResized)
            {
                settings.Save("gridWidths", "");
                SetDragRegionForCustomTitleBar();
                Graphing.GraphInit(GraphingDataInstance, DirectoryDataInstance, GraphGrid.ActualHeight, GraphGrid.ActualWidth);
                graphResized = false;
            }
        }

        public void gridSplitterPointerDown(object sender, PointerRoutedEventArgs e)
        {
            pointerDown = true;
            ((GridSplitter)sender).CapturePointer(e.Pointer);
        }

        public void TreeViewItem_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RelativePanel rp = sender as RelativePanel;
            TextBlock tb = rp.Children.First() as TextBlock;
            tb.Width = rp.ActualWidth - 190;
        }

        // HELPER
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AppBar_Loaded(object sender, RoutedEventArgs e)
        {
            SetDragRegionForCustomTitleBar();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            SelectionHandler(null, null, null);
        }

        public void SetDragRegionForCustomTitleBar()
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
                && m_AppWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = GetScaleAdjustment();
                this.RightPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.RightInset / scaleAdjustment); // / scaleAdjustment
                this.LeftPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.LeftInset / scaleAdjustment);

                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRect1;
                dragRect1.X = (int)((this.LeftPaddingColumn.ActualWidth) * scaleAdjustment);
                dragRect1.Y = 0;
                dragRect1.Height = (int)(this.AppBar.ActualHeight * scaleAdjustment);
                dragRect1.Width = (int)((this.ImageDragRegion.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRect1);

                Windows.Graphics.RectInt32 dragRect2;
                dragRect2.X = (int)((this.LeftPaddingColumn.ActualWidth
                                    + this.ImageDragRegion.ActualWidth
                                    + this.TreeViewCommands.ActualWidth) * scaleAdjustment);
                dragRect2.Y = 0;
                dragRect2.Height = (int)(this.AppBar.ActualHeight * scaleAdjustment);
                dragRect2.Width = (int)(this.TreeViewDragRegion.ActualWidth * scaleAdjustment);
                dragRectsList.Add(dragRect2);

                Windows.Graphics.RectInt32 dragRect3;
                dragRect3.X = (int)((this.LeftPaddingColumn.ActualWidth
                                    + this.ImageDragRegion.ActualWidth
                                    + this.TreeViewCommands.ActualWidth
                                    + this.TreeViewDragRegion.ActualWidth
                                    + this.GraphCommands.ActualWidth) * scaleAdjustment);
                dragRect3.Y = 0;
                dragRect3.Height = (int)(this.AppBar.ActualHeight * scaleAdjustment);
                dragRect3.Width = (int)(this.GraphDragRegion.ActualWidth * scaleAdjustment);
                dragRectsList.Add(dragRect3);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                m_AppWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }
        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(App.Window);
            Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);
        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(App.Window);
            Microsoft.UI.WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }
    }

    class ExtensionMethods
    {
        private CancellationTokenSource debounceToken = null;

        public async void DebounceAsync(Action<CancellationToken> func, int milliseconds = 200)
        {
            try
            {
                // Cancel previous task
                if (debounceToken != null) { debounceToken.Cancel(); }
                try
                {
                    debounceToken?.Dispose();
                }
                catch { }

                // Assign new token
                debounceToken = new CancellationTokenSource();

                // Debounce delay
                await Task.Delay(milliseconds, debounceToken.Token);

                // Throw if canceled
                //debounceToken.Token.ThrowIfCancellationRequested();

                // Run function
                func(debounceToken.Token);
            }
            catch (TaskCanceledException) { }
        }
    }

    public class Converters
    {
        public static string BytesFormatter(long bytes)
        {
            if (bytes < 1000)
            {
                return bytes + " B";
            }
            else if (bytes < 1000000)
            {
                return FormatDoubleToNPlaces((float)bytes / 1000) + " kB";
            }
            else if (bytes < 1000000000)
            {
                return FormatDoubleToNPlaces((float)bytes / 1000000) + " MB";
            }
            else if (bytes < 1000000000000)
            {
                return FormatDoubleToNPlaces((float)bytes / 1000000000) + " GB";
            }
            else
            {
                return FormatDoubleToNPlaces((float)bytes / 1000000000000) + " TB";
            }
        }

        public static string PercentageFormatter(double percentage)
        {
            return FormatDoubleToNPlaces(percentage * 100) + "%";
        }

        private static string FormatDoubleToNPlaces(double number)
        {
            if (number >= 10)
            {
                return Math.Round(number, 1).ToString("0.0");
            }
            if (number >= 1)
            {
                return Math.Round(number, 1).ToString("0.00");
            }
            if (number >= 1)
            {
                return Math.Round(number, 1).ToString("0.00");
            }
            if (number >= 0.1)
            {
                return Math.Round(number, 2).ToString("0.00");
            }
            else if (number >= 0.01)
            {
                return Math.Round(number, 2).ToString("0.00");
            }
            else
            {
                return "0.00";
            }
        }

        public static Visibility BoolInverter(bool b)
        {
            if (b) { return Visibility.Collapsed; }
            else { return Visibility.Visible; }
        }

        public static bool CopySelectedPathButtonEnabled(ExplorerItem SelectedItem)
        {
            if (SelectedItem != null)
            {
                return true;
            }
            return false;
        }
    }

    internal class FileTypeInfo
    {
        public FileExtensionAndSize fileExtensionAndSize { get; set; }
        public BitmapImage bitmapImage { get; set; }
    }

    public class DriveInfoView
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string TotalSize { get; set; }
        public double FillPercentage { get; set; }
    }

    public class ItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            DataTemplate entryType = FolderTemplate;
            var explorerItem = (ExplorerItem)item;
            return explorerItem.Type == ExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
        }
    }

    public class GraphItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            DataTemplate entryType = FolderTemplate;
            var explorerItem = (ExplorerItem)item;
            return explorerItem.Type == ExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
        }
    }
}

class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    struct DispatcherQueueOptions
    {
        internal int dwSize;
        internal int threadType;
        internal int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

    object m_dispatcherQueueController = null;
    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
        {
            // one already exists, so we'll just use it.
            return;
        }

        if (m_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;    // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
        }
    }
}

