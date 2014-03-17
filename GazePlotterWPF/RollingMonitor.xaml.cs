using System.Windows.Controls;

namespace GazePlotterWPF
{
    public partial class RollingMonitor : Canvas
    {
        #region Constructor

        public RollingMonitor()
        {
            InitializeComponent();

            this.MaxValue = 100;
            this.MinValue = 0;
            this.MaintainHistoryCount = 100;
            this.MinValueSpacing = 5;
        }

        #endregion

        #region Get/Set

        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public int UpdateInterval { get; set; }
        public int MaintainHistoryCount { get; set; }
        public double MinValueSpacing { get; set; }

        #endregion

        #region Public methods

        public double ScaleY(double value)
        {
            return this.ActualHeight - (((value - MinValue) / (MaxValue - MinValue)) * this.ActualHeight);
        }

        #endregion
    }
}
