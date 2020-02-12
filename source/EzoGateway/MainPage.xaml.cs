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
using System.Reflection;

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
        HttpServer m_Server;
        #endregion Members

        public MainPage()
        {
            this.InitializeComponent();
            Logger.Write("Initialize built-in GUI", SubSystem.App);

            m_Controller = new Controller();

            m_Server = new HttpServer(ref m_Controller, 591); //default port (80)
            m_Server.ServerInitialize();

            Logger.LogWatermark();

            m_Controller.PropertyChanged += Controller_PropertyChanged;
            m_Controller.ConfigIsLoadedEvent += Controller_ConfigIsLoadedEvent;


            //Apply labels

            tbl_Value1.Text = "pH value";
            tbl_Value2.Text = "Redox potential";
            tbl_Value3.Text = "Water temperature";

            tbl_Version.Text = typeof(App).GetTypeInfo().Assembly.GetName().Version.ToString();

            //Init up for layout-test
            if (true)
            {
                tbx_Value1.Text = "7,03";
                tbx_Value2.Text = "647 mV";
                tbx_Value3.Text = "21,8 °C";
                tbl_Version.Text = "0.4.9-dev";
            }

        }

        private void Controller_ConfigIsLoadedEvent()
        {
            if (m_Controller.Configuration != null && m_Controller.Configuration.Appearance != null)
                tbl_Headline.Text = m_Controller.Configuration.Appearance.DeviceName;
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
