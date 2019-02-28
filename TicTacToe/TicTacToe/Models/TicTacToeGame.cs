using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using MoreLinq;
using TipTacToe.Common;

namespace TipTacToe.Models
{
    [Serializable]
    public class TicTacToeGame
    {
        private readonly S[][][] _winPatterns = {
            new []
            {
                new []{ S.X, S.X, S.X },
                new []{ S._, S._, S._ },
                new []{ S._, S._, S._ }
            },
            new []
            {
                new []{ S._, S._, S._ },
                new []{ S.X, S.X, S.X },
                new []{ S._, S._, S._ }
            },
            new []
            {
                new []{ S._, S._, S._ },
                new []{ S._, S._, S._ },
                new []{ S.X, S.X, S.X }
            },
            new []
            {
                new []{ S.X, S._, S._ },
                new []{ S.X, S._, S._ },
                new []{ S.X, S._, S._ }
            },
            new []
            {
                new []{ S._, S.X, S._ },
                new []{ S._, S.X, S._ },
                new []{ S._, S.X, S._ }
            },
            new []
            {
                new []{ S._, S._, S.X },
                new []{ S._, S._, S.X },
                new []{ S._, S._, S.X }
            },
            new []
            {
                new []{ S.X, S._, S._ },
                new []{ S._, S.X, S._ },
                new []{ S._, S._, S.X }
            },
            new []
            {
                new []{ S._, S._, S.X },
                new []{ S._, S.X, S._ },
                new []{ S.X, S._, S._ }
            }
        };
        public int Id { get; set; }
        public S[][] State { get; set; }
        public string R1C1 => State[0][0].ConvertToString();
        public string R1C2 => State[0][1].ConvertToString();
        public string R1C3 => State[0][2].ConvertToString();
        public string R2C1 => State[1][0].ConvertToString();
        public string R2C2 => State[1][1].ConvertToString();
        public string R2C3 => State[1][2].ConvertToString();
        public string R3C1 => State[2][0].ConvertToString();
        public string R3C2 => State[2][1].ConvertToString();
        public string R3C3 => State[2][2].ConvertToString();
        public GameResult? NNResult { get; set; }
        public string NNResultString => NNResult == null ? "" : GameResultToString((GameResult) NNResult);

        public string ResultString => GameResultToString(Result());

        public TicTacToeGame(int id, S[][] state)
        {
            Id = id;
            State = state;
        }

        public TicTacToeGame(S[][] state)
        {
            Id = 0;
            State = state;
        }

        public TicTacToeGame()
        {
            State = new []
            {
                new []{ S._, S._, S._ },
                new []{ S._, S._, S._ },
                new []{ S._, S._, S._ }
            };
        }

        public static TicTacToeGame Empty()
        {
            return new TicTacToeGame();
        }

        public GameResult Result()
        {
            var xWonPattern = ContainsWinningPattern();
            var oWonPattern = Opposite().ContainsWinningPattern();
            return xWonPattern && oWonPattern 
                ? GameResult.Invalid
                : (xWonPattern
                    ? GameResult.XWon
                    : (oWonPattern
                        ? GameResult.OWon 
                        : (State.SelectMany(x => x).Contains(S._)
                            ? GameResult.Unfinished
                            : GameResult.Draw)));
        }

        private string GameResultToString(GameResult gr)
        {
            switch (gr)
            {
                case GameResult.XWon:
                    return "Lose (X)";
                case GameResult.OWon:
                    return "Win (O)";
                case GameResult.Draw:
                    return "Draw";
                case GameResult.Unfinished:
                    return "Unfinished";
                case GameResult.Invalid:
                    return "Incorrect";
                default:
                    return "Error";
            }
        }

        private bool ContainsWinningPattern()
        {
            foreach (var wp in _winPatterns)
            {
                var wpFlatArr = wp.SelectMany(x => x).ToArray();
                var idxVals = wpFlatArr.Select((x, i) => new { V = x, I = i });
                var indices = idxVals.Where(x => x.V == S.X).Select(x => x.I).ToArray();
                var flatArr = State.SelectMany(x => x).ToArray();
                if (flatArr.Select((x, i) => new { V = x, I = i }).Where(x => x.I.EqualsAny(indices)).All(x => x.V == S.X))
                    return true;
            }
            return false;
        }

        public TicTacToeGame Opposite()
        {
            var clone = this.DeepClone();
            clone.State = clone.State.Select(x => x.Select(Opposite).ToArray()).ToArray();
            return clone;
        }

        public static S Opposite(S s)
        {
            if (s == S._)
                return S._;
            return s == S.O ? S.X : S.O;
        }

        public override string ToString()
        {
            return State.SelectMany(x => x).ToDelimitedString(",");
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TicTacToeGame))
                return false;
            var state2 = (TicTacToeGame) obj;

            return ToString().Equals(state2.ToString());
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (State != null ? State.GetHashCode() : 0);
            }
        }
    }

    public enum S
    {
        X,
        O,
        _
    }

    public enum GameResult
    {
        XWon,
        OWon,
        Draw,
        Unfinished,
        Invalid
    }
}
