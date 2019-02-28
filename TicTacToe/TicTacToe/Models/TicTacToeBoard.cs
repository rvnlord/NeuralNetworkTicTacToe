using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MoreLinq;
using TipTacToe.Common;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace TipTacToe.Models
{
    [Serializable]
    public class TicTacToeBoard
    {
        private TicTacToeGame _gameState;
        private double _gridStrokeSize = 2;
        private readonly List<GameSymbol> _circlesAndCrosses = new List<GameSymbol>();
        private S _prevSymbol = S.X;
        private static readonly Random _rng = new Random();

        public Canvas BaseCanvas { get; set; }

        public TicTacToeGame GameState
        {
            get => _gameState;
            set
            {
                _gameState = value;
                _circlesAndCrosses.Clear();
                var canvasSize = BaseCanvas.Width;
                var divBy3 = canvasSize / 3;
                BaseCanvas.Children.RemoveAll<FrameworkElement>(x => x.Name != "gridLine");
                for (var i = 0; i < _gameState.State.Length; i++)
                {
                    for (var j = 0; j < _gameState.State[i].Length; j++)
                    {
                        var x = divBy3 * (j + 1) - divBy3 / 2;
                        var y = divBy3 * (i + 1) - divBy3 / 2;
                        var size = divBy3 - 20;
                        if (_gameState.State[i][j] == S.X)
                            Add(new Cross(x, y, size, BaseCanvas, i, j));
                        else if (_gameState.State[i][j] == S.O)
                            Add(new Circle(x, y, size, BaseCanvas, i, j));
                        else
                            Add(new Empty(x, y, size, BaseCanvas, i, j));
                    }
                }
            }
        }

        public void Create(Canvas cv)
        {
            BaseCanvas = cv;
            GameState = new TicTacToeGame();
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            var size = BaseCanvas.Width.ToInt();
            var divBy3 = size / 3;
            AddGridLine(divBy3, 0, divBy3, size);
            AddGridLine(divBy3 * 2, 0, divBy3 * 2, size);
            AddGridLine(0, divBy3, size, divBy3);
            AddGridLine(0, divBy3 * 2, size, divBy3 * 2);
        }

        private void AddGridLine(int x1, int y1, int x2, int y2)
        {
            var currentLine = new Line
            {
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = _gridStrokeSize,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Name = "gridLine",
            };
            currentLine.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            Panel.SetZIndex(currentLine, 11);
            BaseCanvas.Children.Add(currentLine);
        }

        public void Add(GameSymbol symbol)
        {
            _circlesAndCrosses.Add(symbol);
        }

        public void Remove(GameSymbol symbol)
        {
            symbol.Delete();
            _circlesAndCrosses.Remove(symbol);
        }

        public void PlaceShadow(Point pos)
        {
            var cF = _circlesAndCrosses.MinBy(f => f.Pos.Distance(pos));
            if (cF is Empty empty && !empty.HasShadow() && _gameState.Result() == GameResult.Unfinished)
            {
                var fewestSymbol = FewestSymbol(false);
                var shadowType = fewestSymbol == S.O ? ShadowType.CircleShadow : ShadowType.CrossShadow;
                ClearShadow();
                empty.AddShadow(shadowType);
            }
            else if (!(cF is Empty))
            {
                ClearShadow();
            }
        }

        public void PlaceShadow(ShadowType st, int i, int j)
        {
            var empty = _circlesAndCrosses.OfType<Empty>().Single(s => s.I == i && s.J == j);
            ClearShadow();
            empty.AddShadow(st);
        }

        public void PlaceNNShadow(S symbol, NeuralNetwork nn)
        {
            if (_gameState.Result() != GameResult.Unfinished)
                return;

            var changedState = _gameState.State.DeepClone();
            var fState = changedState.SelectMany(y => y).Select(NeuralNetwork.EncodeClassAsBinary).SelectMany(z => z).Select(v => v.ToDouble()).ToArray();
            var prediction = nn.PredictOne(fState);
            var decodedState = prediction.SplitInParts(9).Select(x =>
                x.SplitInParts(3).Select(y => NeuralNetwork.DecodeSFromArrayWithLikelihoodFor(y.ToArray(), symbol)).ToArray()).ToArray();
            var fDecodedState = decodedState.Select((x, i) => x.Select((y, j) => new SLikelihoodWithIndexes(y.S, y.L, i, j))).SelectMany(z => z).ToArray();
            var fDecodedStatesOfMatchingSymbol = fDecodedState.Where(x => x.S == symbol).OrderByDescending(x => x.L).ToArray();
            foreach (var bestMatch in fDecodedStatesOfMatchingSymbol)
            {
                if (changedState[bestMatch.I][bestMatch.J] != S._)
                    continue;
                
                ClearShadow();
                var shadowType = symbol == S.O ? ShadowType.CircleShadow : ShadowType.CrossShadow;
                if (_circlesAndCrosses.Single(s => s.I == bestMatch.I && s.J == bestMatch.J) is Empty empty && !empty.HasShadow() && _gameState.Result() == GameResult.Unfinished)
                {
                    empty.AddShadow(shadowType);
                }
                
                return;
            }
            throw new Exception("Wszystkie pola wskazane przez sieć są zajęte");
        }

        public GameSymbol GetShadow()
        {
            return _circlesAndCrosses.OfType<Empty>().Single(x => x.HasShadow()).Shadow;
        }

        public void EditField(Point pos)
        {
            var cF = _circlesAndCrosses.MinBy(f => f.Pos.Distance(pos));
            var editedState = _gameState.State.DeepClone();
            var l = Enum.GetValues(typeof(S)).Length;
            var es = (int)editedState[cF.I][cF.J];
            editedState[cF.I][cF.J] = (S) ((es + 1) % l);
            GameState = new TicTacToeGame(0, editedState);
        }

        public S MakeMove(Point pos)
        {
            var fewestSymbol = FewestSymbol(true);
            var cF = _circlesAndCrosses.MinBy(f => f.Pos.Distance(pos));
            if (cF is Empty && _gameState.Result() == GameResult.Unfinished)
            {
                var changedState = _gameState.State.DeepClone();
                changedState[cF.I][cF.J] = fewestSymbol;
                GameState = new TicTacToeGame(0, changedState);
                ClearShadow();
                _prevSymbol = fewestSymbol;
            }
            return fewestSymbol;
        }

        public bool IsFieldEmpty(Point pos)
        {
            return _circlesAndCrosses.MinBy(f => f.Pos.Distance(pos)) is Empty;
        }

        public void MakeMove(int i, int j)
        {
            var changedState = _gameState.State.DeepClone();
            if (changedState[i][j] != S._)
                throw new Exception("Nie można zrobić ruchu na pole, które zawiera już symbol");
            changedState[i][j] = FewestSymbol(true);
            GameState = new TicTacToeGame(0, changedState);
            ClearShadow();
            _prevSymbol = changedState[i][j];
        }

        public void MakeMove(S symbol, int i, int j)
        {
            var changedState = _gameState.State.DeepClone();
            if (changedState[i][j] != S._)
                throw new Exception("Nie można zrobić ruchu na pole, które zawiera już symbol");
            changedState[i][j] = symbol;
            GameState = new TicTacToeGame(0, changedState);
            ClearShadow();
            _prevSymbol = symbol;
        }

        public S MakeRandomMove()
        {
            var fewestSymbol = FewestSymbol(true);
            if (_gameState.Result() != GameResult.Unfinished)
                return fewestSymbol;

            var rField = _circlesAndCrosses.Where(f => f is Empty).RandomSubset(1, _rng).Single();
           
            var changedState = _gameState.State.DeepClone();
            changedState[rField.I][rField.J] = fewestSymbol;
            GameState = new TicTacToeGame(0, changedState);
            ClearShadow();
            _prevSymbol = fewestSymbol;
            return fewestSymbol;
        }

        public S MakeRandomMove(S symbol)
        {
            if (_gameState.Result() != GameResult.Unfinished)
                return symbol;

            var rField = _circlesAndCrosses.Where(f => f is Empty).RandomSubset(1, _rng).Single();
            var changedState = _gameState.State.DeepClone();
            changedState[rField.I][rField.J] = symbol;
            GameState = new TicTacToeGame(0, changedState);
            ClearShadow();
            _prevSymbol = symbol;
            return symbol;
        }

        public void MakeNNMove(S symbol, NeuralNetwork nn)
        {
            if (_gameState.Result() != GameResult.Unfinished)
                return;

            var changedState = _gameState.State.DeepClone();
            var fState = changedState.SelectMany(y => y).Select(NeuralNetwork.EncodeClassAsBinary).SelectMany(z => z).Select(v => v.ToDouble()).ToArray();
            var prediction = nn.PredictOne(fState);
            var decodedState = prediction.SplitInParts(9).Select(x =>
                x.SplitInParts(3).Select(y => NeuralNetwork.DecodeSFromArrayWithLikelihoodFor(y.ToArray(), symbol)).ToArray()).ToArray();
            var fDecodedState = decodedState.Select((x, i) => x.Select((y, j) => new SLikelihoodWithIndexes(y.S, y.L, i, j))).SelectMany(z => z).ToArray();
            var fDecodedStatesOfMatchingSymbol = fDecodedState.Where(x => x.S == symbol).OrderByDescending(x => x.L).ToArray();
            foreach (var bestMatch in fDecodedStatesOfMatchingSymbol)
            {
                if (changedState[bestMatch.I][bestMatch.J] != S._)
                    continue;
                changedState[bestMatch.I][bestMatch.J] = symbol;
                GameState = new TicTacToeGame(0, changedState);
                ClearShadow();
                _prevSymbol = symbol;
                return;
            }
            throw new Exception("Wszystkie pola wskazane przez sieć są zajęte");
        }

        public void ClearShadow()
        {
            BaseCanvas.Children.RemoveAll<FrameworkElement>(x => x.Name == "shadowPart");
            _circlesAndCrosses.OfType<Empty>().ForEach(s => s.RemoveShadow());
        }

        private S FewestSymbol(bool changePreviousSymbol)
        {
            var groupedSymbols = _gameState.State.SelectMany(x => x).Where(x => x != S._).GroupBy(x => x)
                .Select(x => new { Sym = x.Key, Count = x.Count() }).ToList();
            foreach (var v in Utilities.GetValues<S>().Where(x => x != S._).ToArray())
                if (groupedSymbols.All(x => x.Sym != v))
                    groupedSymbols.Add(new { Sym = v, Count = 0 });
            var minSym = groupedSymbols.MinBy(x => x.Count);
            if (groupedSymbols.Where(s => s.Sym != minSym.Sym).Any(s => s.Count == minSym.Count))
            {
                var tmp = TicTacToeGame.Opposite(_prevSymbol);
                _prevSymbol = changePreviousSymbol ? TicTacToeGame.Opposite(_prevSymbol) : _prevSymbol;
                return tmp;
            }
            _prevSymbol = minSym.Sym;
            return minSym.Sym;
        }

        public void NextSymbol()
        {
            _prevSymbol = TicTacToeGame.Opposite(_prevSymbol);
        }

        public S PrevSymbol => _prevSymbol;
    }

    public class SLikelihoodWithIndexes : SLikelihood
    {
        public int I { get; }
        public int J { get; }

        public SLikelihoodWithIndexes(S s, double l, int i, int j) : base(s, l)
        {
            I = i;
            J = j;
        }
    }

    public abstract class GameSymbol
    {
        public int I { get; }
        public int J { get; }
        public Point Pos { get; protected set; }

        protected GameSymbol(double x, double y, double size, Canvas canvas, int i, int j)
        {
            I = i;
            J = j;
        }

        public abstract void Delete();
    }

    public class Circle : GameSymbol
    {
        protected readonly Path _circle;
        private readonly Canvas _canvas;
        
        public Circle(double x, double y, double size, Canvas canvas, int i, int j) : base(x, y, size, canvas, i, j)
        {
            _canvas = canvas;
            var radius = size / 2;
            var circle = new Path
            {
                Fill = Brushes.Transparent,
                Stroke = Brushes.YellowGreen,
                StrokeThickness = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Data = new EllipseGeometry(new Point(x, y), radius, radius),
                SnapsToDevicePixels = true
            };
            circle.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            Panel.SetZIndex(circle, 11);
            canvas.Children.Add(circle);
            _circle = circle;
            Pos = new Point(x, y);
        }

        public override void Delete()
        {
            _canvas.Children.Remove(_circle);
        }
    }

    public class Cross : GameSymbol
    {
        protected readonly Line[] _lines = new Line[2];
        private readonly Canvas _canvas;
        
        public Cross(double x, double y, double size, Canvas canvas, int i, int j) : base(x, y, size, canvas, i, j)
        {
            _canvas = canvas;
            var radius = size / 2;
            _lines[0] = DrawLine(x - radius, y - radius, x + radius, y + radius);
            _lines[1] = DrawLine(x + radius, y - radius, x - radius, y + radius);
            Pos = new Point(x, y);
        }

        private Line DrawLine(double x1, double y1, double x2, double y2)
        {
            var line = new Line
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 5,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            Panel.SetZIndex(line, 11);
            _canvas.Children.Add(line);
            return line;
        }

        public override void Delete()
        {
            _canvas.Children.RemoveAll<FrameworkElement>(c => _lines.Contains(c));
        }
    }

    public class Empty : GameSymbol
    {
        private readonly Path _dummy;
        private readonly double _size;
        private readonly Canvas _canvas;

        public GameSymbol Shadow { get; private set; }

        public Empty(double x, double y, double size, Canvas canvas, int i, int j) : base(x, y, size, canvas, i, j)
        {
            _size = size;
            _canvas = canvas;
            var radius = size / 2;
            var circle = new Path
            {
                Fill = Brushes.Transparent,
                Stroke = Brushes.Transparent,
                StrokeThickness = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Data = new EllipseGeometry(new Point(x, y), radius, radius),
                SnapsToDevicePixels = true
            };
            circle.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            Panel.SetZIndex(circle, 11);
            canvas.Children.Add(circle);
            _dummy = circle;
            Pos = new Point(x, y);
        }

        public override void Delete()
        {
            _canvas.Children.Remove(_dummy);
        }

        public bool HasShadow()
        {
            return Shadow != null;
        }

        public void AddShadow(ShadowType type)
        {
            if (type == ShadowType.CircleShadow)
                Shadow = new CircleShadow(Pos.X, Pos.Y, _size, _canvas, I, J);
            else if (type == ShadowType.CrossShadow)
                Shadow = new CrossShadow(Pos.X, Pos.Y, _size, _canvas, I, J);
        }

        public void RemoveShadow()
        {
            Shadow = null;
        }
    }

    public class CircleShadow : Circle
    {
        public CircleShadow(double x, double y, double size, Canvas canvas, int i, int j) : base(x, y, size, canvas, i, j)
        {
            _circle.Opacity = 0.3;
            _circle.Name = "shadowPart";
        }
    }

    public class CrossShadow : Cross
    {
        public CrossShadow(double x, double y, double size, Canvas canvas, int i, int j) : base(x, y, size, canvas, i, j)
        {
            _lines.ForEach(l =>
            {
                l.Opacity = 0.3;
                l.Name = "shadowPart";
            });
        }
    }

    public enum ShadowType
    {
        CircleShadow,
        CrossShadow
    }
}
