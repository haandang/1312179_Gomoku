using System;



namespace Gomoku
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

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
