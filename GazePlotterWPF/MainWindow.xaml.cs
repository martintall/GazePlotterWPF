using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using TETCSharpClient;
using TETCSharpClient.Data;

namespace GazePlotterWPF
{
    public partial class MainWindow : Window, IGazeListener
    {
        #region Variabels
      
        private RollingSeries seriesLeftX;
        private RollingSeries seriesLeftY;
        private RollingSeries seriesLeftPupil;
        
        private RollingSeries seriesRightX;
        private RollingSeries seriesRightY;
        private RollingSeries seriesRightPupil;

        private const int LOG_LENGTH = 100;
        private List<GazeData> sampleLog;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);

            ApplyChartSettings();

            this.Closing += MainWindow_Closing;
        }

        #endregion

        #region Public methods

        public void OnGazeUpdate(GazeData gazeData)
        {
            int historyLenght = 10;
            int minBlinkDuration = 3;

            if(GazeManager.Instance.Framerate == GazeManager.FrameRate.FPS_60)
            {
                minBlinkDuration = 6;
                historyLenght = 20;
            }

            AddValuesToPlots(gazeData);
            bool isBlinkDetected = DetermineBlink(gazeData, historyLenght, minBlinkDuration);
            ToggleBlinkState(isBlinkDetected);
            RefreshPlots();
        }

        #endregion

        #region Private methods

        private void ApplyChartSettings()
        {
            monitorLeftX.MaxValue = Screen.PrimaryScreen.Bounds.Width;
            monitorRightX.MaxValue = Screen.PrimaryScreen.Bounds.Width;
            monitorLeftY.MaxValue = Screen.PrimaryScreen.Bounds.Height;
            monitorRightY.MaxValue = Screen.PrimaryScreen.Bounds.Height;

            monitorLeftPupil.MaxValue = 30;
            monitorRightPupil.MaxValue = 30;

            seriesLeftX = new RollingSeries(monitorLeftX);
            seriesLeftY = new RollingSeries(monitorLeftY);
            seriesLeftPupil = new RollingSeries(monitorLeftPupil);
            
            seriesRightX = new RollingSeries(monitorRightX);
            seriesRightY = new RollingSeries(monitorRightY);
            seriesRightPupil = new RollingSeries(monitorRightPupil);

            seriesLeftX.LineBrush = new SolidColorBrush(Colors.LightBlue);
            seriesRightX.LineBrush = new SolidColorBrush(Colors.LightBlue);
            seriesLeftY.LineBrush = new SolidColorBrush(Colors.Red);
            seriesRightY.LineBrush = new SolidColorBrush(Colors.Red);
            
            seriesLeftX.LineThickness = 0.35;
            seriesLeftY.LineThickness = 0.35;
            seriesRightX.LineThickness = 0.35;
            seriesRightY.LineThickness = 0.35;

            seriesLeftPupil.LineBrush = new SolidColorBrush(Colors.GreenYellow);
            seriesRightPupil.LineBrush = new SolidColorBrush(Colors.GreenYellow);

            seriesLeftPupil.LineThickness = 0.5;
            seriesRightPupil.LineThickness = 0.5;
        }

        private void AddValuesToPlots(GazeData gazeData)
        {
            if (gazeData == null)
                return;

            if (gazeData.LeftEye != null)
            {
                if (seriesLeftX != null)
                    seriesLeftX.AddValue(gazeData.LeftEye.RawCoordinates.X);

                if (seriesLeftY != null)
                    seriesLeftY.AddValue(gazeData.LeftEye.RawCoordinates.Y);

                if (seriesLeftPupil != null)
                    seriesLeftPupil.AddValue(gazeData.LeftEye.PupilSize);
            }

            if (gazeData.RightEye != null)
            {
                if (seriesRightX != null)
                    seriesRightX.AddValue(gazeData.RightEye.RawCoordinates.X);

                if (seriesRightY != null)
                    seriesRightY.AddValue(gazeData.RightEye.RawCoordinates.Y);

                if (seriesRightPupil != null)
                    seriesRightPupil.AddValue(gazeData.RightEye.PupilSize);
            }
        }

        private bool DetermineBlink(GazeData gazeData, int historyLenght, int minDuration)
        {
            // Add sample to log (lazy init and pop.)
            if (sampleLog == null)
            {
                sampleLog = new List<GazeData>(LOG_LENGTH);

                GazeData dummy = new GazeData();
                dummy.LeftEye.PupilSize = 10;
                dummy.RightEye.PupilSize = 10;

                for (int i = 0; i < LOG_LENGTH; i++)
                    sampleLog.Add(dummy);
            }

            sampleLog.Add(gazeData);
            sampleLog.RemoveAt(0);

            bool leftBlinkFound = FindBlinkEvent(historyLenght, true, minDuration);
            bool rightBlinkFound = FindBlinkEvent(historyLenght, false, minDuration);

            if (leftBlinkFound && rightBlinkFound)
                return true;
            else
                return false;
        }

        private bool FindBlinkEvent(int historyLenght, bool isLeft, int minDuration)
        {
            bool endFound = false;
            double lastSample = 9999;
            int duration = 0;

            for (int i = sampleLog.Count - 1; i > sampleLog.Count - 1 - historyLenght; i--)
            {
                double value = 0;

                if(isLeft)
                   value = sampleLog[i].LeftEye.PupilSize;
                else
                   value = sampleLog[i].RightEye.PupilSize;

                if (value == 0)
                {
                    lastSample = value;
                    continue;
                }

                if (lastSample == 0 && value != 0)
                {
                    if (((sampleLog[i].State & (0x4)) != 0))
                        duration++;

                    if (duration > minDuration)
                        endFound = true;
                }
            }

            return endFound;
        }

        private void ToggleBlinkState(bool isBlink)
        {
            if (GridLeftPupil.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new MethodInvoker(() => ToggleBlinkState(isBlink)));
                return;
            }

            if(isBlink)
                this.Background = new SolidColorBrush(Colors.White);
            else
                this.Background = new SolidColorBrush(Colors.Black);
                
        }

        private void RefreshPlots()
        {
            // Check calling thread, must be UI
            // If needed we dispatch here, once, instead of dispatching for each series individually
            if (monitorLeftX.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new MethodInvoker(RefreshPlots));
                return;
            }

            seriesLeftX.Refresh();
            seriesLeftY.Refresh();
            seriesLeftPupil.Refresh();

            seriesRightX.Refresh();
            seriesRightY.Refresh();
            seriesRightPupil.Refresh();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GazeManager.Instance.RemoveGazeListener(this);
            GazeManager.Instance.Deactivate();
        }

        #endregion
    }
}
