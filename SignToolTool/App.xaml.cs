using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SignToolTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var model = new SignToolModel
            {
                ToolPath = SignToolToolSettings.Default.SignToolPath,
                TimestampAuthority = SignToolToolSettings.Default.TimeStampAuthority,
            };

            if (SignToolToolSettings.Default.TimeStampAuthorities is not null)
                foreach (var item in SignToolToolSettings.Default.TimeStampAuthorities)
                {
                    if (item is { } uri)
                        model.TimestampAuthorities.Add(item);
                }

           
            var view = new MainWindow
            {
                DataContext = model
            };

            if (SignToolToolSettings.Default.PositionSet)
            {
                view.Top = SignToolToolSettings.Default.Top;
                view.Left = SignToolToolSettings.Default.Left;
            }

            Exit += (_, _) =>
            {
                try
                {
                    SignToolToolSettings.Default.SignToolPath = model.ToolPath;
                    SignToolToolSettings.Default.TimeStampAuthority = model.TimestampAuthority;
                    if (SignToolToolSettings.Default.TimeStampAuthorities is null)
                        SignToolToolSettings.Default.TimeStampAuthorities = new StringCollection();
                    else
                        SignToolToolSettings.Default.TimeStampAuthorities.Clear();
                    foreach (var tsa in model.TimestampAuthorities)
                        SignToolToolSettings.Default.TimeStampAuthorities.Add(tsa);

                    SignToolToolSettings.Default.Save();
                }
                catch
                {
                    // Woops, failed to save.
                }
            };


            view.Show();
        }
    }
}