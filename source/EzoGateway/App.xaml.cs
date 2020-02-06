using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace EzoGateway
{
    /// <summary>
    /// Provides the application-specific behavior to supplement the standard application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object. This is the first line of generated code
        /// and therefore the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            Logger.Write("Startup EzoGateway App", SubSystem.App);
            Logger.Write("Version " + typeof(App).GetTypeInfo().Assembly.GetName().Version.ToString(), SubSystem.App);
            this.UnhandledException += OnUnhandledException;

            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Log unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Write(":::::::::::::::::::::::::::::::::::::: START OF UNHANDLED EXCEPTION ::::::::::::::::::::::::::::::::::::::", SubSystem.App);
            Logger.Write(e.Message, SubSystem.App, LoggerLevel.CriticalError);
            Logger.Write(e.Exception.StackTrace, SubSystem.App, LoggerLevel.CriticalError);
            Logger.Write("::::::::::::::::::::::::::::::::::::::: END OF UNHANDLED EXCEPTION :::::::::::::::::::::::::::::::::::::::", SubSystem.App);
            
            Logger.Flush();
        }

        /// <summary>
        /// Is called when the application is started normally by the end user. Other entry points
        /// are used, for example, when the application is started to open a particular file.
        /// </summary>
        /// <param name="e">Details about start request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization if the window already contains content.
            // Just make sure that the window is active.
            if (rootFrame == null)
            {
                // Create a frame that acts as a navigation context and navigates to the parameter of the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously paused application
                }

                // Den Frame im aktuellen Fenster platzieren
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // If the navigation stack is not restored, navigate to the first page and configure
                    // the new page by passing the required information as navigation parameters
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Make sure that the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Called when navigation to a specific page fails
        /// </summary>
        /// <param name="sender">The frame where navigation failed</param>
        /// <param name="e">Details about the navigation error</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Is called when the execution of the application is stopped.  The application status is saved without
        /// knowing whether the application is terminated or continued, and the memory contents remain undamaged.
        /// </summary>
        /// <param name="sender">The source of the stop request.</param>
        /// <param name="e">Details of the stop request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application status and end all background activities
            deferral.Complete();
        }
    }
}
