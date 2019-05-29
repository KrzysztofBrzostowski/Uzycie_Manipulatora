using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Uzycie_Manipulatora
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>



    ///////////////////////////////////////////////////////////////////////////////
    /// \file    Window1.xaml.cs 
    ///	\brief   This class provides application main window
    /// \author  Krzysztof Brzostowski
    ///	\date    17.04.2012
    ///	\version 0.1
    /// \remarks \n
    /// \remarks \n
    ///////////////////////////////////////////////////////////////////////////////


    public partial class Window1 : Window
    {
        Manipulator manipulator;
        public Window1()
        {
            this.Loaded += MainWindowLoaded;
            InitializeComponent();
        }


        private int MouseStartX;
        private int MouseStartY;

        private int MouseRectangleX;
        private int MouseRectangleY;
        private int MouseRectangleWidth;
        private int MouseRectangleHeight;

        TextBlock textBlock_KBD = new TextBlock();
        private Polyline polyline1;
        private Polyline polyline2;
        private Polyline[] tab_polyline;
        private TextBlock[] tab_TextBlock;
        private CheckBox[] tab_CheckBox;
        private CheckBox[] tab_Led_CheckBox = new CheckBox[10];//LED
        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            manipulator = new Manipulator(this);//startuje watek, pojawia sie cos na outpucie
            DataContext = manipulator;

            manipulator.HID_ENC_EventHandler += new EventHandler<MyEventArgs>(Handle_HID_ENC);
            manipulator.HID_SWT_EventHandler += new EventHandler<MyEventArgs>(Handle_HID_SWT);
            manipulator.HID_OFN_EventHandler += new EventHandler<MyEventArgs>(Handle_HID_OFN);
            manipulator.HID_KBD_EventHandler += new EventHandler<MyEventArgs>(manipulator_HID_KBD_EventHandler);
            //var przykladUzycia = new PrzykladUzycia(this);


            //narysuj 10 kontrolek: ENC, tab_TextBlock
            tab_polyline = new Polyline[10];//ENC
            tab_TextBlock = new TextBlock[10];//ENC
            tab_CheckBox = new CheckBox[10];//SWT

            //tab_Led_CheckBox = new CheckBox[10];//LED

            for (int i = 0; i < 10; i++)
            {
                //------------------------------------------------------------------------
                tab_polyline[i] = new Polyline();

                tab_polyline[i].Points.Add(new Point(25 / 2, 25 / 2));
                tab_polyline[i].Points.Add(new Point(0 / 2, 50 / 2));
                tab_polyline[i].Points.Add(new Point(25 / 2, 75 / 2));
                tab_polyline[i].Points.Add(new Point(50 / 2, 50 / 2));
                tab_polyline[i].Points.Add(new Point(25 / 2, 25 / 2));
                tab_polyline[i].Points.Add(new Point(25 / 2, 0 / 2));

                tab_polyline[i].Stroke = Brushes.Blue;
                tab_polyline[i].StrokeThickness = 5;

                Canvas.SetLeft(tab_polyline[i], 20 + 80 * i);
                Canvas.SetTop(tab_polyline[i], 30);
                myCanvas.Children.Add(tab_polyline[i]);
                //------------------------------------------------------------------------------------

                tab_TextBlock[i] = new TextBlock();
                tab_TextBlock[i].Width = 50;
                tab_TextBlock[i].Height = 30;
                tab_TextBlock[i].Text = "0";

                Canvas.SetLeft(tab_TextBlock[i], 20 + 80 * i);
                Canvas.SetTop(tab_TextBlock[i], 90);
                myCanvas.Children.Add(tab_TextBlock[i]);
                //-----------------------------------------------------------------------------------
                tab_CheckBox[i] = new CheckBox();
                tab_CheckBox[i].Width = 50;
                tab_CheckBox[i].Height = 30;
                tab_CheckBox[i].IsChecked = false;
                tab_CheckBox[i].IsThreeState = true;
                tab_CheckBox[i].Content = "SWT" + i.ToString();
                tab_CheckBox[i].IsEnabled = false;

                Canvas.SetLeft(tab_CheckBox[i], 20 + 80 * i);
                Canvas.SetTop(tab_CheckBox[i], 140);
                myCanvas.Children.Add(tab_CheckBox[i]);

                //---------------------------------------------------------------------------------------------------------

            }

            MouseStartX = 50 + 200 / 2;
            MouseStartY = 200 + 200 / 2;
            //Ustawienie Kursora Myszki na poczatek

            // Create a Mouse Cursor
            blueMouseCursor = new Rectangle();
            blueMouseCursor.Height = 5;
            blueMouseCursor.Width = 5;

            // Create a blue and a black Brush
            SolidColorBrush blueBrush = new SolidColorBrush();
            blueBrush.Color = Colors.Blue;
            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            // Set Rectangle's width and color
            blueMouseCursor.StrokeThickness = 4;
            blueMouseCursor.Stroke = blueBrush;
            // Fill rectangle with blue color
            blueMouseCursor.Fill = blueBrush;



           

            //ramka, ograniczenie dla myszki
            MouseRectangleX=50;
            MouseRectangleY=200;
            MouseRectangleWidth=200;
            MouseRectangleHeight=194;

            //najpierw pole dla myszy
            CreateMouseRectangle(MouseRectangleX, MouseRectangleY, MouseRectangleWidth, MouseRectangleHeight);

            //pozniej kursor myszy
            SetMouseCursor(MouseStartX, MouseStartY);



            //TextBlock Klawiatury
            //BorderBrush="Black" BorderThickness="4"
            textBlock_KBD.Width = 400;
            textBlock_KBD.Height = 194;
            textBlock_KBD.Text = "";

            //Create a backG Brush
            SolidColorBrush backG = new SolidColorBrush();
            backG.Color = Colors.LightGray;

            textBlock_KBD.Background = backG;

            textBlock_KBD.TextWrapping = TextWrapping.Wrap;

            Canvas.SetLeft(textBlock_KBD, 300);
            Canvas.SetTop(textBlock_KBD, 200);
            myCanvas.Children.Add(textBlock_KBD);

            //ramka dla klawiatury
            //CreateARectangle_KBD(300, 200, 300, 200);


        }

        void manipulator_HID_KBD_EventHandler(object sender, MyEventArgs e)
        {
            int klawisz1 = ((KBD)e.urzadzenie).klawisz1;
            int klawisz2 = ((KBD)e.urzadzenie).klawisz2;

            textBlock_KBD.Text += klawisz1.ToString();
            textBlock_KBD.Text += " ";
            textBlock_KBD.Text += klawisz2.ToString();
            textBlock_KBD.Text += ";";
        }


        void Handle_HID_OFN(object sender, MyEventArgs e)
        {
            int nr_urzadzenia = ((OFN)e.urzadzenie).nr_urzadzenia;
            int dx = ((OFN)e.urzadzenie).DX;
            int dy = ((OFN)e.urzadzenie).DY;

            int x = MouseStartX + dx;
            int y = MouseStartY + dy;

            if  (
                (y < (MouseRectangleY + MouseRectangleHeight)) && 
                (y > MouseRectangleY) && (x > MouseRectangleX) && x < (MouseRectangleX + MouseRectangleWidth)
                )
            {
                myCanvas.Children.Remove(blueMouseCursor);
                SetMouseCursor(x, y);
            }
        }



        void Handle_HID_SWT(object sender, MyEventArgs e)
        {
            int nr_urzadzenia = ((SWT)e.urzadzenie).nr_urzadzenia;
            bool stan = ((SWT)e.urzadzenie).stan;
            string opis = ((SWT) e.urzadzenie).opis;

            //tab_CheckBox[nr_urzadzenia].Content = opis;

            if (((SWT)e.urzadzenie).P_press == true)
            {
                if (stan == true) tab_CheckBox[nr_urzadzenia].IsChecked = null;
                else
                    tab_CheckBox[nr_urzadzenia].IsChecked = false;
            }
        }

        void Handle_HID_ENC(object sender, MyEventArgs e)
        {//dodac slownik?

            //---------------------------------------------------------------------------------------------
            int nr_urzadzenia = ((ENC)e.urzadzenie).nr_urzadzenia;
            double obrot = ((ENC)e.urzadzenie).wartosc;
            obrot *= 360.0 / 24.0;

            RotateTransform rotateTransform1a = new RotateTransform(obrot, 25 / 2, 50 / 2);
            tab_polyline[nr_urzadzenia].RenderTransform = rotateTransform1a;

            myCanvas.Children.Remove(tab_polyline[nr_urzadzenia]);
            Canvas.SetLeft(tab_polyline[nr_urzadzenia], 20 + 80 * nr_urzadzenia);
            Canvas.SetTop(tab_polyline[nr_urzadzenia], 30);

            myCanvas.Children.Add(tab_polyline[nr_urzadzenia]);
            //---------------------------------------------------------------------------------------------------        
            tab_TextBlock[nr_urzadzenia].Text = ((ENC)e.urzadzenie).wartosc.ToString();
            //---------------------------------------------------------------------------------------------------      

        }

        private Rectangle blueMouseCursor;
        public void SetMouseCursor(int x, int y)
        {
            Canvas.SetLeft(blueMouseCursor, x);
            Canvas.SetTop(blueMouseCursor, y);

            // Add Rectangle to the Grid.
            myCanvas.Children.Add(blueMouseCursor);
        }

        /// <summary>
        /// Mouse Rectangle
        /// </summary>
        public void CreateMouseRectangle(int x, int y, int width, int height)
        {
            // Create a Rectangle (Ograniczenie myszki)
            Rectangle blueRectangle = new Rectangle();

            blueRectangle.Height = height;
            blueRectangle.Width = width;


            // Create a Gray and a black Brush
            SolidColorBrush blueBrush = new SolidColorBrush();
            blueBrush.Color = Colors.LightGray;

            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            // Set Rectangle's width and color
            blueRectangle.StrokeThickness = 4;
            blueRectangle.Stroke = blackBrush;

            // Fill rectangle with Gray color
            blueRectangle.Fill = blueBrush;

            Canvas.SetLeft(blueRectangle, x);
            Canvas.SetTop(blueRectangle, y);

            // Add Rectangle to the Grid.
            myCanvas.Children.Add(blueRectangle);
        }


        /// <summary>
        /// KeyBoard Rectangle
        /// </summary>
        public void CreateARectangle_KBD(int x, int y, int width, int height)
        {
            // Create a Rectangle (Ograniczenie myszki)
            Rectangle blueRectangle = new Rectangle();

            blueRectangle.Height = height;
            blueRectangle.Width = width;

            // Create a Gray and a black Brush
            SolidColorBrush blueBrush = new SolidColorBrush();
            blueBrush.Color = Colors.LightGray;

            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            // Set Rectangle's width and color
            blueRectangle.StrokeThickness = 4;
            blueRectangle.Stroke = blackBrush;

            // Fill rectangle with Gray color
            blueRectangle.Fill = blueBrush;

            Canvas.SetLeft(blueRectangle, x);
            Canvas.SetTop(blueRectangle, y);

            // Add Rectangle to the Grid.
            myCanvas.Children.Add(blueRectangle);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            textBlock_KBD.Text = String.Empty;
        }


    }
}
