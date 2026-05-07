using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MissionTelemetry.Core.ViewModels;

namespace MissionTelemetry.Wpf
{
    public partial class MainWindow
    {
        private Brush HoloGrid => (Brush)FindResource("HoloCyanBrush");
        private Brush HoloRing => (Brush)FindResource("HoloGreenBrush");
        private Brush HoloArrow => (Brush)FindResource("HoloAmberBrush");
        private Brush ContactNormal => (Brush)FindResource("HoloGreenBrush");
        private Brush ContactWarn => (Brush)FindResource("HoloAmberBrush");
        private Brush ContactAlarm => (Brush)FindResource("ImperialRedBrush");

        private bool _radarBackgroundDrawn;
        private DateTime _lastRadarOverlayUpdate = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void RadarCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DrawRadarBackground();

            CompositionTarget.Rendering += (_, __) =>
            {
                if (!_radarBackgroundDrawn)
                    DrawRadarBackground();

                // Overlay nur ca. alle 40 ms neu zeichnen (~25 FPS)
                var now = DateTime.UtcNow;
                if ((now - _lastRadarOverlayUpdate).TotalMilliseconds >= 40)
                {
                    DrawRadarOverlay();
                    _lastRadarOverlayUpdate = now;
                }
            };
        }

        private void DrawRadarBackground()
        {
            double W = RadarBackgroundCanvas.ActualWidth > 0 ? RadarBackgroundCanvas.ActualWidth : RadarBackgroundCanvas.Width;
            double H = RadarBackgroundCanvas.ActualHeight > 0 ? RadarBackgroundCanvas.ActualHeight : RadarBackgroundCanvas.Height;
            double cx = W / 2;
            double cy = H / 2;

            RadarBackgroundCanvas.Children.Clear();

            // Ringe
            int rings = 5;
            for (int i = 1; i <= rings; i++)
            {
                double r = i * Math.Min(cx, cy) / rings;
                var circle = new Ellipse
                {
                    Width = 2 * r,
                    Height = 2 * r,
                    Stroke = HoloRing,
                    StrokeThickness = 1.2,
                    Opacity = 0.6,
                    Effect = (Effect)FindResource("HoloGlow")
                };
                Canvas.SetLeft(circle, cx - r);
                Canvas.SetTop(circle, cy - r);
                RadarBackgroundCanvas.Children.Add(circle);
            }

            // Achsen
            RadarBackgroundCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = cy,
                X2 = W,
                Y2 = cy,
                Stroke = HoloGrid,
                StrokeThickness = 1,
                Opacity = 0.35,
                Effect = (Effect)FindResource("HoloGlow")
            });

            RadarBackgroundCanvas.Children.Add(new Line
            {
                X1 = cx,
                Y1 = 0,
                X2 = cx,
                Y2 = H,
                Stroke = HoloGrid,
                StrokeThickness = 1,
                Opacity = 0.35,
                Effect = (Effect)FindResource("HoloGlow")
            });

            // Eigenes Fahrzeug
            var self = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = (Brush)FindResource("HoloCyanBrush"),
                Effect = (Effect)FindResource("StrongGlow")
            };
            Canvas.SetLeft(self, cx - 6);
            Canvas.SetTop(self, cy - 6);
            RadarBackgroundCanvas.Children.Add(self);

            _radarBackgroundDrawn = true;
        }

        private void DrawRadarOverlay()
        {
            if (DataContext is not TelemetryViewModel vm)
                return;

            double W = RadarOverlayCanvas.ActualWidth > 0 ? RadarOverlayCanvas.ActualWidth : RadarOverlayCanvas.Width;
            double H = RadarOverlayCanvas.ActualHeight > 0 ? RadarOverlayCanvas.ActualHeight : RadarOverlayCanvas.Height;
            double cx = W / 2;
            double cy = H / 2;
            double scale = Math.Min(cx, cy) / vm.RadarRangeKm;

            RadarOverlayCanvas.Children.Clear();

            foreach (var c in vm.Proximity.ToList())
            {
                // Body->Canvas: X (vorne) -> -Y(Canvas), Y (rechts) -> +X(Canvas)
                double x = cx + c.Y_km * scale;
                double y = cy - c.X_km * scale;

                Brush dotBrush = ContactNormal;
                if (c.CPA_Dist_km < 3.0 || c.Distance_km < 5.0)
                    dotBrush = ContactWarn;
                if (c.CPA_Dist_km < 1.0 || c.Distance_km < 2.0)
                    dotBrush = ContactAlarm;

                // Punkt
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = dotBrush,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.7,
                    Opacity = 0.95,
                    Effect = (Effect)FindResource("StrongGlow")
                };
                Canvas.SetLeft(dot, x - 4);
                Canvas.SetTop(dot, y - 4);
                RadarOverlayCanvas.Children.Add(dot);

                // Velocity-Pfeil
                double velScalePx = 28.0;
                double vx = c.Vy_kms * velScalePx;
                double vy = -c.Vx_kms * velScalePx;

                var line = new Line
                {
                    X1 = x,
                    Y1 = y,
                    X2 = x + vx,
                    Y2 = y + vy,
                    Stroke = HoloArrow,
                    StrokeThickness = 2.0,
                    Opacity = 0.95,
                    Effect = (Effect)FindResource("HoloGlow")
                };
                RadarOverlayCanvas.Children.Add(line);

                RadarOverlayCanvas.Children.Add(ArrowHead(x + vx, y + vy, x, y, 9, 26, HoloArrow));

                // Label
                var label = new TextBlock
                {
                    Text = $"#{c.Id}  {c.Distance_km:F1}km  {c.Bearing_deg:F0}°  CPA {c.CPA_Dist_km:F1}km @{(double.IsInfinity(c.TCPA_s) ? "∞" : $"{c.TCPA_s:F0}s")}",
                    Foreground = HoloGrid
                };
                Canvas.SetLeft(label, x + 10);
                Canvas.SetTop(label, y - 12);
                RadarOverlayCanvas.Children.Add(label);
            }
        }

        private Polygon ArrowHead(double x2, double y2, double x1, double y1, double width, double length, Brush brush)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double L = Math.Sqrt(dx * dx + dy * dy);
            if (L < 1e-6) L = 1e-6;

            dx /= L;
            dy /= L;
            double qx = -dy;
            double qy = dx;

            var tip = new Point(x2, y2);
            var bx = x2 - dx * length;
            var by = y2 - dy * length;
            var p2 = new Point(bx + qx * width * 0.5, by + qy * width * 0.5);
            var p3 = new Point(bx - qx * width * 0.5, by - qy * width * 0.5);

            return new Polygon
            {
                Points = new PointCollection { tip, p2, p3 },
                Stroke = brush,
                Fill = brush,
                Opacity = 0.95,
                Effect = (Effect)FindResource("HoloGlow")
            };
        }
    }
}