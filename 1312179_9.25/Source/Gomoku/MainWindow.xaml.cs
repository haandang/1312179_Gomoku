using System;
using System.Collections.Generic;
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
        //
        object synch = new object();
        object synch_pc_ol = new object();
        /// <summary>
        /// Biên người chơi, COM, server
        /// </summary>
        CPlayer player_user, player_pc, player_server;
        int testshot = 2;
        int testshot_online = 1;

        /// <summary>
        /// Bàn cờ (chứa các ô nhỏ)
        /// </summary>
        Rectangle rec;
        /// <summary>
        /// Số lượng ô cờ trên 1 hàng hoặc cột
        /// </summary>
        int cell_quantity = 12;
        /// <summary>
        /// Kích thước 1 ô mặc định
        /// </summary>
        int cell_height = 30, cell_width = 30;
        /// <summary>
        /// Tên người chơi
        /// </summary>
        string user;
        /// <summary>
        /// Màu quân cờ của người chơi, COM, server
        /// </summary>
        Brush color1, color2;   

        /// <summary>
        /// Ma trận lưu nước đi
        /// </summary>
        public int[,] matrix;

        /// <summary>
        /// Socket
        /// </summary>
        Socket socket;

        #endregion

        /// <summary>
        /// Hàm dựng mặc định
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            //Thiet lap ban co ban dau
            cvs_gomoku.Width = cell_quantity * cell_width;
            cvs_gomoku.Height = cell_quantity * cell_height;
            border.Width = cvs_gomoku.Width + 4;
            border.Height = cvs_gomoku.Height + 4;
            wdw_gomoku.MinHeight = wdw_gomoku.Height = border.Height + 60;
            wdw_gomoku.MinWidth = wdw_gomoku.Width = border.Width + lvw_chat.Width + 60;

            user = tbx_name.Text;
            createMatrix(cell_quantity);
            
            color1 = Brushes.Red;
            color2 = Brushes.Blue;
            
            //chế độ chơi mặc đinh
            player_user = new CPlayer(color1, (int)EPlayerFlag.Player1);
            player_user.State = true;
            player_pc = new CPlayer(color2, (int)EPlayerFlag.Player2);
            player_server = new CPlayer(color2, (int)EPlayerFlag.Server);
            newGame();

        }
        #region Các hàm xử lý sự kiện trên form
        /// <summary>
        /// Xử lý sự kiện mousedown trên bàn cờ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cvs_gomoku_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock (synch)
            {
                int col = -1, row = -1;

                Point p = new Point();
                p = e.GetPosition(cvs_gomoku);
                col = (int)(p.X / cell_width);
                row = (int)(p.Y / cell_height);

                //xác định vị trí người chơi nhấp vào:
                if (checkPoint(col, row))    //Kiểm tra ví trí chọn có hợp lệ
                {
                    //xét trường hợp: người chơi nhấp vào bàn cờ
                    /*TH1: Người chơi có chọn chế độ chơi
                     * TH1.1 Người chơi chơi offline
                    */
                    if (player_user.State == true)
                    {
                        if (player_server.State == false)
                        {
                            //chế độ người chơi offine 1vs2
                            player_user = new CPlayer(row, col, true);
                            //kiểm tra chơi người vs người
                            if (player_pc.State == false)
                            {
                                testshot = 3 - testshot;
                                player_user.PlayerFlag = testshot;
                                player_user.ColorPlayer = testshot == (int)EPlayerFlag.Player1 ? color1 : color2; 
                                drawGomoku(player_user);
                                if (checkWinner(player_user))
                                    showWinner(testshot);
                            }
                            else //1vsCOM
                            {
                                player_user = new CPlayer(row, col, true, color1, (int)EPlayerFlag.Player1);
                                drawGomoku(player_user);
                                if (checkWinner(player_user))
                                    showWinner((int)EPlayerFlag.Player1);
                                else
                                {
                                    player_pc = findWayforPC(1, 2);
                                    drawGomoku(player_pc);
                                    if (checkWinner(player_pc))
                                        showWinner((int)EPlayerFlag.COM);
                                }
                                testshot = 2;
                            }
                        }
                        else //chế độ online
                        {
                            if (player_pc.State == false)
                            {
                                if (testshot_online == 3)
                                {
                                    testshot_online = 4 - testshot_online;
                                    player_user = new CPlayer(row, col, true, color1, (int)EPlayerFlag.Player1);
                                    drawGomoku(player_user);
                                    socket.Emit("MyStepIs", JObject.FromObject(new { row = row, col = col }));
                                    if (checkWinner(player_user))
                                        showWinner((int)EPlayerFlag.Player1);
                                }
                                else
                                {
                                    lvw_chat.Items.Add("Server: This is not your turn" + getTime());
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút Send
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_sendmes_Click(object sender, RoutedEventArgs e)
        {
            if (player_server.State == true)
            {
                socket.Emit("ChatMessage", tbx_mes.Text);
            }
            else
            {
                string mes = tbx_mes.Text;
                if (mes == "")
                    mes = "Type something!!!";
                mes = user + ": " + mes + getTime();
                lvw_chat.Items.Add(mes);
            }
            tbx_mes.Clear();
        }

        /// <summary>
        /// Xử lý sự kiện load form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wdw_gomoku_Loaded(object sender, RoutedEventArgs e)
        {
            drawBoard();
        }

        /// <summary>
        /// Xử lý sự kiện thay đổi kích thước form (thay đổi kích thước bàn cờ)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wdw_gomoku_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            resizeBoard();
            cvs_gomoku.Children.Clear();
            drawBoard();
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút đổi tên
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_name_Click(object sender, RoutedEventArgs e)
        {
            if (user != tbx_name.Text)
            {
                if (player_server.State == true)
                {
                    user = tbx_name.Text;
                    socket.Emit("MyNameIs", user);
                }
                else
                {
                    string mes;
                    mes = "Server offline: " + user;
                    user = tbx_name.Text;
                    mes = mes + " is now called " + user + getTime();
                    lvw_chat.Items.Add(mes);
                }
            }
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút New game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newgame_Click(object sender, RoutedEventArgs e)
        {
            newGame();
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút Exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exit_Click(object sender, RoutedEventArgs e)
        {
            socket.Disconnect();
            this.Close();
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút About
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void about_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gomoku\n" + "Thực hiện: 1312179 - Đặng Văn Quốc Hân\n" +
                "Đồ án môn học Lập trình Windows\n" + "GVHD: Nguyễn Huy Khánh", "ABOUT GOMOKU");
        }

        /// <summary>
        /// Xử lý sự kiến chuột rời khỏi các nút (trong menu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseleave_Item(object sender, MouseEventArgs e)
        {
            MenuItem mitem = (MenuItem)sender;
            mitem.FontWeight = FontWeights.Normal;
        }

        /// <summary>
        /// Xử lý sự kiến chuột đưa vào các nút (trong menu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mousemove_Item(object sender, MouseEventArgs e)
        {
            MenuItem mitem = (MenuItem)sender;
            mitem.FontWeight = FontWeights.Bold;
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút Play offline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mode_offline_Click(object sender, RoutedEventArgs e)
        {
            player_server.State = false;
            socket.Close();
            newGame();
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút 1 vs 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mode_1vs2_Click(object sender, RoutedEventArgs e)
        {
            player_user.State = true;
            player_pc.State = false;
            //player_server.State = false;
            newGame();
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút 1 vs COM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mode_1vsCOM_Click(object sender, RoutedEventArgs e)
        {
            if (player_server.State == false)
            {
                player_user.State = true;
                player_pc.State = true;
            }
            else
            {
                player_user.State = false;
                player_pc.State = true;
            }
            newGame();
        }

        /// <summary>
        /// Xử lý sự kiện nhấn nút Play online
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mode_online_Click(object sender, RoutedEventArgs e)
        {
            player_server.State = true;
            connectServer();
            newGame();
        }
        #endregion

        #region Các hàm hỗ trợ khác
        /// <summary>
        /// Lấy thời gian hệ thống
        /// </summary>
        /// <returns>Chuỗi thời gian hệ thống</returns>
        private string getTime()
        {
            string time = "\n(" +
                       DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + "-" +
                       DateTime.Now.Day + "/" + DateTime.Now.Month + "/" + DateTime.Now.Year +
                       ")";
            return time;
        }

        /// <summary>
        /// Khởi tạo ma trận lưu nước đi
        /// </summary>
        /// <param name="size">Kích thước của bàn cờ (size x size)</param>
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
        
        /// <summary>
        /// Tạo hình elip (quân cờ)
        /// </summary>
        /// <param name="_width">Chiều rộng</param>
        /// <param name="_height">Chiều cao</param>
        /// <param name="_col">Vị trí (cột)</param>
        /// <param name="_row">Vị trí (hàng)</param>
        /// <param name="_color">Màu sắc</param>
        /// <returns>Đối tượng elip</returns>
        private Ellipse createElip(int _width, int _height, int _col, int _row, Brush _color)
        {
            var elip = new Ellipse
            {
                Height = _height,
                Width = _width,
                Fill = _color
            };
            Canvas.SetLeft(elip, _col * cell_width + 2);
            Canvas.SetTop(elip, _row * cell_height + 2);
            return elip;
        }

        /// <summary>
        /// Tạo hình chữ nhật (ô cờ)
        /// </summary>
        /// <param name="_width">Chiều rộng</param>
        /// <param name="_height">Chiều cao</param>
        /// <param name="_col">Vị trí (cột)</param>
        /// <param name="_row">Vị trí (hàng)</param>
        /// <param name="_color">Màu sắc</param>
        /// <returns>Đối tượng hình chữ nhật</returns>
        private Rectangle createRec(int _width, int _height, int _col, int _row, Brush _color)
        {
            var rec = new Rectangle
            {
                Height = _height,
                Width = _width,
                Fill = _color
            };
            Canvas.SetLeft(rec, _col * cell_width);
            Canvas.SetTop(rec, _row * cell_height);
            return rec;
        }

        /// <summary>
        /// Vẽ bàn cờ
        /// </summary>
        private void drawBoard()
        {
            for (int i = 0; i < cell_quantity; i++)
            {
                for (int j = 0; j < cell_quantity; j++)
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

        /// <summary>
        /// Thông báo người thắng
        /// </summary>
        /// <param name="playerflag">Cờ người thắng</param>
        private void showWinner(int playerflag)
        {
            string winner = "";
            switch (playerflag)
            {
                case (int)EPlayerFlag.Player1:
                    winner = "Player 1 win";
                    break;
                case (int)EPlayerFlag.Player2:
                    winner = "Player 2 win";
                    break;
                case (int)EPlayerFlag.COM:
                    winner = "COM win";
                    break;
                case (int)EPlayerFlag.Server:
                    winner = "Server win";
                    break;
            }
            MessageBox.Show(winner, "Winner", MessageBoxButton.OK);
            newGame();
        }

        /// <summary>
        /// Tạo ván chơi mới
        /// </summary>
        private void newGame()
        {
            //Xóa bàn cờ
            cvs_gomoku.Children.Clear();
            //Vẽ lại bàn cờ
            drawBoard();
            //Tạo lại ma trận mới
            createMatrix(cell_quantity);

            string mes = "";
            if (player_server.State == false)
            {
                mes += "Sever ofline: Ván mới - Chế độ chơi offline. \n";
                if (player_pc.State == false)
                {
                    mes += "1 vs 2 \n";
                    mes += "First turn: ";
                    mes += (testshot == 1) ? "Player 2 - BLUE" : "Player 1 - RED";
                    mes = mes + getTime();
                    lvw_chat.Items.Add(mes);
                }
                else
                {
                    mes += "1 vs COM \n";
                    mes += "First turn: Player 1 - RED";
                    mes = mes + getTime();
                    lvw_chat.Items.Add(mes);
                }
            }
            else
            {
                mes += "Sever: Ván mới - Chế độ chơi online." + getTime();
                lvw_chat.Items.Add(mes);
                //socket.
                socket.Emit("ConnectToOtherPlayer");
            }
        }

        /// <summary>
        /// Thay đổi kích thước bàn cờ theo kích thước cửa sổ
        /// </summary>
        private void resizeBoard()
        {
            border.Width = wdw_gomoku.Width - lvw_chat.Width - 60;
            border.Height = wdw_gomoku.Height - 75;
            cell_height = (int.Parse(border.Height.ToString()) - 4) / cell_quantity;
            cell_width = (int.Parse(border.Width.ToString()) - 4) / cell_quantity;
            cvs_gomoku.Height = cell_height * cell_quantity;
            cvs_gomoku.Width = cell_width * cell_quantity;
            border.Width = cvs_gomoku.Width + 4;
            border.Height = cvs_gomoku.Height + 4;
        }



        /// <summary>
        /// Kiểm tra xem vị trí đã được đánh chưa
        /// </summary>
        /// <param name="col">Cột</param>
        /// <param name="row">Hàng</param>
        /// <returns>Cờ kiểm tra xem vị trí đã được đánh chưa</returns>
        private bool checkPoint(int col, int row)
        {
            return (matrix[col, row] == 0) ? true : false;
        }

        /// <summary>
        /// Kiểm tra người thắng sau khi đi 1 nước
        /// </summary>
        /// <param name="pOject">Đối tượng được kiểm tra</param>
        /// <returns>Cờ cho biết là đã thắng chưa</returns>
        private bool checkWinner(CPlayer pOject)
        {
            int kt = pOject.PlayerFlag;
            int count = 1;
            int _row = pOject.Row, _col = pOject.Column;
            int x = pOject.Column, y = pOject.Row;
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
            while (x - 1 >= 0 && y + 1 < 12 && matrix[x - 1, y + 1] == kt)
            {
                count++;
                x -= 1;
                y += 1;
            }
            x = _col;
            y = _row;
            while (x + 1 < 12 && y - 1 > 0 && matrix[x + 1, y - 1] == kt)
            {
                count++;
                x += 1;
                y -= 1;
            }
            if (count == 5)
                return true;
            return false;
        }

        /// <summary>
        /// Tìm kiếm nước đi cho COM
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>Nước đi mới được lưu trong đối tượng CPlayer mới</returns>
        private CPlayer findWayforPC(int p1, int p2)
        {
            int col, row;
            do
            {
                Point p = TimKiemNuocDi(p1, p2);
                col = (int)p.X;
                row = (int)p.Y;
            }
            while (matrix[col, row] != 0);
            CPlayer pc_new = new CPlayer(row, col, true, color2, (int)EPlayerFlag.COM);
            return pc_new;
        }
        
        /// <summary>
        /// Vẽ cờ lên bàn cờ
        /// </summary>
        /// <param name="pObject">Đối tượng được vẽ</param>
        /// <returns></returns>
        private bool drawGomoku(CPlayer pObject)
        {
            //Xét ô đã được đánh
            checkPoint(pObject.Column, pObject.Row);

            Ellipse cell_elip = createElip(cell_width - 4, cell_height - 4, pObject.Column, pObject.Row, pObject.ColorPlayer);
            matrix[pObject.Column, pObject.Row] = pObject.PlayerFlag;
            cvs_gomoku.Children.Add(cell_elip);
            return true;
        }
        #endregion

        #region Socket
        /// <summary>
        /// Kết nối
        /// </summary>
        private void connectServer()
        {
            socket = IO.Socket(System.Configuration.ConfigurationSettings.AppSettings["IPConnectGomoku"]);

            socket.On(Socket.EVENT_CONNECT, () =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lvw_chat.Items.Add("Server: Connected to Server" + getTime());
                }));
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
                    lvw_chat.Items.Add("connect Error");
                }));
            });


            socket.On("ChatMessage", (data) =>
            {
                if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Welcome!")
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        socket.Emit("MyNameIs", user);
                       // socket.Emit("ConnectToOtherPlayer");
                    }));
                }
                else
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        String name = "";
                        String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();
                        s = strimMes(s);
                        if (name == "")
                            name = "Server: ";
                        else
                            name += ": ";
                        lvw_chat.Items.Add(name + s + getTime());
                    }));
                }

            });
            socket.On(Socket.EVENT_ERROR, (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lvw_chat.Items.Add(data + " event error");
                }));
            });
            socket.On("NextStepIs", (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lock (synch)
                    {
                        int row = (int)((JObject)data).GetValue("row");
                        int col = (int)((JObject)data).GetValue("col");
                        if (checkPoint(col, row))
                        {
                            testshot_online = 3;
                            player_server = new CPlayer(row, col, true, color2, (int)EPlayerFlag.Server);
                            drawGomoku(player_server);
                            if (checkWinner(player_server))
                            {
                                showWinner((int)EPlayerFlag.Server);
                                newGame();
                            }
                            else
                            {
                                //String s = "Your turn!";
                                //lvw_chat.Items.Add(s);
                            }
                            //Tính nước chơi cho COM
                            if (player_pc.State == true)
                            {
                                lock (synch_pc_ol)
                                {
                                    player_pc = findWayforPC(3, 1);
                                    player_pc.ColorPlayer = color1;
                                    player_pc.State = true;
                                    drawGomoku(player_pc);
                                    socket.Emit("MyStepIs", JObject.FromObject(new { row = player_pc.Row, col = player_pc.Column }));
                                    if (checkWinner(player_pc))
                                    {
                                        showWinner((int)EPlayerFlag.Player1);
                                        newGame();
                                    }
                                }
                            }
                        }
                    }
                }));
            });
        }
     
        /// <summary>
        /// Xóa tag <br /> và thay bằng \n
        /// </summary>
        /// <param name="s">Chuỗi có tag <br /></param>
        /// <returns></returns>
        private String strimMes(string s)
        {
            for (int i = 25; i < s.Length - 6; i++)
            {
                if (s[i] == '<' && s[i + 1] == 'b' && s[i + 2] == 'r' && s[i + 3] == ' ' && s[i + 4] == '/' && s[i + 5] == '>')
                {                    
                    string s1 = s.Substring(i+6);
                    if (s1.Equals("You are the first player!"))
                    {
                        testshot_online = 3;
                        if (player_pc.State == true)
                        {
                            Random rd = new Random();
                            player_pc = new CPlayer(rd.Next(0, 11), rd.Next(0, 11), true, color1, (int)EPlayerFlag.COM);
                            drawGomoku(player_pc);
                            socket.Emit("MyStepIs", JObject.FromObject(new { row = player_pc.Row, col = player_pc.Column }));
                            testshot_online = 2;
                        }
                    }
                    else
                    {
                        testshot_online = 1;
                    }
                    return s = (s.Remove(i, 6)).Insert(i, "\n");
                }
            }
            return s;
        }

        #endregion

        #region Tính nước đi cho COM
        // Nguồn tham khảo từ INTERNET
        private long[] MangDiemTanCong = new long[7] { 0, 9, 54, 162, 1458, 13112, 118008 };
        private long[] MangDiemPhongNgu = new long[7] { 0, 3, 27, 99, 729, 6561, 59049 };
        // p1: đối thủ
        // p2: 
        private Point TimKiemNuocDi(int p1, int p2)
        {
            Point oCoResult = new Point();
            long DiemMax = 0;
            for (int i = 0; i < cell_quantity; i++)
            {
                for (int j = 0; j < cell_quantity; j++)
                {
                    if (matrix[i, j] == 0)
                    {
                        long DiemTanCong = DiemTanCong_DuyetDoc(i, j, p1, p2) + DiemTanCong_DuyetNgang(i, j, p1, p2) + DiemTanCong_DuyetCheoNguoc(i, j, p1, p2) + DiemTanCong_DuyetCheoXuoi(i, j, p1, p2);
                        long DiemPhongNgu = DiemPhongNgu_DuyetDoc(i, j, p1, p2) + DiemPhongNgu_DuyetNgang(i, j, p1, p2) + DiemPhongNgu_DuyetCheoNguoc(i, j, p1, p2) + DiemPhongNgu_DuyetCheoXuoi(i, j, p1, p2);
                        long DiemTam = DiemTanCong > DiemPhongNgu ? DiemTanCong : DiemPhongNgu;
                        if (DiemMax < DiemTam)
                        {
                            DiemMax = DiemTam;
                            oCoResult = new Point(i, j);

                        }
                    }
                }
            }

            return oCoResult;
        }
        #region Tấn công
        private long DiemTanCong_DuyetDoc(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currDong + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong + Dem, currCot] == p1)
                    SoQuanTa++;
                else if (matrix[currDong + Dem, currCot] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currDong - Dem >= 0; Dem++)
            {
                if (matrix[currDong - Dem, currCot] == p1)
                    SoQuanTa++;
                else if (matrix[currDong - Dem, currCot] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }
            if (SoQuanDich == 2)
                return 0;
            DiemTong -= MangDiemPhongNgu[SoQuanDich + 1] * 2;
            DiemTong += MangDiemTanCong[SoQuanTa];
            return DiemTong;
        }
        private long DiemTanCong_DuyetNgang(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currCot + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong, currCot + Dem] == p1)
                    SoQuanTa++;
                else if (matrix[currDong, currCot + Dem] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currCot - Dem >= 0; Dem++)
            {
                if (matrix[currDong, currCot - Dem] == p1)
                    SoQuanTa++;
                else if (matrix[currDong, currCot - Dem] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }
            if (SoQuanDich == 2)
                return 0;
            DiemTong -= MangDiemPhongNgu[SoQuanDich + 1] * 2;
            DiemTong += MangDiemTanCong[SoQuanTa];
            return DiemTong;
        }
        private long DiemTanCong_DuyetCheoNguoc(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currCot + Dem < cell_quantity && currDong - Dem >= 0; Dem++)
            {
                if (matrix[currDong - Dem, currCot + Dem] == p1)
                    SoQuanTa++;
                else if (matrix[currDong - Dem, currCot + Dem] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currCot - Dem >= 0 && currDong + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong + Dem, currCot - Dem] == p1)
                    SoQuanTa++;
                else if (matrix[currDong + Dem, currCot - Dem] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }
            if (SoQuanDich == 2)
                return 0;
            DiemTong -= MangDiemPhongNgu[SoQuanDich + 1] * 2;
            DiemTong += MangDiemTanCong[SoQuanTa];
            return DiemTong;
        }
        private long DiemTanCong_DuyetCheoXuoi(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currCot + Dem < cell_quantity && currDong + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong + Dem, currCot + Dem] == p1)
                    SoQuanTa++;
                else if (matrix[currDong + Dem, currCot + Dem] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currCot - Dem >= 0 && currDong - Dem >= 0; Dem++)
            {
                if (matrix[currDong - Dem, currCot - Dem] == p1)
                    SoQuanTa++;
                else if (matrix[currDong - Dem, currCot - Dem] == p2)
                {
                    SoQuanDich++;
                    break;
                }
                else
                    break;
            }
            if (SoQuanDich == 2)
                return 0;
            DiemTong -= MangDiemPhongNgu[SoQuanDich + 1] * 2;
            DiemTong += MangDiemTanCong[SoQuanTa];
            return DiemTong;
        }
        #endregion Tấn công
        #region Phòng ngự
        private long DiemPhongNgu_DuyetDoc(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currDong + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong + Dem, currCot] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong + Dem, currCot] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currDong - Dem >= 0; Dem++)
            {
                if (matrix[currDong - Dem, currCot] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong - Dem, currCot] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }
            if (SoQuanTa == 2)
                return 0;
            DiemTong += MangDiemPhongNgu[SoQuanDich];
            return DiemTong;
        }

        private long DiemPhongNgu_DuyetNgang(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currCot + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong, currCot + Dem] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong, currCot + Dem] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currCot - Dem >= 0; Dem++)
            {
                if (matrix[currDong, currCot - Dem] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong, currCot - Dem] == p2)
                {
                    SoQuanDich++;

                }
                else
                    break;
            }
            if (SoQuanTa == 2)
                return 0;
            DiemTong += MangDiemPhongNgu[SoQuanDich];
            return DiemTong;
        }
        private long DiemPhongNgu_DuyetCheoNguoc(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currCot + Dem < cell_quantity && currDong - Dem >= 0; Dem++)
            {
                if (matrix[currDong - Dem, currCot + Dem] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong - Dem, currCot + Dem] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currCot - Dem >= 0 && currDong + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong + Dem, currCot - Dem] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong + Dem, currCot - Dem] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }
            if (SoQuanTa == 2)
                return 0;
            DiemTong += MangDiemPhongNgu[SoQuanTa];
            return DiemTong;
        }
        private long DiemPhongNgu_DuyetCheoXuoi(int currDong, int currCot, int p1, int p2)
        {
            long DiemTong = 0;
            int SoQuanTa = 0;
            int SoQuanDich = 0;
            for (int Dem = 1; Dem < 6 && currCot + Dem < cell_quantity && currDong + Dem < cell_quantity; Dem++)
            {
                if (matrix[currDong + Dem, currCot + Dem] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong + Dem, currCot + Dem] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }

            for (int Dem = 1; Dem < 6 && currCot - Dem >= 0 && currDong - Dem >= 0; Dem++)
            {
                if (matrix[currDong - Dem, currCot - Dem] == p1)
                {
                    SoQuanTa++;
                    break;
                }
                else if (matrix[currDong - Dem, currCot - Dem] == p2)
                {
                    SoQuanDich++;
                }
                else
                    break;
            }
            if (SoQuanTa == 2)
                return 0;
            DiemTong += MangDiemPhongNgu[SoQuanTa];
            return DiemTong;
        }
        #endregion  Phòng ngự

        #endregion Tính nước đi cho COM
    }
}
