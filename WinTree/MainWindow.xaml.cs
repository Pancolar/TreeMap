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
using System.Security.Cryptography.X509Certificates;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Microsoft.Graphics.Canvas.Text;
using CommunityToolkit.WinUI.UI.Helpers;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TreeMap
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        //TODO

        /* /// OPTIMIZATIONS
         * 
         * /// MVVM BUGS ///
         * O Refresh button is always enabled, because selectedDirectoryPath is (almost) never null.
         * O Pressing Esc doesnt deselect the selectedItem
         * O Disable drive radio button until drives have loaded in.
         * /// FEATURES ///
         * O Add "No Extension" text to file color blob when files without extension are among top extensions.
         * X Add bottom Bar with the file types and colors
         * O Refresh Views when file is deleted. Ideally, do not recalculate everything.
         * O Add "Free Up Space" Option to cloud files (https://stackoverflow.com/questions/56600252/onedrive-free-up-space-with-python)
         * O Treat files as their own subfolder, so as to group the block of files in the graph Then only sort the files by descending size
         * ~ Change Squarify Orientation with Orientation of Square to fill (Does changing the minSideRatio suffice and changing the comparison?)
         * O Display file/drive/extension icon (maybe calculate/get at the same time as most used extensions, so it only gets called once per file type.)
         *    - getting icon per file is too slow and would maybe return thumbnail <System.Drawing.Icon.ExtractAssociatedIcon(filePath)>
         *    - getting all icons once could be slow for huge directories
         *    - getting icons with PInvoke is complicated. <SHFILEINFO>
         * O If Window is in the background, notify about finishing the search.
         * O parallelize graphwin2D by locking parts of bitmap...
         * O Make the light in the graph dependent on position inside all upper directories.
         *      Idea for calculating the light: Transform coordinates of file in parent to file and invert. 
         *      (Bottom right in parent turns into top left in file)
         * O Add options to settings: Show percentage, show size or only show fill bar in treeview.
         * X When selecting, going up, update hover text box!
         * X Confirm Copy by (for a second?) changing text of selection bar. Or changing copy icon to check mark
         * X Give App Bar Buttons tool tips
         * O How to interrupt operation: (CancellationToken)
         *      Second resize of graph while graph is being drawn
         *      Changing indexed folder while another folder is indexing.
         *      O While scan is running, replace "Select Folder" Button with Cross to stop scan.
         * O Localize
         * O Possible to delete all local storage entries when resetting app?
         * /// STORE ///
         * X Is "Disk Space" a search term?
         * X "Disk Usage" Search term
         * O Update Store entry with mounted scannable network drives.
         * O Upload to GitHub and put link in Store listing.
         * /// BUGS ///   
         * O On Phillips PC, no drives appear in the list. selecting the radio button then crashes the app. 
         * O When changing folder, hovering over graph changes the hover text box.
         * X Pressing the copy button doesnt copy the selection. (but root folder?)
         * X Searching drive makes it show up in recent directories list.
         * X Settings check box has no background in dark mode.
         * X Settings have no background in light mode.
         *      Changing Theme Resource ContentDialogTopOverlay works, but only the first time the dialog is opened.
         * O Pressing ESC in a content dialog should not deselect.
         * O Graphing the first time seems slower than subsequent times.
         * X Be able to index recycle bin.
         * O Bigger MS Store icon
         * O Maximizing and minimizing before graph was redrawn has weird behavior.
         * X File extensions with different capitalization are not treated the same.
         */
        /* UNSUPPORTED BY WinUI3:
         * O give window a minimum size (Using WinUIEx works, but changes a lot of code. Maybe just wait for more WinUI3 development.
         * O Random Nodes are expanded when reloading or changing dir. 
         *      https://github.com/microsoft/microsoft-ui-xaml/issues/7044
         * O Mirroring selection in treeview is unreliable
         *    Sometimes, datacontextchanged events don't fire, making it impossible to listen to expansion...
         *    WHen a second folder is opened, or the current folder is reset, all breaks
         * O Browse Explorer Dialog doesnt open when run as administrator. (WinUI 3 problem?)
         * O Reduce indentation amount on treelistview
        */

        /* ///// DONE /////
         * OPTIMIZATIONS
         * X Only populate tree when it is expanded ?
         * X Only expand the root folder
         * X virtualize treeview         
         * X Improve performance by only counting filesizes of files in directory instead of all subdirectories but including the sizes of the folders. (this way, each file is only queried once.
         * X Prallelize Folder and File searching
         * X Only redraw when window is let go.
         * X Make showing graph selection in treeview faster
         * FEATURES
         * X FileSize of files
         * X Filesize of folders
         * X Percentage is relative to rootFolder, not parent folder
         * X Sort files by size in the list (LINQ sort list = list.OrderByDescending(x => x.size).ToList();
         * X Graph folder and file sizes
         * X filesize percent
         * X display percentage as bar
         * X Change Canvas Size with viewport
         * X redraw Canvas when viewport size is changed by user
         * X Get selection from graph
         * X Open Explorer at location
         * X Grey Out Open Explorer Here button when no item is selected         
         * X Make the dir selection textbox editable (Two-Way binding)
         * X Color Rectangles based on file type
         * X Outline Folders (BAD IDEA)
         * X ACTUALLY Expand content into titlebar
         * X Use x:Bind for treeview
         * X Use x:Bind for graph
         * X Radial gradient brush
         * X Color Graph by file extensions
         * X Move Mainwindow code and xaml to Page
         * X Format FileSize, SizePercentage, FileIcon with converters? (prerequisite: above)
         * X give file extensions colors based on total file size, not count
         * X decouple graph interface (async?) to enable showing messages, loading rings, etc.
         * X Mirror selection in treeview and graph
         * X stretch rectangles while resizing, so it looks like it is dynamically adjusting.
         * X give ability to search drive (drive root: c:\)
         * X Show the file selection interface instead of empty graph when no dir has been selected
         * X Radialgradientbrush light should come from the middle of the folder a file is in.
         * X Make Log() write to debug console.
         * X Mica in the treeviewandgraph Page
         * X System Files can't be read... folderpicker doesnt open
         * X possible performance enhancement: Directory.Grtdirdctories returns IEnumerable, so it can be read before it finishes filling up...
         * X Move determining file color out of graph init, so it doesnt rerun those calculations every time. Maybe even save the ColorGradientBrush? Would not work with resizing though
         * X Settings Dialog to change the value of the constants
         * X Make the size of the two panes draggable (listview and graph)
         * X open directory select dialog when app has loaded.
         * X Populate DropDown list of recent paths in the content dialog. Make persistent.
         * X Don't change the selectedDirectoryPath every time the text box is changed, only if change is applied.
         * X In the settings, switch primary and secondary buttons.
         * X Windowing: When GridSplitter is dragged, only redraw graph when mouse up.
         * X Graph: draw the radial smaller if file area is smaller (Maybe even with the aspect ratio of the file.)
         * X In the settings, gray out apply button, unless file size setting has been changed.
         * X persist gripper setting
         * X Gray out graph when it is stretched (resizing)
         * X Draw Graph only when window resizing is done (cant capture pointer when outside window, which it is while resizing...)
         *    (solution: debounding the size-changed event)
         * X Make the last directory the selected one in the combobox, even if the last one wasnt the one that was at the top of the list.
         * X graph doesnt show as invalid between list being done and graph being done. (working as intended)
         * X Show working ring when graph is recalculating.
         * X Show working ring when getting data.
         * X Make right click selection select the parent folder.
         * X Make middle click selection open in file explorer
         * X In the settings, inform the user what middle and right on graph click do 
         * X decouple treeview from interface (async?) to enable showing messages, loading rings, etc.
         * X Zoom into folder or out to rootFolder in graph
         * X have right click continue moving up through parent folders...
         * X have a copy path option in the status bar.
         * X Make Items deletable and prompt for confirmation
         * X give user list of drives to choose from
         * X Limit length of text in treeview
         *    - No luck with: <TextBlock Text="{x:Bind Name}" Width="{Binding ElementName=panel, Path=ActualWidth, Converter={StaticResource textTrimmer}}"/>
         * X move selected treeviewitem into view (UIElement.StartBringIntoView())
         * X Not only expand the necessary folders in the treeview, but also select the corresponding item.
         * X Release 1.0:
         *      X Make app icon
         *      X pressing ESC clears selection
         *          Make deselection handler
         *      X appbar graph controls move with gridsplitter resize
         *          Also make sure that the grabregions of the window get updated.
         *          Implemented INotifyPropertyChanged
         *      X Fix all bugs
         *      X Zoom in only zooms in one level, make button to zoom in to item.
         *          Disable zoom in button if the selected item is child of graphrootfolder.
         * BUGS
         * X "C:", "C:\" cant be searched.
         * X Duplicates of paths get into recentDirectories
         * X recent files dont update immediately in combobox drop down.
         * X scanning Dokumente and then Programmieren seems to fuck up the extension colors
         * X Selection Rectangle doesnt get reset when new folder is selected.
         * X "Programmieren\C" cant be opened... (div0)
         * X Changing the folder breaks the graph
         * X Canvas doesnt seem to empty on resize, this is clear, when size is changed suddenly, such as when it is maximized
         * X Moving the gripper all the way to the right breaks the graphing.
         *      apparently happens when the first item fills the whole row and fails to have a side ratio < 0.4
         * X When resetting, selection rectangle renders until graph is redrawn
         * X When dragging window, selectionrectangle doesnt change size with graph
         * X Making a selection in the tree view does not change the app bar buttons state.
         * X Selecting the root in treeview and then zooming in activates the zoom out buttons.
         * X Opening TestFolder after Bidler und Videos throws error in bm (could not be reproduced)
         * X When mouse is pressed and then moved into graph
         * X ExpandNode throws an error when something is selected and you reload the graph
         * X moving up to root folder in the graph (right click) crashes.
         * X text stops being trimmed when lots of dirs are open.
         * X Length of text in treeview does not resize with window.
         * X Not the whole appbar shows when the window is small. (now scrollable)
         * X selecting in the treeview, when graph is zoomed draws the wrong selection rectangle.
         */

        private AppWindow m_AppWindow;
        //public Settings s = new Settings();

        public MainWindow()
        {
            this.Title = "TreeMap";
            this.InitializeComponent();
            m_AppWindow = GetAppWindowForCurrentWindow();

            // LISTEN TO THEME CHANGE (currently not possible, so the theme is corrected every time the window is activated.)
            this.Activated += WindowActivatedEvent;

            // TITLE BAR
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                m_AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;  // enable custom titlebar
                m_AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                m_AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                m_AppWindow.TitleBar.ButtonHoverBackgroundColor = (Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as SolidColorBrush).Color;
                m_AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
                m_AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                //SetTitleBar(twag.AppBar);
            }
            else
            {
                Windows10AppTitleBarRow.Height = new GridLength(28);
                m_AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                m_AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                m_AppWindow.TitleBar.ButtonHoverBackgroundColor = (Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as SolidColorBrush).Color;
                m_AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
                m_AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                Windows10AppTitleBar.Visibility = Visibility.Visible;
                //SetTitleBar(Windows10AppTitleBar);
            }

            m_AppWindow.SetIcon(System.IO.Path.Combine(Package.Current.InstalledLocation.Path, @"Assets\TreeMap Logo512x.ico"));
        }

        //public void Frame_KeyUp(object sender, KeyRoutedEventArgs e)
        //{
        //    Debug.Write("FrameKeyup");
        //    if (e.Key == VirtualKey.Escape)
        //    {
        //        // TODO
        //        //twag.SelectionHandler(null, null, null);
        //    }
        //}

        private void WindowActivatedEvent(object sender, WindowActivatedEventArgs e)
        {
            m_AppWindow.TitleBar.ButtonHoverBackgroundColor = (Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as SolidColorBrush).Color;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }
    }
}   