using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using EzoGateway.Server;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace EzoGateway
{
    /// <summary>
    /// User interface only for displaying live data.
    /// Operation only possible via web interface and REST API.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Members
        Controller m_Controller;

        #endregion Members

        public MainPage()
        {
            this.InitializeComponent();

            m_Controller = new Controller();

            var svr = new HttpServer(ref m_Controller); //default port (80)
            svr.ServerInitialize();

            Logger.LogWatermark();

            m_Controller.PropertyChanged += Controller_PropertyChanged;

            //Test GUI
            tbl_Value1.Text = "pH value";
            tbl_Value2.Text = "Redox potential";
            tbl_Value3.Text = "Water temperature";
        }

        private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //TODO: Invoke auf GUI zusammen fassen. Einmal reicht, da eh alles aus dem selben Thread kommt.

            if (m_Controller.LatestMeasData != null)
            {
                if (m_Controller.LatestMeasData.TryGetValue(1, out var tempValue))
                {
                    if (tbx_Value3.Dispatcher.HasThreadAccess)
                        tbx_Value3.Text = tempValue.ToString(1);
                    else
                    {
                        tbx_Value3.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            tbx_Value3.Text = tempValue.ToString(1);
                        }
                        );
                    }
                }

                if (m_Controller.LatestMeasData.TryGetValue(2, out var phValue))
                {
                    if (tbx_Value1.Dispatcher.HasThreadAccess)
                        tbx_Value1.Text = phValue.ToString(2);
                    else
                    {
                        tbx_Value3.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            tbx_Value1.Text = phValue.ToString(2);
                        }
                        );
                    }
                }

                if (m_Controller.LatestMeasData.TryGetValue(3, out var redoxValue))
                {
                    if (tbx_Value2.Dispatcher.HasThreadAccess)
                        tbx_Value2.Text = redoxValue.ToString(0);
                    else
                    {
                        tbx_Value3.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            tbx_Value2.Text = redoxValue.ToString(0);
                        }
                        );
                    }
                }
            }
        }
    }
}
