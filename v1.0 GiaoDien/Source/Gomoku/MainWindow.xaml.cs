using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Gomoku
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Ô lớn (chứa các ô nhỏ)
        Rectangle rec;

        //Số lượng ô vuông
        int no_square = 12;
        //Kích thước 1 ô
        int square_height = 30, square_width = 30;
        //Tên user
        string user;

        public MainWindow()
        {
            InitializeComponent();

            cvs_gomoku.Width = no_square * square_width;
            cvs_gomoku.Height = no_square * square_height;
            border.Width = cvs_gomoku.Width + 4;
            border.Height = cvs_gomoku.Height + 4;
            wdw_gomoku.MinHeight = wdw_gomoku.Height = border.Height + 60;
            wdw_gomoku.MinWidth = wdw_gomoku.Width = border.Width + lvw_chat.Width + 60;
            user = tbx_name.Text;
        }

        private void cvs_gomoku_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Xác định vị trí
            Point p = new Point();
            p = e.GetPosition(cvs_gomoku);
            int col, row;
            col = (int)(p.X / square_width) + 1;
            row = (int)(p.Y / square_height) + 1;

            //Tô màu đỏ cho ô được chọn
            Brush color = Brushes.Red;
            rec = createRec(square_width, square_height, square_width*(col-1), square_height*(row-1), color);
            cvs_gomoku.Children.Add(rec);

            //Thông báo ô được chọn
            MessageBox.Show("Bạn vừa chọn ô ở hàng " + row + " và cột " + col, "Bấm nút", MessageBoxButton.OK);
            
            //Tô lại màu cũ cho ô được chọn
            if (col % 2 == row % 2)
                color = Brushes.White;
            else
                color = Brushes.Gray;
            rec = createRec(square_width, square_height, square_width * (col - 1), square_height * (row - 1), color);
            cvs_gomoku.Children.Add(rec);
        }
        private Rectangle createRec(int _width, int _height, int _x, int _y, Brush color)
        {
            var rec = new Rectangle {
                            Height = _height,
                            Width= _width,
                            Fill = color
                        };
            Canvas.SetLeft(rec, _x);
            Canvas.SetTop(rec, _y);
            return rec;
        }

        private void resize()
        {
            border.Width = wdw_gomoku.Width - lvw_chat.Width - 60;
            border.Height = wdw_gomoku.Height - 60;
            square_height = (int.Parse(border.Height.ToString()) - 4) / no_square;
            square_width = (int.Parse(border.Width.ToString()) - 4) / no_square;
            cvs_gomoku.Height = square_height * no_square;
            cvs_gomoku.Width = square_width * no_square;
            border.Width = cvs_gomoku.Width + 4;
            border.Height = cvs_gomoku.Height + 4;
        }
        private void btn_sendmes_Click(object sender, RoutedEventArgs e)
        {
            string mes = tbx_mes.Text;
            if (mes == "")
                mes = "Type something!!!";
            mes = user + ": " + mes + getTime();
            tbx_mes.Clear();
            lvw_chat.Items.Add(mes);
        }

        private string getTime()
        {
            string time = "\n(" +
                       DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + "-" +
                       DateTime.Now.Day + "/" + DateTime.Now.Month + "/" + DateTime.Now.Year +
                       ")";
            return time;
        }


        private void wdw_gomoku_Loaded(object sender, RoutedEventArgs e)
        {
            resize();
            cvs_gomoku.Children.Clear();
            for (int i = 0; i < no_square; i++)
            {
                for (int j = 0; j < no_square; j++)
                {
                    Brush color;
                    if (i % 2 == j % 2)
                        color = Brushes.White;
                    else
                        color = Brushes.Gray;
                    rec = createRec(square_width, square_height, square_width * i, square_height * j, color);
                    cvs_gomoku.Children.Add(rec);
                }
            }
        }

        private void btn_resize_Click(object sender, RoutedEventArgs e)
        {
            wdw_gomoku_Loaded(sender, e);
        }

        private void btn_name_Click(object sender, RoutedEventArgs e)
        {
            if (user != tbx_name.Text)
            {
                string mes;
                mes = "Server: " + user;
                user = tbx_name.Text;
                mes = mes + " is now called " + user + getTime();
                lvw_chat.Items.Add(mes);
            }
        }
    }
}
