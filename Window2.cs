using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Uzycie_Manipulatora
{
    class Window2 : Window
    {
        public Window2()
        {
            Grid myGrid = new Grid();

            //Content - domyslna wlasciwosc
            this.Content = myGrid;
            myGrid.RowDefinitions.Add(new RowDefinition() 
            { Height= new GridLength(1.0, GridUnitType.Star),
            
            }
            );

            myGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(100.0, GridUnitType.Pixel)
            }
            );
            Button n = new Button() { Content = "Klick", Background = new SolidColorBrush() { Color = Colors.BlueViolet }, Opacity = 0.5 };
            n.SetValue(Grid.RowProperty,0);
            myGrid.Children.Add(n);

            ItemsControl nn = new ItemsControl();


            //ItemsPanelTemplate nnn=new ItemsPanelTemplate();
            //nnn.VisualTree = new FrameworkTemplate();
            //nn.ItemsPanel  = nnn;

            nn.Items.Add("sdddd");

            nn.SetValue(Grid.RowProperty, 1);
            myGrid.Children.Add(nn);



            //W jaki sposob maja byc ukladane elementy w ItemsControl 
            //czy pionowo czy poziomo


        }



    }
}
