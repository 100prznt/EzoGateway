using Rca.EzoGateway.Plc.Sharp7;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EzoGateway.Plc
{
    public class PlcWorker
    {
        /// <summary>
        /// Cycle delaytime in ms
        /// </summary>
        private const int CYCLE_DELAY = 250;
        private S7Client m_Plc;
        private int m_TriggerAddress;
        private int m_TriggerBitPosition;
        private bool m_TriggerState;
        private BackgroundWorker m_Worker;
        private string m_IpAddress;

        public ConcurrentQueue<PlcDbData> SendBuffer { get; set; }

        /// <summary>
        /// PLC worker are in the cyclic running mode
        /// </summary>
        public bool IsRunning => m_Worker != null && m_Worker.IsBusy;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="ipAddress">IP address of connected PLC (Siemens LOGO!)</param>
        public PlcWorker(string ipAddress)
        {
            m_IpAddress = ipAddress;
            SendBuffer = new ConcurrentQueue<PlcDbData>();
            m_Worker = new BackgroundWorker();

            m_Worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            m_Worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            m_Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            m_Worker.WorkerReportsProgress = true;
            m_Worker.WorkerSupportsCancellation = true;

            InitPlc();
        }

        public void SetUpTrigger(int address, int bitPosition)
        {
            m_TriggerAddress = address;
            m_TriggerBitPosition = bitPosition;
        }

        public void Start()
        {
            if (!m_Worker.CancellationPending)
                Stop();

            m_Worker.RunWorkerAsync();
            Debug.WriteLine("PLC worker started.");
        }

        public void Stop()
        {
            if (m_Worker.IsBusy)
            {
                m_Worker.CancelAsync();
                SpinWait.SpinUntil(() => false, 2 * CYCLE_DELAY);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start">Start worker after init</param>
        public void InitPlc(bool start = false)
        {
            if (string.IsNullOrEmpty(m_IpAddress))
                throw new ArgumentException("IP address of PLC is not set.");

            m_Plc = new S7Client();

            m_Plc.SetConnectionParams(m_IpAddress, 0x0300, 0x0200);
            if (m_Plc.Connect() != 0)
                Debug.WriteLine("Failed to open PLC connection.");
            else
                Debug.WriteLine("PLC successfully connected. (" + m_IpAddress + ")");

            if (start)
                Start();
        }

        /// <summary>
        /// On completed do the appropriate task
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The background process is complete. We need to inspect
            // our response to see if an error occurred, a cancel was
            // requested or if we completed successfully.  
            if (e.Cancelled)
            {
                Debug.WriteLine("Task Cancelled.");
            }

            // Check to see if an error occurred in the background process.

            else if (e.Error != null)
            {
                Debug.WriteLine("Error while performing background operation.");
            }
            else
            {
                // Everything completed normally.
                Debug.WriteLine("Task Completed...");
            }
        }

        /// <summary>
        /// Notification is performed here to the progress bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            // This function fires on the UI thread so it's safe to edit
            // the UI control directly, no funny business with Control.Invoke :)
            // Update the progressBar with the integer supplied to us from the
            // ReportProgress() function.  

            //Debug.WriteLine("Processing......" + e.ProgressPercentage.ToString() + "%");
        }

        /// <summary>
        /// Time consuming operations go here </br>
        /// i.e. Database operations,Reporting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // The sender is the BackgroundWorker object we need it to
            // report progress and check for cancellation.
            //NOTE : Never play with the UI thread here...
            while (true)
            {
                //SEND
                do
                {
                    if (SendBuffer.TryDequeue(out var dataToSend))
                    {
                        //Send data to PLC
                        if (dataToSend.Address < 0 || dataToSend.Address > 850)
                            throw new Exception("Invalid VM-Address!");

                        if (m_Plc == null)
                            throw new Exception("PLC is not initialized.");

                        m_Plc.WriteArea(S7Consts.S7AreaDB, 1, dataToSend.Address, 1, S7Consts.S7WLWord, dataToSend.Data);

                        //if (incrementSecureCounter)
                        //    IncrementSecureCounter();
                    }

                } while (!SendBuffer.IsEmpty);


                //RECEIVE
                var buffer = new byte[1];
                var result = m_Plc.ReadArea(S7Consts.S7AreaDB, 1, m_TriggerAddress, 1, S7Consts.S7WLByte, buffer);
                if (result != 0)
                {
                    Debug.WriteLine("Error during read trigger signal from PLC.");
                    InitPlc(true);
                    return;
                }
                else
                {
                    if ((buffer[0] & (1 << m_TriggerBitPosition)) != 0)
                    {
                        if (m_TriggerState == false)
                        {
                            m_TriggerState = true;
                            TriggerEvent?.Invoke();
                        }
                    }
                    else if (m_TriggerState)
                        m_TriggerState = false;
                }



                //WAIT
                SpinWait.SpinUntil(() => false, CYCLE_DELAY);




                // Periodically report progress to the main thread so that it can
                // update the UI.  In most cases you'll just need to send an
                // integer that will update a ProgressBar                    
                m_Worker.ReportProgress(5);
                // Periodically check if a cancellation request is pending.
                // If the user clicks cancel the line
                // m_AsyncWorker.CancelAsync(); if ran above.  This
                // sets the CancellationPending to true.
                // You must check this flag in here and react to it.
                // We react to it by setting e.Cancel to true and leaving
                if (m_Worker.CancellationPending)
                {
                    // Set the e.Cancel flag so that the WorkerCompleted event
                    // knows that the process was cancelled.
                    e.Cancel = true;
                    m_Worker.ReportProgress(0);
                    break;
                }
            }
        }




        public event Action TriggerEvent;
    }
}
