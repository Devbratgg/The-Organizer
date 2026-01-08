using System;
using System.Windows;
using System.Data.SQLite;
using Dapper;
using TheOrganizer.Data;
using TheOrganizer.Models;
using TheOrganizer.Views;
using System.Linq;

namespace TheOrganizer
{
    public partial class MainWindow : Window
    {
        private int _currentFolderId = -1;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // 1. LISTEN TO SIDE MENU EVENTS
                if (SideMenuControl != null)
                {
                    SideMenuControl.CloseRequested += SideMenuControl_CloseRequested;
                    SideMenuControl.OpenRequested += SideMenuControl_OpenRequested;
                    SideMenuControl.FolderSelected += OnFolderSelected;
                }

                // 2. LISTEN TO ENTRY LIST: "Open Album"
                if (EntryListControl != null)
                {
                    EntryListControl.AlbumViewRequested += OnAlbumViewRequested;
                }

                // 3. LISTEN TO ALBUM VIEW: "Go Back" & "SubView"
                if (AlbumViewControl != null)
                {
                    AlbumViewControl.BackRequested += OnBackToEntryRequested;
                    AlbumViewControl.SubViewRequested += OnOpenSubView;
                }

                // 4. LISTEN TO SUB VIEW
                if (MySubView != null)
                {
                    MySubView.BackRequested += OnBackToAlbumRequested;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CRASH REPORT:\n\n{ex.Message}\n\n{ex.InnerException?.Message}", "App Failed to Start");
                Application.Current.Shutdown();
            }
        }

        private void OnOpenSubView(object sender, AlbumItem album)
        {
            AlbumViewControl.Visibility = Visibility.Collapsed;
            MySubView.Visibility = Visibility.Visible;
            MySubView.LoadSubAlbum(album.Id, album.Name);
        }

        private void OnBackToAlbumRequested(object sender, RoutedEventArgs e)
        {
            MySubView.Visibility = Visibility.Collapsed;
            AlbumViewControl.Visibility = Visibility.Visible;
        }

        // --- NAVIGATION LOGIC ---

        private void OnFolderSelected(CategoryFolder folder)
        {
            _currentFolderId = folder.Id;

            // RESET VIEW LAYERS
            if (EntryListControl != null) EntryListControl.Visibility = Visibility.Visible;
            if (AlbumViewControl != null) AlbumViewControl.Visibility = Visibility.Collapsed;
            if (MySubView != null) MySubView.Visibility = Visibility.Collapsed;

            // Calculate and Update Header with real Data
            RefreshHeaderData(folder.Id, folder.Name);

            // Load Entries for this folder
            if (EntryListControl != null)
            {
                EntryListControl.LoadEntriesFromDb(folder.Id);
            }
        }

        private void RefreshHeaderData(int folderId, string folderName)
        {
            using (var conn = new SQLiteConnection(DbConfig.ConnectionString))
            {
                conn.Open();

                // Sum all ManualSizes from Albums belonging to this Folder
                long totalBytes = conn.QueryFirstOrDefault<long>(@"
                    SELECT COALESCE(SUM(ManualSize), 0) 
                    FROM Albums 
                    WHERE EntryId IN (SELECT Id FROM Entries WHERE ParentFolderId = @Fid)", 
                    new { Fid = folderId });

                string formattedSize = FormatSize(totalBytes);

                if (HeaderControl != null)
                {
                    HeaderControl.UpdateHeader(folderName, formattedSize);
                }
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1073741824) return $"{(bytes / 1073741824.0):F2} GB";
            if (bytes >= 1048576) return $"{(bytes / 1048576.0):F2} MB";
            if (bytes >= 1024) return $"{(bytes / 1024.0):F2} KB";
            return $"{bytes} Bytes";
        }

        private void OnAlbumViewRequested(EntryItem entry)
        {
            EntryListControl.Visibility = Visibility.Collapsed;
            AlbumViewControl.Visibility = Visibility.Visible;
            AlbumViewControl.LoadAlbum(entry.RealId, entry.Name);
        }

        private void OnBackToEntryRequested(object sender, RoutedEventArgs e)
        {
            AlbumViewControl.Visibility = Visibility.Collapsed;
            EntryListControl.Visibility = Visibility.Visible;

            // Update size in case user changed it while in Album View
            if (_currentFolderId != -1)
            {
                using (var conn = new SQLiteConnection(DbConfig.ConnectionString))
                {
                    conn.Open();
                    var fName = conn.QueryFirstOrDefault<string>("SELECT Name FROM CategoryFolders WHERE Id = @Id", new { Id = _currentFolderId });
                    RefreshHeaderData(_currentFolderId, fName ?? "Unknown");
                }
            }
        }

        private void SideMenuControl_CloseRequested(object sender, RoutedEventArgs e)
        {
            SidePanelColumn.Width = GridLength.Auto;
        }

        private void SideMenuControl_OpenRequested(object sender, RoutedEventArgs e)
        {
            SidePanelColumn.Width = new GridLength(250);
        }
    }
}