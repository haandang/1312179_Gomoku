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
using System.Threading;

using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;

namespace Gomoku
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Biến
        //Ô lớn (chứa các ô nhỏ)
        Rectangle rec;

        //Số lượng ô vuông
        int no_cell = 12;
        //Kích thước 1 ô
        int cell_height = 30, cell_width = 30;
        //Tên user
        string user;

        Brush color1;   //color cua nguoi choi 1
        Brush color2;   //color cua nguoi choi 2 va COM
        
        //Cờ đánh dầu người chơi
        //true:      1vs2
        //false:  1vsCOM
        public bool flag_player = true;
        
        //cờ đánh dấu chơi vs người hoặc máy.
        bool flag_game;
        //Ma trận:
        public int[,] matrix;

        //
        bool flag_online;
        //
        Socket socket;


        #endregion
        
        public MainWindow()
        {
            InitializeComponent();
            cvs_gomoku.Width = no_cell * cell_width;
            cvs_gomoku.Height = no_cell * cell_height;
            border.Width = cvs_gomoku.Width + 4;
            border.Height = cvs_gomoku.Height + 4;
            wdw_gomoku.MinHeight = wdw_gomoku.Height = border.Height + 60;
            wdw_gomoku.MinWidth = wdw_gomoku.Width = border.Width + lvw_chat.Width + 60;
            user = tbx_name.Text;
            //load();
            createMatrix(no_cell);
            flag_game = true; //1vs2
            string mes = "Server: 1 vs 1\nPlayer 1: Red - Player 2: Blue.";
            mes = mes + getTime();
            lvw_chat.Items.Add(mes);
            newGame();
            color1 = Brushes.Red;
            color2 = Brushes.Blue;

            flag_online = true;
            connectServer();
        }
        #region Các hàm xử lý sự kiện trên form
        private void cvs_gomoku_MouseDown(object sender, MouseButtonEventArgs e)
        {
            

            //Xác định vị trí
            Point p = new Point();
            p = e.GetPosition(cvs_gomoku);
            int _col, _row;
            _col = (int)(p.X / cell_width);
            _row = (int)(p.Y / cell_height);
            //Gửi thông tin về server
            socket.On("NextStepIs", (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();

                    lvw_chat.Items.Add("NextStepIs: " + s);
                    socket.Emit("MyStepIs", JObject.FromObject(new { row = _row, col = _col }));

                }));

            });

            //if (matrix[col, row] != 0)
            //{
            //    //MessageBox.Show("Ô đã được chọn", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            //    string mes = "Server: Not allow!!!";
            //    mes = mes + getTime();
            //    lvw_chat.Items.Add(mes);
            //}
            //else
            //{
            //    Ellipse cell_elip = new Ellipse();

            //    if (flag_player)
            //    {
            //        //nguoi choi 1
            //        cell_elip = createElip(cell_width - 4, cell_height - 4, col, row, color1);
            //        matrix[col, row] = 1;
            //        cvs_gomoku.Children.Add(cell_elip);
            //        //Thay doi luot choi
            //        flag_player = !flag_player;
            //        //kiểm tra thắng thua.
            //        if (checkWinner(!flag_player, col, row))
            //        {
            //            showWinner();
            //            //MessageBox
            //        }                  

            //        //toi luot may neu choi 1vsCOM
            //        if (!flag_game)
            //        {
            //            //Phát sinh nước đi của máy
            //            do
            //            {
            //                Random rand = new Random();
            //                row = rand.Next(0, 11);
            //                col = rand.Next(0, 11);
            //            } while (matrix[row, col] != 0);

            //            cell_elip = createElip(cell_width - 4, cell_height - 4, col, row, color2);
            //            matrix[col, row] = 2;
            //            cvs_gomoku.Children.Add(cell_elip);
            //            //Thay doi luot choi
            //            flag_player = !flag_player;
            //            //kiểm tra thắng thua.

            //            if (checkWinner(!flag_player, col, row))
            //            {
            //                showWinner();
            //                //MessageBox
            //            }                
            //        }
            //    }
            //    else
            //    {
            //        if (flag_game)
            //        {
            //            //nguoi choi 2
            //            cell_elip = createElip(cell_width - 4, cell_height - 4, col, row, color2);
            //            matrix[col, row] = 2;
            //            cvs_gomoku.Children.Add(cell_elip);
            //            //Thay doi luot choi
            //            flag_player = !flag_player;
            //            //kiểm tra thắng thua.
            //            if (checkWinner(!flag_player, col, row))
            //            {
            //                showWinner();
            //                //MessageBox
            //            }

            //        }
            //    }
            //}
        }
        

        
        private void btn_sendmes_Click(object sender, RoutedEventArgs e)
        {
            string mes = tbx_mes.Text;
            if (flag_online == true)
            {
                Semaphore sm = new Semaphore(1, 1);
                sm.WaitOne();
                //Gửi tin nhắn tới server
                socket.On("ChatMessage", (data) =>
                {


                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Welcome!")
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            socket.Emit("MyNameIs", tbx_name.Text);
                            socket.Emit("ConnectToOtherPlayer");

                        }));

                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();
                            lvw_chat.Items.Add(s + "");
                        }));
                    }

                });

                Thread.Sleep(3000);
                sm.Release();
            }
            else
            {
                if (mes == "")
                    mes = "Type something!!!";
                mes = user + " (offline): " + mes + getTime();
            }
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
            load();
        }

        private void wdw_gomoku_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            resize();
            cvs_gomoku.Children.Clear();
            load();
        }

        private void btn_name_Click(object sender, RoutedEventArgs e)
        {
            if (user != tbx_name.Text)
            {
                string mes;
                //mes = "Server: " + user;
                user = tbx_name.Text;
                mes = user;
                //Gửi tên mới tới server


                //mes = mes + " is now called " + user + getTime();
                //lvw_chat.Items.Add(mes);
            }
        }

        private void btn_1vsCOM_Click(object sender, RoutedEventArgs e)
        {
            flag_game = false;
            string mes = "Server: 1 vs COM\nPlayer 1: Red - COM: Blue.";
            mes = mes + getTime();
            lvw_chat.Items.Add(mes);
            newGame();
        }

        private void btn_1vs1_Click(object sender, RoutedEventArgs e)
        {
            flag_game = true;
            string mes = "Server: 1 vs 1\nPlayer 1: Red - Player 2: Blue.";
            mes = mes + getTime();
            lvw_chat.Items.Add(mes);
            newGame();
        }
        #endregion

        #region Các hàm hỗ trợ khác

        public void createMatrix(int size)
        {
            matrix = new int[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix[i, j] = 0;
                }
            }
        }
        //tạo hình elip (quân cờ)
        private Ellipse createElip(int _width, int _height, int _col, int _row, Brush color)
        {
            var elip = new Ellipse
            {
                Height = _height,
                Width = _width,
                Fill = color
            };
            Canvas.SetLeft(elip, _col * cell_width + 2);
            Canvas.SetTop(elip, _row * cell_height + 2);
            return elip;
        }
        //tạo hình chữ nhật (ô bàn cờ)
        private Rectangle createRec(int _width, int _height, int _col, int _row, Brush color)
        {
            var rec = new Rectangle
            {
                Height = _height,
                Width = _width,
                Fill = color
            };
            Canvas.SetLeft(rec, _col * cell_width);
            Canvas.SetTop(rec, _row * cell_height);
            return rec;
        }
        private void load()
        {
            for (int i = 0; i < no_cell; i++)
            {
                for (int j = 0; j < no_cell; j++)
                {
                    Brush color;
                    if (i % 2 == j % 2)
                        color = Brushes.White;
                    else
                        color = Brushes.Gray;
                    rec = createRec(cell_width, cell_height, i, j, color);
                    cvs_gomoku.Children.Add(rec);
                }
            }
        }
        private void showWinner()
        {
            string winner;
            if (flag_game)
                if (!flag_player)
                    winner = "Player 1 win";
                else
                    winner = "Player 2 win";
            else
                        if (!flag_player)
                winner = "Player win";
            else
                winner = "COM win";
            MessageBox.Show(winner, "Winner", MessageBoxButton.OK);
            // flag_player = true;
            newGame();
        }
        private void newGame()
        {
            cvs_gomoku.Children.Clear();
            load();
            createMatrix(no_cell);
            string mes = "Server: First turn: ";
            if (flag_player)
                mes = mes + "Player 1.";
            else
                if (flag_game)
                mes = mes + "Player 2.";
            else
                mes = mes + "COM.";
            mes = mes + getTime();
            lvw_chat.Items.Add(mes);
        }

        private void resize()
        {
            border.Width = wdw_gomoku.Width - lvw_chat.Width - 60;
            border.Height = wdw_gomoku.Height - 60;
            cell_height = (int.Parse(border.Height.ToString()) - 4) / no_cell;
            cell_width = (int.Parse(border.Width.ToString()) - 4) / no_cell;
            cvs_gomoku.Height = cell_height * no_cell;
            cvs_gomoku.Width = cell_width * no_cell;
            border.Width = cvs_gomoku.Width + 4;
            border.Height = cvs_gomoku.Height + 4;
        }

        private bool checkWinner(bool _flag_player, int _col, int _row)
        {
            int kt = (_flag_player == true) ? 1 : 2;
            int count = 1;
            int x = _col, y = _row;
            //kiểm tra theo hàng
            while (y - 1 >= 0 && matrix[x, y - 1] == kt)
            {
                count++;
                y -= 1;
            }
            y = _row;
            while (y + 1 < 12 && matrix[x, y + 1] == kt)
            {
                count++;
                y += 1;
            }
            if (count == 5)
                return true;
            //kiểm tra theo cột:
            x = _col;
            y = _row;
            count = 1;
            while (x - 1 >= 0 && matrix[x - 1, y] == kt)
            {
                count++;
                x -= 1;
            }
            x = _col;
            while (x + 1 < 12 && matrix[x + 1, y] == kt)
            {
                count++;
                x += 1;
            }
            if (count == 5)
                return true;
            //theo chiều chéo trên xuống
            x = _col;
            y = _row;
            count = 1;
            while (x - 1 >= 0 && y - 1 >= 0 && matrix[x - 1, y - 1] == kt)
            {
                count++;
                x -= 1;
                y -= 1;
            }
            x = _col;
            y = _row;
            while (x + 1 < 12 && y + 1 < 12 && matrix[x + 1, y + 1] == kt)
            {
                count++;
                x += 1;
                y += 1;
            }
            if (count == 5)
                return true;
            //theo chiều chéo dưới lên
            x = _col;
            y = _row;
            count = 1;
            while (x - 1 >= 0 && y + 1 >= 0 && matrix[x - 1, y + 1] == kt)
            {
                count++;
                x -= 1;
                y += 1;
            }
            x = _col;
            y = _row;
            while (x + 1 < 12 && y - 1 < 12 && matrix[x + 1, y - 1] == kt)
            {
                count++;
                x += 1;
                y -= 1;
            }
            if (count == 5)
                return true;
            return false;
        }

#endregion

        #region Socket
        private void connectServer()
        {


            Socket socket = IO.Socket("ws://gomoku-lajosveres.rhcloud.com:8000");
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lvw_chat.Items.Add("Connected to Server");
                }));
                //lvw_chat.Items.Add("Enter to begin");
                //lvw_chat.Items.Add("Enter to make your move");
            });

            socket.On(Socket.EVENT_MESSAGE, (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {

                    lvw_chat.Items.Add(data + "");
                }));
            });
            socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();
                    lvw_chat.Items.Add(s + "");
                }));
            });
            //socket.On("ChatMessage", (data) =>
            //{


            //    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Welcome!")
            //    {
            //        this.Dispatcher.Invoke((Action)(() =>
            //        {
            //            socket.Emit("MyNameIs", tbx_name.Text);
            //            socket.Emit("ConnectToOtherPlayer");

            //        }));

            //    }
            //    else
            //    {
            //        this.Dispatcher.Invoke((Action)(() =>
            //        {
            //            String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();
            //            lvw_chat.Items.Add(s + "");
            //        }));
            //    }

            //});
            socket.On(Socket.EVENT_ERROR, (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lvw_chat.Items.Add(data + "");
                }));
            });
            //socket.On("NextStepIs", (data) =>
            //{
            //    this.Dispatcher.Invoke((Action)(() =>
            //    {
            //        String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();

            //        lvw_chat.Items.Add("NextStepIs: " + s);
            //        socket.Emit("MyStepIs", JObject.FromObject(new { row = 5, col = 5 }));

            //    }));

            //});
        }
        private void Event_Socket()
        {


            //socket.Connect();
        }
        #endregion
    }
}
