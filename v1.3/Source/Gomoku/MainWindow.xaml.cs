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
        //
        CPlayer player_user, player_pc, player_server;
        int testshot = 2;
        int testshot_online = 1;

        //Bàn cờ (chứa các ô nhỏ)
        Rectangle rec;

        //Số lượng ô vuông trên 1 hàng hoặc cột
        int cell_quantity = 12;
        //Kích thước 1 ô
        int cell_height = 30, cell_width = 30;
        /// ten nguoi choi
        string user;

        Brush color1;   //color cua nguoi choi 1
        Brush color2;   //color cua nguoi choi 2, COM va server

        //Cờ đánh dầu người chơi
        //true:      1vs2
        //false:  1vsCOM
        public bool flag_player = true;
        
        //Ma trận:
        public int[,] matrix;

        //
        Socket socket;


        #endregion

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
            //flag_game = true;       //1vs2
            //string mes = "Server: 1 vs 2\nPlayer 1: Red - Player 2: Blue.";
            //mes = mes + getTime();
            //lvw_chat.Items.Add(mes);
            
            color1 = Brushes.Red;
            color2 = Brushes.Blue;

            //flag_online = true;

            //chế độ chơi mặc đinh
            player_user = new CPlayer(color1, (int)EPlayerFlag.Player1);
            player_user.State = true;
            player_pc = new CPlayer(color2, (int)EPlayerFlag.Player2);
            player_server = new CPlayer(color2, (int)EPlayerFlag.Server);
            newGame();

        }
        #region Các hàm xử lý sự kiện trên form
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
                if (testPoint(row, col))    //Kiểm tra ví trí chọn có hợp lệ
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
                                if (testshot_online != 1)
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
                                    lvw_chat.Items.Add("Waiting plays");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void btn_sendmes_Click(object sender, RoutedEventArgs e)
        {
            if (player_server.State == true)
            {
                socket.Emit("ChatMessage", tbx_mes.Text);
            }
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
            drawBoard();
        }

        private void wdw_gomoku_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            resizeBoard();
            cvs_gomoku.Children.Clear();
            drawBoard();
        }

        private void btn_name_Click(object sender, RoutedEventArgs e)
        {
            if (player_server.State == true)
            {
                socket.Emit("MyNameIs", tbx_name.Text);
                socket.Emit("ConnectToOtherPlayer");
            }
            if (user != tbx_name.Text)
            {
                string mes;
                mes = "Server: " + user;
                user = tbx_name.Text;
                mes = mes + " is now called " + user + getTime();
                lvw_chat.Items.Add(mes);
            }
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
                mes += "Sever: chế độ chơi offline. \n";
                if (player_pc.State == false)
                {
                    mes += "1 vs 2 \n";
                    mes += "First turn: ";
                    mes += (testshot == 1) ? "Player 2" : "Player 1";
                    mes = mes + getTime();
                    lvw_chat.Items.Add(mes);
                }
                else
                {
                    mes += "1 vs COM \n";
                    mes += "Turn first: ";
                    mes += (testshot == 1) ? "COM" : "Player 1";
                    mes = mes + getTime();
                    lvw_chat.Items.Add(mes);
                }
            }
            else
            {
                mes += "Sever: chế độ chơi online \n";
                lvw_chat.Items.Add(mes);
            }
        }

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

        private void newgame_Click(object sender, RoutedEventArgs e)
        {
            newGame();
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void about_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gomoku", "ABOUT GOMOKU");
        }

        private void mouseleave_Item(object sender, MouseEventArgs e)
        {
            MenuItem mitem = (MenuItem)sender;
            mitem.FontWeight = FontWeights.Normal;
        }

        private void mousemove_Item(object sender, MouseEventArgs e)
        {
            MenuItem mitem = (MenuItem)sender;
            mitem.FontWeight = FontWeights.Bold;
        }

        private void mode_1vs2_Click(object sender, RoutedEventArgs e)
        {
            flag_player = true;
            player_user.State = true;
            player_pc.State = false;
            newGame();
        }

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

        private void mode_online_Click(object sender, RoutedEventArgs e)
        {
            player_server.State = true;
            newGame();
            connectServer();
        }

        private bool testPoint(int row, int col)
        {
            return (matrix[col, row] == 0) ? true : false;
        }

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
        
        //Hàm vẽ cờ
        private bool drawGomoku(CPlayer pObject)
        {
            //Xét ô đã được đánh
            testPoint(pObject.Row, pObject.Column);

            Ellipse cell_elip = createElip(cell_width - 4, cell_height - 4, pObject.Column, pObject.Row, pObject.ColorPlayer);
            matrix[pObject.Column, pObject.Row] = pObject.PlayerFlag;
            cvs_gomoku.Children.Add(cell_elip);
            return true;
        }
        #endregion

        #region Socket
        private void connectServer()
        {
            socket = IO.Socket("ws://gomoku-lajosveres.rhcloud.com:8000");

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
                    //  String s = ((Newtonsoft.Json.Linq.JObject)data)["message"].ToString();
                    lvw_chat.Items.Add("connect Error");
                }));
            });


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
                        s = strimMess(s);
                        lvw_chat.Items.Add(s + "");
                        //socket.Emit("ChatMessage", "Start");
                    }));
                }

            });
            socket.On(Socket.EVENT_ERROR, (data) =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lvw_chat.Items.Add(data + "event error");
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
                        if (testPoint(row, col))
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
                                String s = "Your turn!";
                                lvw_chat.Items.Add(s);
                            }
                            if (player_pc.State == true)
                            {
                                lock (synch_pc_ol)
                                {
                                    player_user = findWayforPC(3, 1);
                                    player_user.State = true;
                                    drawGomoku(player_user);
                                    socket.Emit("MyStepIs", JObject.FromObject(new { row = player_user.Row, col = player_user.Column }));
                                    if (checkWinner(player_user))
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
        private String strimMess(string s)
        {
            for (int i = 34; i < s.Length - 6; i++)
            {
                if (s[i] == '<' && s[i + 1] == 'b' && s[i + 2] == 'r' && s[i + 4] == '/' && s[i + 5] == '>')
                {

                    string s1 = s.Substring(40);
                    if (s1.Equals("You are the first player!"))
                    {
                        testshot_online = 3;
                        if (player_pc.State == true)
                        {
                            Random rd = new Random();
                            player_pc = new CPlayer(rd.Next(0, 11), rd.Next(0, 11), true, color2, (int)EPlayerFlag.Player2);
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

        private void Event_Socket()
        {


            //socket.Connect();
        }
        #endregion

        private long[] MangDiemTanCong = new long[7] { 0, 9, 54, 162, 1458, 13112, 118008 };
        private long[] MangDiemPhongNgu = new long[7] { 0, 3, 27, 99, 729, 6561, 59049 };
        // p1: đối thủ
        //p2: 
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
        #endregion
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
        #endregion
    }
}
