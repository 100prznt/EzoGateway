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
        }
    }
}
