using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Schrebergaerten
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Boxen setzen
            BoxDaten = new List<Box>();
        }

        private List<Box> BoxDaten;
        private List<Box> Boxen;
        private Zelle StartZelle;

        private double DX, DY, X0, Y0;
        private double Rand = 40;
        private int GartenBreite = 20;
        private int GartenHoehe = 20;

        // +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+

        private void SetzeSkalierung()
        {
            DX = (CanvasGarten.ActualWidth - 2 * Rand) / GartenBreite;
            DY = (CanvasGarten.ActualHeight - 2 * Rand) / -GartenHoehe;

            if (DX > -DY)
            {
                DX = -DY;
            }
            else
            {
                DY = -DX;
            }

            X0 = (CanvasGarten.ActualWidth / 2) - (DX * GartenBreite) / 2;
            Y0 = (CanvasGarten.ActualHeight / 2) + (DY * -GartenHoehe) / 2;
        }

        private void Zeichne()
        {
            CanvasGarten.Children.Clear();
            SetzeSkalierung();
            ZeichneBoxen();
            ZeichneGitter();

            StatusBarItemInfo.Content = "Länge: " + GartenHoehe + "m | Breite: " + GartenBreite + "m | Fläche: " + Berechnen_Flaeche() + "m²";
        }

        private void ZeichneBoxen()
        {
            if(Boxen == null) { return; }

            foreach(Box B in Boxen)
            {
                if(B.Position == null) { MessageBox.Show("Die Boxen konnten nicht korrekt angezeigt werden!"); return; }

                Point Pos = ZuCanvas(new Point(B.Position.X, B.Position.Y));

                Rectangle Rect = new Rectangle()
                {
                    Height = B.Hoehe * -DY,
                    Width = B.Breite * DX,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(Rect, Pos.X);
                Canvas.SetTop(Rect, Pos.Y - B.Hoehe * -DY);

                CanvasGarten.Children.Add(Rect);
            }
        }

        private void ZeichneGitter()
        {
            // Umriss
            Rectangle Umriss = new Rectangle()
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Width = GartenBreite * DX,
                Height = GartenHoehe * -DY
            };

            Point P = ZuCanvas(new Point() { X = 0, Y = 0 });

            Canvas.SetLeft(Umriss, P.X);
            Canvas.SetTop(Umriss, P.Y - GartenHoehe * -DY);

            CanvasGarten.Children.Add(Umriss);

            // Gitter
            for(int I = 0; I <= GartenHoehe; I++)
            {
                Point P1 = ZuCanvas(new Point() { X = 0, Y = I });
                Point P2 = ZuCanvas(new Point() { X = GartenBreite, Y = I });

                Line Linie = new Line()
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    X1 = P1.X,
                    X2 = P2.X,
                    Y1 = P1.Y,
                    Y2 = P2.Y
                };

                CanvasGarten.Children.Add(Linie);
            }

            // Gitter
            for (int I = 0; I <= GartenBreite; I++)
            {
                Point P1 = ZuCanvas(new Point() { X = I, Y = 0 });
                Point P2 = ZuCanvas(new Point() { X = I, Y = GartenHoehe });

                Line Linie = new Line()
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    X1 = P1.X,
                    X2 = P2.X,
                    Y1 = P1.Y,
                    Y2 = P2.Y
                };

                CanvasGarten.Children.Add(Linie);
            }
        }

        private Point ZuCanvas(Point P)
        {
            return new Point(DX * P.X + X0, DY * P.Y + Y0);
        }

        // +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+

        private void Berechnen()
        {
            Boxen = new List<Box>(BoxDaten);

            if (BoxDaten.Count < 1)
            {
                MessageBox.Show("Es gibt nichts zu berechnen!");
                return;
            }

            // Feld vergrößern das in jedem Fall die Gärten passen
            GartenHoehe = 0;
            GartenBreite = 0;

            foreach (Box B in Boxen)
            {
                GartenHoehe += B.Hoehe;
                GartenBreite += B.Breite;
            }

            // Breite und Höhe gleichzeitig verringern
            while (true)
            {
                if (Berechnen_Packen())
                {
                    GartenBreite--;
                    GartenHoehe--;
                }
                else
                {
                    GartenBreite++;
                    GartenHoehe++;
                    break;
                }
            }

            // Breite verringen
            while (true)
            {
                if(Berechnen_Packen())
                {
                    GartenBreite--;
                }
                else
                {
                    GartenBreite++;
                    break;
                }
            }

            // Höhe verringern
            while (true)
            {
                if (Berechnen_Packen())
                {
                    GartenHoehe--;
                }
                else
                {
                    GartenHoehe++;
                    break;
                }
            }

            // Ausgabe
            Berechnen_Packen();
            Zeichne();
        }

        private bool Berechnen_Packen()
        {
            // Daten zurücksetzen
            StartZelle = new Zelle() { Hoehe = GartenHoehe, Breite = GartenBreite };

            // Absteigend nach Flaeche ordnen
            Boxen.ForEach(x => x.Flaeche = (x.Hoehe * x.Breite));
            Boxen = Boxen.OrderByDescending(x => x.Flaeche).ToList();

            foreach (Box B in Boxen)
            {
                Zelle Z = Berechnen_Suche(StartZelle, B.Hoehe, B.Breite);
                if (Z != null)
                {
                    B.Position = Berechnen_ZelleSpalten(Z, B.Hoehe, B.Breite);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private Zelle Berechnen_Suche(Zelle Start, int Hoehe, int Breite)
        {
            if (Start.Belegt)
            {
                Zelle ZelleNeu = Berechnen_Suche(Start.ZelleUnten, Hoehe, Breite);

                if (ZelleNeu == null)
                {
                    ZelleNeu = Berechnen_Suche(Start.ZelleRechts, Hoehe, Breite);
                }

                return ZelleNeu;
            }
            else if (Breite <= Start.Breite && Hoehe <= Start.Hoehe)
            {
                return Start;
            }
            else
            {
                return null;
            }
        }

        private Zelle Berechnen_ZelleSpalten(Zelle Z, int Hoehe, int Breite)
        {
            Z.Belegt = true;
            Z.ZelleUnten = new Zelle { Y = Z.Y, X = Z.X + Breite, Hoehe = Z.Hoehe, Breite = Z.Breite - Breite };
            Z.ZelleRechts = new Zelle { Y = Z.Y + Hoehe, X = Z.X, Hoehe = Z.Hoehe - Hoehe, Breite = Breite };
            return Z;
        }

        private int Berechnen_Flaeche()
        {
            return GartenBreite * GartenHoehe;
        }

        // +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+

        private void ButtonBerechnen_Click(object sender, RoutedEventArgs e)
        {
            Berechnen();
        }

        private void ButtonBeenden_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            int Laenge, Breite;

            if (!int.TryParse(TextBoxLaenge.Text, out Laenge) || !int.TryParse(TextBoxBreite.Text, out Breite))
            {
                MessageBox.Show("Bitte nur Ganzzahlen eingeben!");
                return;
            }

            ListViewGaerten.Items.Add(new ListViewItem() { Content = Laenge + "x" + Breite });
            BoxDaten.Add(new Box() { Hoehe = Laenge, Breite = Breite });
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            int Index = ListViewGaerten.SelectedIndex;
            if(Index < 0) { return; }

            ListViewGaerten.Items.RemoveAt(Index);
            BoxDaten.RemoveAt(Index);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetzeSkalierung();
            Zeichne();
        }

        internal class Zelle
        {
            public Zelle ZelleUnten;
            public Zelle ZelleRechts;
            public int X;
            public int Y;
            public int Hoehe;
            public int Breite;
            public bool Belegt;
        }

        internal class Box
        {
            public int Hoehe;
            public int Breite;
            public int Flaeche;
            public Zelle Position;
        }
    }
}
