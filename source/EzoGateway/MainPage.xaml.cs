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

            m_Controller.PropertyChanged += Controller_PropertyChanged;

            //Test GUI
            tbl_Value1.Text = "pH Wert";
            tbl_Value2.Text = "Redoxpotential";
            tbl_Value3.Text = "Wassertemperatur";
        }

        private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Controller.LatestMeasData))
            {
                if (m_Controller.LatestMeasData != null
                    && m_Controller.LatestMeasData.ContainsKey(1)
                    && m_Controller.LatestMeasData.ContainsKey(2)
                    && m_Controller.LatestMeasData.ContainsKey(3))
                {
                    tbx_Value1.Text = m_Controller.LatestMeasData[2].ToString(2);
                    tbx_Value2.Text = m_Controller.LatestMeasData[3].ToString(0);
                    tbx_Value3.Text = m_Controller.LatestMeasData[1].ToString(1);
                }
            }
        }
    }
}
