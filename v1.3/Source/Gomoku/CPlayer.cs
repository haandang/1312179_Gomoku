using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
namespace Gomoku
{
    enum EPlayerFlag
    {
        NONE = 0,
        Player1 = 1,
        Player2 = 2,
        COM = 3,
        Server = 4,
    }
    class CPlayer
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public bool State { get; set; }
        public Brush ColorPlayer { get; set; }
        public int PlayerFlag { get; set; }

        public CPlayer()
        {
            PlayerFlag = (int)EPlayerFlag.NONE;
            Row = Column = -1;
            ColorPlayer = Brushes.Black;
            State = false;
        }
        public CPlayer(int _row, int _column)
        {
            Row = _row;
            Column = _column;
            State = false;
        }
        public CPlayer(Brush _color, int _playerflag)
        {
            ColorPlayer = _color;
            PlayerFlag = _playerflag;
        }
        public CPlayer(int _row, int _column, bool _state)
        {
            Row = _row;
            Column = _column;
            State = _state;
        }
        public CPlayer(int _row, int _column, bool _state, Brush _color)
        {
            Row = _row;
            Column = _column;
            State = _state;
            ColorPlayer = _color;
        }
        public CPlayer(int _row, int _column, bool _state, Brush _color, int _playerflag)
        {
            Row = _row;
            Column = _column;
            State = _state;
            ColorPlayer = _color;
            PlayerFlag = _playerflag;
        }
        public CPlayer(CPlayer _player)
        {
            Row = _player.Row;
            Column = _player.Column;
            State = _player.State;
            ColorPlayer = _player.ColorPlayer;
            PlayerFlag = _player.PlayerFlag;
        }
    }
}
