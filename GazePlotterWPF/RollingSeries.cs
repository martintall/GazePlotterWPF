using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GazePlotterWPF
{
    public class RollingSeries
    {
        #region Variables

        private List<double> values = new List<double>();
        private List<Line> lines = new List<Line>();
        private RollingMonitor monitor = null;
        private Brush lineBrush = new SolidColorBrush(Colors.Red);
        private double lineThickness = 1.0;

        #endregion 

        #region Constructor

        public RollingSeries(RollingMonitor monitor)
        {
            this.monitor = monitor;

            for (int i = 0; i < monitor.MaintainHistoryCount; i++)
            {
                values.Add(0);
                Line l = new Line() 
                { 
                    X1 = i * 1, 
                    X2 = i * 1 + 1, 
                    Y1 = 0, 
                    Y2 = 0,
                    Stroke = this.LineBrush, 
                    StrokeThickness = this.lineThickness 
                };
                lines.Add(l);
                monitor.Children.Add(l);
            }

            monitor.SizeChanged += Canvas_SizeChanged;
            Canvas_SizeChanged(this, null);
        }

        #endregion

        #region Get/Set

        public Brush LineBrush
        {
            get
            {
                return lineBrush;
            }
            set
            {
                lineBrush = value;
                foreach (Line l in lines)
                    l.Stroke = lineBrush;
            }
        }

        public double LineThickness
        {
            get
            {
                return lineThickness;
            }
            set
            {
                lineThickness = value;
                foreach (Line l in lines)
                    l.StrokeThickness = lineThickness;
            }
        }

        #endregion

        #region Public methods

        public void AddValue(double value)
        {
            values.Add(value);
            values.RemoveAt(0);
        }

        public void Refresh()
        {
            // Note: must be called on UI thread
            for (int i = 0; i < lines.Count - 1 && i < values.Count - 1; i++)
            {
                double y1 = Math.Max(Math.Min(values[i], monitor.MaxValue), monitor.MinValue);
                double y2 = Math.Max(Math.Min(values[i + 1], monitor.MaxValue), monitor.MinValue);
                lines[i].Y1 = monitor.ScaleY(y1);
                lines[i].Y2 = monitor.ScaleY(y2);
            }
        }

        #endregion

        #region Private methods

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double newMaxX = monitor.ActualWidth;
            double spacing = newMaxX / (lines.Count - 1);

            if (spacing >= monitor.MinValueSpacing)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    lines[i].X1 = i * spacing;
                    lines[i].X2 = i * spacing + spacing;
                }
            }
            else
            {
                double oldMaxX = (lines.Count - 2) * monitor.MinValueSpacing + monitor.MinValueSpacing;
                double modify = newMaxX - oldMaxX;

                for (int i = 0; i < lines.Count; i++)
                {
                    lines[i].X1 = (i * monitor.MinValueSpacing) + modify;
                    lines[i].X2 = (i * monitor.MinValueSpacing + monitor.MinValueSpacing) + modify;
                }
            }
        }

        #endregion
    }
}
