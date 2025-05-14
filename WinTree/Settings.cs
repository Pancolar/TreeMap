using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TreeMap
{
    public class Settings : INotifyPropertyChanged
    {
        private static readonly Lazy<Settings> lazyInstance;

        static Settings()
        {
            lazyInstance = new Lazy<Settings>(() => new Settings());
        }

        public static Settings Instance => lazyInstance.Value;

        private Settings() { }


        public event PropertyChangedEventHandler PropertyChanged;
        public ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        // Runtime Variables
        public ObservableCollection<string> recentDirectories;
        private string gridWidthLeft;
        private string gridWidthRight;
        private bool useFileSizeOnDisk;
        private bool mirrorSelectionInTreeView;
        public string GridWidthLeft
        {
            get
            {
                return this.gridWidthLeft;
            }

            set
            {
                if (value != this.gridWidthLeft)
                {
                    this.gridWidthLeft = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string GridWidthRight
        {
            get
            {
                return this.gridWidthRight;
            }

            set
            {
                if (value != this.gridWidthRight)
                {
                    this.gridWidthRight = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool UseFileSizeOnDisk
        {
            get
            {
                return this.useFileSizeOnDisk;
            }

            set
            {
                if (value != this.useFileSizeOnDisk)
                {
                    this.useFileSizeOnDisk = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool MirrorSelectionInTreeView
        {
            get
            {
                return this.mirrorSelectionInTreeView;
            }

            set
            {
                if (value != this.mirrorSelectionInTreeView)
                {
                    this.mirrorSelectionInTreeView = value;
                    localSettings.Values["mirrorSelectionInTreeView"] = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public void Reset()
        {
            Debug.WriteLine("Settings reset.");

            localSettings.Values["fileSizeSetting"] = true;
            localSettings.Values["recentDirectories"] = new string[5];
            localSettings.Values["gridWidths"] = new string[] { "0.5*", "0.5*" };
            localSettings.Values["mirrorSelectionInTreeView"] = false;
            while (recentDirectories.Count > 0)
            {
                recentDirectories.RemoveAt(0);
            }
            Load();
        }

        public void Load()
        {
            // Initialize settings if the app is launched on a new installation.
            if (!localSettings.Values.ContainsKey("recentDirectories"))
            {
                Debug.WriteLine("recentDirectories initialized");
                localSettings.Values["recentDirectories"] = new string[5];
            }
            if (!localSettings.Values.ContainsKey("gridWidths"))
            {
                Debug.WriteLine("gridWidths initialized");
                localSettings.Values["gridWidths"] = new string[2] { "0.5*", "0.5*" };
            }
            if (!localSettings.Values.ContainsKey("fileSizeSetting"))
            {
                Debug.WriteLine("fileSizeSetting initialized");
                localSettings.Values["fileSizeSetting"] = true;
            }
            if (!localSettings.Values.ContainsKey("mirrorSelectionInTreeView"))
            {
                Debug.WriteLine("mirrorSelectionInTreeView initialized");
                localSettings.Values["mirrorSelectionInTreeView"] = false;
            }

            // Set runtime variables from settings file
            GridWidthLeft = (localSettings.Values["gridWidths"] as string[])[0];
            GridWidthRight = (localSettings.Values["gridWidths"] as string[])[1];
            useFileSizeOnDisk = (bool)localSettings.Values["fileSizeSetting"];
            MirrorSelectionInTreeView = (bool)localSettings.Values["mirrorSelectionInTreeView"];

            if (recentDirectories == null)
            {
                Debug.WriteLine("recentDirectories was null");
                recentDirectories = new ObservableCollection<string>((localSettings.Values["recentDirectories"] as string[]).ToList().Where(i => i != "").ToList());
            }
            Debug.WriteLine("Settings loaded.");
        }

        public void Save(string setting, string value)
        {
            Debug.WriteLine("Saving...");

            if (setting == "recentDirectories")
            {
                Debug.WriteLine("Settings saved: " + setting);

                // Saving to runtime variable
                // (Don't replace recentDirectories as this breaks bindings)
                if (recentDirectories.Contains(value)) // See if path was selected before
                {
                    // Put the selection at the top of the list even if it was in the list before
                    recentDirectories.RemoveAt(recentDirectories.IndexOf(value));
                }
                recentDirectories.Insert(0, value); // insert selected path into recent list
                if (recentDirectories.Count > 5) // limit recent directories to 5 items
                {
                    recentDirectories.RemoveAt(5);
                }
                // Saving to settings file
                localSettings.Values["recentDirectories"] = recentDirectories.ToArray();
            }
            if (setting == "gridWidths")
            {
                Debug.WriteLine("Settings saved: " + setting);
                // Saving to runtime variable is done by TwoWay Binding

                // Saving to settings file
                localSettings.Values["gridWidths"] = new string[] { gridWidthLeft, gridWidthRight };
            }
            Load();
        }

        public static string SettingsDisplayer(string setting, bool current)
        {
            Debug.Write("SettingsDisplayer called");
            if (setting == "fileSizeSetting" && current) { return "Use file size on disk"; }
            else { return "Use real file size"; }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "") //was string propertyName
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
