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

        private void OpenRadar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TelemetryViewModel vm)
                vm.IsRadarExpanded = true;

            _radarBackgroundDrawn = false;
        }

        private void CloseRadar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TelemetryViewModel vm)
                vm.IsRadarExpanded = false;
        }

        private void RadarCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DrawMiniRadarBackground();
            DrawFullRadarBackground();

            CompositionTarget.Rendering += (_, __) =>
            {
                var now = DateTime.UtcNow;
                if ((now - _lastRadarOverlayUpdate).TotalMilliseconds >= 40)
                {
                    DrawMiniRadarOverlay();
                    DrawFullRadarOverlay();
                    _lastRadarOverlayUpdate = now;
                }
            };
        }

        private void DrawMiniRadarBackground()
        {
            double W = MiniRadarBackgroundCanvas.ActualWidth > 0 ? MiniRadarBackgroundCanvas.ActualWidth : MiniRadarBackgroundCanvas.Width;
            double H = MiniRadarBackgroundCanvas.ActualHeight > 0 ? MiniRadarBackgroundCanvas.ActualHeight : MiniRadarBackgroundCanvas.Height;
            double cx = W / 2;
            double cy = H / 2;

            MiniRadarBackgroundCanvas.Children.Clear();

            int rings = 4;
            for (int i = 1; i <= rings; i++)
            {
                double r = i * Math.Min(cx, cy) / rings;
                var circle = new Ellipse
                {
                    Width = 2 * r,
                    Height = 2 * r,
                    Stroke = HoloRing,
                    StrokeThickness = 1.0,
                    Opacity = 0.5,
                    Effect = (Effect)FindResource("HoloGlow")
                };
                Canvas.SetLeft(circle, cx - r);
                Canvas.SetTop(circle, cy - r);
                MiniRadarBackgroundCanvas.Children.Add(circle);
            }

            MiniRadarBackgroundCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = cy,
                X2 = W,
                Y2 = cy,
                Stroke = HoloGrid,
                StrokeThickness = 1,
                Opacity = 0.25
            });

            MiniRadarBackgroundCanvas.Children.Add(new Line
            {
                X1 = cx,
                Y1 = 0,
                X2 = cx,
                Y2 = H,
                Stroke = HoloGrid,
                StrokeThickness = 1,
                Opacity = 0.25
            });

            var self = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = (Brush)FindResource("HoloCyanBrush"),
                Effect = (Effect)FindResource("StrongGlow")
            };
            Canvas.SetLeft(self, cx - 5);
            Canvas.SetTop(self, cy - 5);
            MiniRadarBackgroundCanvas.Children.Add(self);
        }

        private void DrawFullRadarBackground()
        {
            double W = FullRadarBackgroundCanvas.ActualWidth > 0 ? FullRadarBackgroundCanvas.ActualWidth : FullRadarBackgroundCanvas.Width;
            double H = FullRadarBackgroundCanvas.ActualHeight > 0 ? FullRadarBackgroundCanvas.ActualHeight : FullRadarBackgroundCanvas.Height;
            double cx = W / 2;
            double cy = H / 2;

            FullRadarBackgroundCanvas.Children.Clear();

            int rings = 6;
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
                FullRadarBackgroundCanvas.Children.Add(circle);
            }

            FullRadarBackgroundCanvas.Children.Add(new Line
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

            FullRadarBackgroundCanvas.Children.Add(new Line
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

            var self = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = (Brush)FindResource("HoloCyanBrush"),
                Effect = (Effect)FindResource("StrongGlow")
            };
            Canvas.SetLeft(self, cx - 6);
            Canvas.SetTop(self, cy - 6);
            FullRadarBackgroundCanvas.Children.Add(self);
        }

        private void DrawMiniRadarOverlay()
        {
            if (DataContext is not TelemetryViewModel vm)
                return;

            double W = MiniRadarOverlayCanvas.ActualWidth > 0 ? MiniRadarOverlayCanvas.ActualWidth : MiniRadarOverlayCanvas.Width;
            double H = MiniRadarOverlayCanvas.ActualHeight > 0 ? MiniRadarOverlayCanvas.ActualHeight : MiniRadarOverlayCanvas.Height;
            double cx = W / 2;
            double cy = H / 2;
            double scale = Math.Min(cx, cy) / vm.RadarRangeKm;

            MiniRadarOverlayCanvas.Children.Clear();

            var contacts = vm.Proximity
                .Where(c => c.CPA_Dist_km < 3.0 || c.Distance_km < 5.0)
                .ToList();

            foreach (var c in contacts)
            {
                double x = cx + c.Y_km * scale;
                double y = cy - c.X_km * scale;

                Brush dotBrush = ContactNormal;
                if (c.CPA_Dist_km < 3.0 || c.Distance_km < 5.0)
                    dotBrush = ContactWarn;
                if (c.CPA_Dist_km < 1.0 || c.Distance_km < 2.0)
                    dotBrush = ContactAlarm;

                var outerRing = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Stroke = dotBrush,
                    StrokeThickness = 1.8,
                    Fill = Brushes.Transparent,
                    Opacity = 0.9
                };
                Canvas.SetLeft(outerRing, x - 7);
                Canvas.SetTop(outerRing, y - 7);
                MiniRadarOverlayCanvas.Children.Add(outerRing);

                var core = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.White,
                    Stroke = dotBrush,
                    StrokeThickness = 1.0,
                    Opacity = 0.98
                };
                Canvas.SetLeft(core, x - 3);
                Canvas.SetTop(core, y - 3);
                MiniRadarOverlayCanvas.Children.Add(core);
            }
        }

        private void DrawFullRadarOverlay()
        {
            if (DataContext is not TelemetryViewModel vm || !vm.IsRadarExpanded)
                return;

            double W = FullRadarOverlayCanvas.ActualWidth > 0 ? FullRadarOverlayCanvas.ActualWidth : FullRadarOverlayCanvas.Width;
            double H = FullRadarOverlayCanvas.ActualHeight > 0 ? FullRadarOverlayCanvas.ActualHeight : FullRadarOverlayCanvas.Height;
            double cx = W / 2;
            double cy = H / 2;
            double scale = Math.Min(cx, cy) / vm.RadarRangeKm;

            FullRadarOverlayCanvas.Children.Clear();

            var contacts = vm.Proximity.ToList();

            if (vm.RadarMode == RadarDisplayMode.CriticalOnly)
            {
                contacts = contacts
                    .Where(c => c.CPA_Dist_km < 3.0 || c.Distance_km < 5.0)
                    .ToList();
            }

            foreach (var c in contacts)
            {
                double x = cx + c.Y_km * scale;
                double y = cy - c.X_km * scale;

                Brush dotBrush = ContactNormal;
                if (c.CPA_Dist_km < 3.0 || c.Distance_km < 5.0)
                    dotBrush = ContactWarn;
                if (c.CPA_Dist_km < 1.0 || c.Distance_km < 2.0)
                    dotBrush = ContactAlarm;

                double velScalePx = 36.0;
                double vx = c.Vy_kms * velScalePx;
                double vy = -c.Vx_kms * velScalePx;

                if (vm.RadarMode == RadarDisplayMode.Forecast)
                {
                    var forecast = new Polyline
                    {
                        Stroke = Brushes.White,
                        StrokeThickness = 3.0,
                        Opacity = 0.95,
                        StrokeDashArray = new DoubleCollection { 7, 3 },
                        Effect = (Effect)FindResource("HoloGlow")
                    };

                    Point lastPoint = new Point(x, y);

                    // Ausgangswerte
                    double baseX = c.X_km;
                    double baseY = c.Y_km;
                    double vx_kms = c.Vx_kms;
                    double vy_kms = c.Vy_kms;

                    // Leichte künstliche Kurvenänderung für die Prognose
                    // Je weiter in die Zukunft, desto stärker die Drehung
                    for (int t = 0; t <= 180; t += 5)
                    {
                        double turnFactor = 0.015 * t; // leichte Krümmung
                        double cos = Math.Cos(turnFactor);
                        double sin = Math.Sin(turnFactor);

                        // Geschwindigkeitsvektor leicht rotieren
                        double curvedVx = vx_kms * cos - vy_kms * sin;
                        double curvedVy = vx_kms * sin + vy_kms * cos;

                        double futureX_km = baseX + curvedVx * t;
                        double futureY_km = baseY + curvedVy * t;

                        double fx = cx + futureY_km * scale;
                        double fy = cy - futureX_km * scale;

                        var p = new Point(fx, fy);
                        forecast.Points.Add(p);
                        lastPoint = p;
                    }

                    FullRadarOverlayCanvas.Children.Add(forecast);

                    // Endpunkt markieren
                    var endDot = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.White,
                        Stroke = dotBrush,
                        StrokeThickness = 1.4,
                        Opacity = 0.98,
                        Effect = (Effect)FindResource("StrongGlow")
                    };
                    Canvas.SetLeft(endDot, lastPoint.X - 6);
                    Canvas.SetTop(endDot, lastPoint.Y - 6);
                    FullRadarOverlayCanvas.Children.Add(endDot);
                }

                var outerRing = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Stroke = dotBrush,
                    StrokeThickness = 2.0,
                    Fill = Brushes.Transparent,
                    Opacity = 0.9,
                    Effect = (Effect)FindResource("HoloGlow")
                };
                Canvas.SetLeft(outerRing, x - 9);
                Canvas.SetTop(outerRing, y - 9);
                FullRadarOverlayCanvas.Children.Add(outerRing);

                var core = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = Brushes.White,
                    Stroke = dotBrush,
                    StrokeThickness = 1.0,
                    Opacity = 0.98,
                    Effect = (Effect)FindResource("StrongGlow")
                };
                Canvas.SetLeft(core, x - 3.5);
                Canvas.SetTop(core, y - 3.5);
                FullRadarOverlayCanvas.Children.Add(core);

                if (c.CPA_Dist_km < 1.0 || c.Distance_km < 2.0)
                {
                    var dangerRing = new Ellipse
                    {
                        Width = 26,
                        Height = 26,
                        Stroke = ContactAlarm,
                        StrokeThickness = 1.4,
                        Fill = Brushes.Transparent,
                        Opacity = 0.8,
                        Effect = (Effect)FindResource("StrongGlow")
                    };
                    Canvas.SetLeft(dangerRing, x - 13);
                    Canvas.SetTop(dangerRing, y - 13);
                    FullRadarOverlayCanvas.Children.Add(dangerRing);
                }

                var velocityLine = new Line
                {
                    X1 = x,
                    Y1 = y,
                    X2 = x + vx,
                    Y2 = y + vy,
                    Stroke = HoloArrow,
                    StrokeThickness = 2.2,
                    Opacity = 0.95,
                    Effect = (Effect)FindResource("HoloGlow")
                };
                FullRadarOverlayCanvas.Children.Add(velocityLine);

                var chevron = TrackChevron(x, y, vx, vy, 16, 12, dotBrush);
                FullRadarOverlayCanvas.Children.Add(chevron);

                FullRadarOverlayCanvas.Children.Add(ArrowHead(x + vx, y + vy, x, y, 8, 18, HoloArrow));

                var label = new TextBlock
                {
                    Text = $"TRK-{c.Id:00}  {c.Distance_km:F1} km  {c.Bearing_deg:F0}°  CPA {c.CPA_Dist_km:F1} km  TCPA {(double.IsInfinity(c.TCPA_s) ? "∞" : $"{c.TCPA_s:F0}s")}",
                    Foreground = dotBrush,
                    FontWeight = FontWeights.SemiBold
                };
                Canvas.SetLeft(label, x + 14);
                Canvas.SetTop(label, y - 14);
                FullRadarOverlayCanvas.Children.Add(label);
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

        private Polygon TrackChevron(double centerX, double centerY, double dirX, double dirY, double length, double width, Brush brush)
        {
            double L = Math.Sqrt(dirX * dirX + dirY * dirY);
            if (L < 1e-6) L = 1e-6;

            dirX /= L;
            dirY /= L;

            double qx = -dirY;
            double qy = dirX;

            var tip = new Point(centerX + dirX * (length * 0.6), centerY + dirY * (length * 0.6));
            
            var left = new Point(centerX - dirX * (length * 0.4) + qx * (width * 0.5),
                                 centerY - dirY * (length * 0.4) + qy * (width * 0.5));

            var mid = new Point(centerX - dirX * (length * 0.1),
                                centerY - dirY * (length * 0.1));

            var right = new Point(centerX - dirX * (length * 0.4) - qx * (width * 0.5),
                                  centerY - dirY * (length * 0.4) - qy * (width * 0.5));

            return new Polygon
            {
                Points = new PointCollection { left, tip, right, mid },
                Stroke = brush,
                Fill = Brushes.Transparent,
                StrokeThickness = 1.6,
                Opacity = 0.95,
                Effect = (Effect)FindResource("HoloGlow")
            };
        }
    }
}