using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SignToolTool.Annotations;

namespace SignToolTool
{
    public class SignToolModel : INotifyPropertyChanged
    {
        private string _toolPath = string.Empty;
        private string _signature = string.Empty;
        private string _timestampAuthority = string.Empty;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string ToolPath      
        {
            get => _toolPath;
            set
            {
                if (value == _toolPath) return;
                _toolPath = value;
                OnPropertyChanged();
            }
        }

        public string Signature 
        {
            get => _signature;
            set
            {
                if (value == _signature) return;
                _signature = value;
                OnPropertyChanged();
            }
        }

        public string TimestampAuthority
        {
            get => _timestampAuthority;
            set
            {
                if (value == _timestampAuthority) return;
                _timestampAuthority = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Files { get; }
        public ObservableCollection<string> TimestampAuthorities { get; }

        public SignToolModel()
        {
            Files = new ObservableCollection<string>();
            TimestampAuthorities = new ObservableCollection<string>();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand BrowseToolPathCommand => new RelayCommand(BrowseToolPathCommandExecuted);
        public ICommand BrowseCertificateCommand => new RelayCommand(BrowseCertificateCommandExecuted);
        public ICommand AddFileCommand => new RelayCommand(AddFileCommandExecuted);
        public ICommand RemoveFileCommand => new RelayCommand<object>(RemoveFileCommandExecuted, RemoveFileCommandCanExecute);
        public ICommand SignCommand => new AsyncRelayCommand(SignExecuted);

        private async Task SignExecuted()
        {   
            if (!Files.Any() || string.IsNullOrWhiteSpace(ToolPath) ||
                string.IsNullOrWhiteSpace(Signature))
                return;
            var p = new ProcessStartInfo(ToolPath)
            {
                ArgumentList =
                {
                    "sign",
                    "/fd","SHA512",
                    "/td","SHA512",
                    "/sha1", $"{Signature}",
                    "/tr", $"{TimestampAuthority}",
                },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            foreach(var path in Files)
                p.ArgumentList.Add(path);

            using var process = Process.Start(p);
            if (process == null) return;
            await using var errorWriter = new StringWriter();
            await using var messageWriter = new StringWriter();

            await messageWriter.WriteLineAsync(await process.StandardOutput.ReadToEndAsync());
            await errorWriter.WriteLineAsync(await process.StandardError.ReadToEndAsync());

            await process.WaitForExitAsync();

            if (!TimestampAuthorities.Contains(TimestampAuthority))
                TimestampAuthorities.Add(TimestampAuthority);

            var error = errorWriter.ToString();
            if (!string.IsNullOrWhiteSpace(error))
            {
                MessageBox.Show(Application.Current!.MainWindow!, error, "Error from Signing", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }

            var message = messageWriter.ToString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(Application.Current!.MainWindow!, message, "Message from Signing", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool RemoveFileCommandCanExecute(object? obj)
        {
            return obj is string filePath && Files.Contains(filePath);
        }

        private void RemoveFileCommandExecuted(object? o)
        {
            if (o is string filePath && Files.Contains(filePath))
                Files.Remove(filePath);
        }

        private void AddFileCommandExecuted()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                DefaultExt = "*.*",
                Title = "Choose Files to Sign...",
                CheckFileExists = true,
                Multiselect = true,
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) is true)
            {
                foreach (var file in dialog.FileNames)
                {
                    Files.Add(file);
                }
            }
        }

        private void BrowseCertificateCommandExecuted()
        {
            // Get a certificate from a Windows Store
            var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            // Display a dialog box to select a certificate from the Windows Store
            var selectedCertificates =
                X509Certificate2UI.SelectFromCollection(store.Certificates, "Select Certificate","Select the Certificate to Sign with", X509SelectionFlag.SingleSelection);

            // Get the first certificate that has a primary key
            foreach (var certificate in selectedCertificates)
            {
                if (certificate.HasPrivateKey)
                {
                    Signature = certificate.Thumbprint;
                    break;
                }
            }
        }

        private void BrowseToolPathCommandExecuted()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                DefaultExt = "signtool.exe",
                Title = "Set signtool.exe Path",
                CheckFileExists = true,
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) is true)
            {
                ToolPath = dialog.FileName;
            }
        }
    }
}
