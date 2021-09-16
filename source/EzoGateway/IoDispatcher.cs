using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace EzoGateway
{
    /// <summary>
    /// GPIO handling on the EzoGateway PCB, don´t work on the Whitebox Labs Tentacle T3
    /// </summary>
    public class IoDispatcher
    {
#if EZOGW_HW
        public const bool EZO_GATEWAY_HARDWARE_AVAILABLE = true;
#else
        public const bool EZO_GATEWAY_HARDWARE_AVAILABLE = false;
#endif

        #region Constants
        /// <summary>
        /// Top LED GPIO pin
        /// </summary>
        private const int LED1_PIN = 16;
        private const int LED2_PIN = 26;
        private const int LED3_PIN = 19;
        /// <summary>
        /// Bottom LED GPIO pin
        /// </summary>
        private const int LED4_PIN = 13;
        /// <summary>
        /// Flashtime in ms
        /// </summary>
        private const int FLASH_TIME_MS = 300;

        //Only available if no EZO gateway hardware is used.
        //GPIO pins in sequence for channel 1 to n.
        //Pin 2 and 3 are reservated for I2C communication with the EZO circuit modules.
        private readonly int[] OUT_PINS = { 5, 6, 13, 19, 26, 12 };


        private const GpioPinValue ON = GpioPinValue.Low;
        private const GpioPinValue OFF = GpioPinValue.High;
        #endregion Constants

        #region Members
        private GpioPin m_Led1;
        private GpioPin m_Led2;
        private GpioPin m_Led3;
        private GpioPin m_Led4;

        private GpioPin[] m_Outputs;

        private Timer m_Timer;
        private int m_CycleCounter;

        private bool m_AliveState;
        private bool m_CyclicUpdaterActive;

        #endregion Members

        #region Properties

        public int OutputChannelCount
        {
            get
            {
                if (!EZO_GATEWAY_HARDWARE_AVAILABLE && m_Outputs != null)
                    return m_Outputs.Length;
                else
                    return 0;
            }
        }

        #endregion Properties



        public IoDispatcher()
        {
            if (EZO_GATEWAY_HARDWARE_AVAILABLE)
            {
                m_Timer = new Timer(GpioWorker, new AutoResetEvent(false), 1000, 10);
                m_CycleCounter = 0;
            }

            InitLeds();
            InitOutputs();
        }

        /// <summary>
        /// Reinitialization of the used GPIO pins
        /// </summary>
        public void Reinit()
        {
            InitLeds();
            InitOutputs();
        }

        public void SetAliveState(bool alive)
        {
            m_AliveState = alive;
        }

        public void SetCyclicUpdaterState(bool active)
        {
            m_CyclicUpdaterActive = active;
        }

        public async void IndicateHttpRequest()
        {
            if (!EZO_GATEWAY_HARDWARE_AVAILABLE)
                return;

            m_Led3.Write(ON);
            await Task.Delay(250);
            m_Led3.Write(OFF);
        }

        public async void IndicateMeasurement()
        {
            if (!EZO_GATEWAY_HARDWARE_AVAILABLE)
                return;

            m_Led4.Write(ON);
            await Task.Delay(250);
            m_Led4.Write(OFF);
        }


        public async void Flash(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                m_Led1.Write(GpioPinValue.Low);
                m_Led2.Write(GpioPinValue.Low);
                m_Led3.Write(GpioPinValue.Low);
                m_Led4.Write(GpioPinValue.Low);
                await Task.Delay(FLASH_TIME_MS);
                m_Led1.Write(GpioPinValue.High);
                m_Led2.Write(GpioPinValue.High);
                m_Led3.Write(GpioPinValue.High);
                m_Led4.Write(GpioPinValue.High);
                if (i < count)
                    await Task.Delay(FLASH_TIME_MS * 3);
            }
        }

        /// <summary>
        /// Set a specific output channel
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="state">State: true = on; false = off</param>
        public void SetOutput(int channel, bool state = true)
        {
            if (EZO_GATEWAY_HARDWARE_AVAILABLE)
                return;

            if (m_Outputs == null || m_Outputs.Length <= 0)
                throw new ArgumentNullException("Outputs not initialized");

            if (channel < 1 || channel > m_Outputs.Length)
                throw new ArgumentOutOfRangeException("Channel number out of valid range.");


            m_Outputs[channel - 1].Write(state == true ? ON : OFF);
        }

        /// <summary>
        /// Read the state of a specific output channel
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <returns>State: true = on; false = off</returns>
        public bool GetOutput(int channel)
        {
            if (EZO_GATEWAY_HARDWARE_AVAILABLE)
                return false;

            if (m_Outputs == null || m_Outputs.Length <= 0)
                throw new ArgumentNullException("Outputs not initialized");

            if (channel < 1 || channel > m_Outputs.Length)
                throw new ArgumentOutOfRangeException("Channel number out of valid range.");


            return m_Outputs[channel - 1].Read() == ON ? true : false;
        }

        private async void GpioWorker(object stateInfo)
        {
            if (m_AliveState && m_CycleCounter < 25)
                m_Led1.Write(ON);
            else
                m_Led1.Write(OFF);

            if (m_CyclicUpdaterActive && m_CycleCounter > 25)
                m_Led2.Write(ON);
            else
                m_Led2.Write(OFF);

            if (m_CycleCounter >= 100)
                m_CycleCounter = 0;
            else
                m_CycleCounter++;
        }

        private void InitLeds()
        {
            if (!EZO_GATEWAY_HARDWARE_AVAILABLE)
                return;

            var gpio = GpioController.GetDefault();
            if (gpio == null)
                throw new Exception("Kein GPIO-Controller auf diesem Gerät");

            m_Led1 = gpio.OpenPin(LED1_PIN);
            m_Led2 = gpio.OpenPin(LED2_PIN);
            m_Led3 = gpio.OpenPin(LED3_PIN);
            m_Led4 = gpio.OpenPin(LED4_PIN);

            m_Led1.SetDriveMode(GpioPinDriveMode.Output);
            m_Led2.SetDriveMode(GpioPinDriveMode.Output);
            m_Led3.SetDriveMode(GpioPinDriveMode.Output);
            m_Led4.SetDriveMode(GpioPinDriveMode.Output);

            m_Led1.Write(GpioPinValue.High);
            m_Led2.Write(GpioPinValue.High);
            m_Led3.Write(GpioPinValue.High);
            m_Led4.Write(GpioPinValue.High);

            //BlinkGreenLed(2000);
        }

        private void InitOutputs()
        {
            if (EZO_GATEWAY_HARDWARE_AVAILABLE)
                return;

            var gpio = GpioController.GetDefault();
            if (gpio == null)
                throw new Exception("Kein GPIO-Controller auf diesem Gerät");

            m_Outputs = new GpioPin[OUT_PINS.Length];

            for (int i = 0; i < OUT_PINS.Length; i++)
            {
                m_Outputs[i] = gpio.OpenPin(OUT_PINS[i]);
                m_Outputs[i].SetDriveMode(GpioPinDriveMode.Output);
                m_Outputs[i].Write(GpioPinValue.High);
            }
        }


        private async void BlinkGreenLed(int delay)
        {
            m_Led2.Write(GpioPinValue.Low);
            await Task.Delay(delay);
            m_Led2.Write(GpioPinValue.High);
        }

        private async void IsAlive(object stateInfo)
        {
            m_Led1.Write(GpioPinValue.Low);
            await Task.Delay(100);
            m_Led2.Write(GpioPinValue.Low);
            await Task.Delay(100);
            m_Led1.Write(GpioPinValue.High);
            m_Led3.Write(GpioPinValue.Low);
            await Task.Delay(100);
            m_Led2.Write(GpioPinValue.High);
            m_Led4.Write(GpioPinValue.Low);
            await Task.Delay(100);
            m_Led3.Write(GpioPinValue.High);
            await Task.Delay(100);
            m_Led4.Write(GpioPinValue.High);



            //Heartbeat
            //m_Led3.Write(GpioPinValue.Low);
            //await Task.Delay(100);
            //m_Led3.Write(GpioPinValue.High);
            //await Task.Delay(100);
            //m_Led3.Write(GpioPinValue.Low);
            //await Task.Delay(50);
            //m_Led3.Write(GpioPinValue.High);
        }
    }
}
