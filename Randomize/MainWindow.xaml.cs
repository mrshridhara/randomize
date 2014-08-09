using Randomize.Converters;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Randomize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool closeOnContentRendered;
        private bool isStarted;
        private volatile BackgroundWorker worker;

        public MainWindow()
        {
            SetIcon();
            InitializeComponent();
            RegisterEvents();
        }

        private void OnWindowContentRendered(object sender, EventArgs e)
        {
            LoadUsbDrives();

            if (closeOnContentRendered)
            {
                Close();
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (isStarted)
            {
                var result = MessageBox.Show("Are you sure you want to exit while in progress?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            DeregisterEvents();
        }

        private void OnStartCliecked(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void SetIcon()
        {
            var converter = new BitmapConverter();
            var iconSource = converter.Convert(Properties.Resources.CarAudioPng, typeof(ImageSource), null, CultureInfo.CurrentCulture);
            Icon = iconSource as ImageSource;
        }

        private void RegisterEvents()
        {
            ContentRendered += OnWindowContentRendered;
            Closing += OnWindowClosing;
            startButton.Click += OnStartCliecked;
        }

        private void DeregisterEvents()
        {
            ContentRendered -= OnWindowContentRendered;
            Closing -= OnWindowClosing;
            startButton.Click -= OnStartCliecked;
        }

        private void LoadUsbDrives()
        {
            try
            {
                driveComboBox.ItemsSource = DriveHelper.GetRemovableDrives();
                driveComboBox.SelectedIndex = 0;
            }
            catch (DriveNotFoundException)
            {
                MessageBox.Show("No USB drives found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                closeOnContentRendered = true;
            }
        }

        private void Start()
        {
            if (String.IsNullOrWhiteSpace(sourceTextBox.Text))
            {
                return;
            }

            var sourceDirectory = sourceTextBox.Text.Trim();
            if (Directory.Exists(sourceDirectory) == false)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(fileTypesTextBox.Text))
            {
                return;
            }

            var fileTypes = fileTypesTextBox.Text.Trim();

            waitScreen.Visibility = Visibility.Visible;
            isStarted = true;

            StartBackgroundWorker(sourceDirectory, fileTypes, driveComboBox.SelectedItem as Drive);
        }

        private void StartBackgroundWorker(string sourceDirectory, string fileTypes, Drive targetDrive)
        {
            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            worker.DoWork += OnDoWork;
            worker.ProgressChanged += OnProgressChanged;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;
            worker.RunWorkerAsync(new { sourceDirectory, fileTypes, targetDrive });
        }

        private void OnDoWork(object sender, DoWorkEventArgs e)
        {
            dynamic argument = e.Argument;

            string sourceDirectory = argument.sourceDirectory;
            string fileTypes = argument.fileTypes;
            Drive targetDrive = argument.targetDrive;

            var filesToBeDeleted = Directory.GetFiles(targetDrive.DriveLetter, "*.*", SearchOption.TopDirectoryOnly);
            var directoriesToBeDeleted = Directory.GetDirectories(targetDrive.DriveLetter);
            var filesToBeCopied = Directory.GetFiles(sourceDirectory, fileTypes, SearchOption.TopDirectoryOnly);

            var totalIterations = filesToBeDeleted.LongLength + directoriesToBeDeleted.LongLength + filesToBeCopied.LongLength;

            for (long index = 0; index < filesToBeDeleted.LongLength; index++)
            {
                var percentage = (int)Math.Round((index) * (100.0M / totalIterations));
                worker.ReportProgress(percentage);

                var currentFile = filesToBeDeleted[index];

                File.Delete(currentFile);
            }

            for (long index = 0; index < directoriesToBeDeleted.LongLength; index++)
            {
                var percentage = (int)Math.Round((filesToBeDeleted.LongLength + index) * (100.0M / totalIterations));
                worker.ReportProgress(percentage);

                var currentDirectory = directoriesToBeDeleted[index];

                Directory.Delete(currentDirectory, true);
            }

            filesToBeCopied.Shuffle();

            for (long index = 0; index < filesToBeCopied.LongLength; index++)
            {
                var percentage = (int)Math.Round((filesToBeDeleted.LongLength + directoriesToBeDeleted.LongLength + index) * (100.0M / totalIterations));
                worker.ReportProgress(percentage);

                var sourceFilePath = filesToBeCopied[index];
                var fileName = DriveHelper.GetRelativePath(sourceFilePath, argument.sourceDirectory);
                var destinationFilePath = Path.Combine(targetDrive.DriveLetter, fileName);

                File.Copy(sourceFilePath, destinationFilePath);
            }

            e.Result = filesToBeCopied.Length;
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            waitProgress.IsIndeterminate = false;
            waitProgress.Value = e.ProgressPercentage;
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            worker.DoWork -= OnDoWork;
            worker.ProgressChanged -= OnProgressChanged;
            worker.RunWorkerCompleted -= OnRunWorkerCompleted;

            waitScreen.Visibility = Visibility.Hidden;
            isStarted = false;

            if (e.Error != null)
            {
                MessageBox.Show(String.Format("{0}{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(String.Format("Done! Copied {0} files.", e.Result), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
