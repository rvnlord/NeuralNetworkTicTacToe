using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using TipTacToe.Common;

namespace TipTacToe.Models
{
    public class NeuralNetwork
    {
        private static Random _rng;

        private readonly int _numInput;
        private readonly int _numHidden;
        private readonly int _numOutput;
        private readonly int _numWeights;

        private readonly double[] _input;

        private readonly double[][] _ihWeights; // wejściowa-ukryta
        private readonly double[] _hBiases;
        private readonly double[] _hOutputs;

        private readonly double[][] _hoWeights; // ukryta-wyjściowa
        private readonly double[] _oBiases;

        private double[] _output;
        
        public TrainSet DataSet { get; }
        public TrainSet TrainSet { get; set; }
        public TrainSet TestSet { get; set; }

        public NeuralNetwork(TrainSet dataSet, int numHidden)
        {
            _rng = new Random(0);
            DataSet = dataSet;
            TrainSet = dataSet;
            
            _numInput = dataSet[0].Inputs.Length;
            _numHidden = numHidden;
            _numOutput = dataSet[0].Outputs.Length;
            _numWeights = _numInput * _numHidden + _numHidden * _numOutput + _numHidden + _numOutput;

            _input = new double[_numInput];

            _ihWeights = MakeMatrix(_numInput, _numHidden);
            _hBiases = new double[_numHidden];
            _hOutputs = new double[_numHidden];

            _hoWeights = MakeMatrix(_numHidden, _numOutput);
            _oBiases = new double[_numOutput];

            _output = new double[_numOutput];
        }

        private static double[][] MakeMatrix(int rows, int cols) // tworzenie pustej macierzy (tablicy tablic))
        {
            var result = new double[rows][];
            for (var r = 0; r < result.Length; ++r)
                result[r] = new double[cols];
            return result;
        }
        
        public void SetWeights(double[] weights)
        {
            // kopiuj wagi i odchylenia z tablicy wag 'weights[]' do tablic: i-h weights, i-h biases, h-o weights, h-o biases
            if (weights.Length != _numWeights)
                throw new Exception("Niepoprawna długość tablicy wag");

            var k = 0; // wskazuje gdzie w tablicy wag się znajdujemy

            for (var i = 0; i < _numInput; ++i)
                for (var j = 0; j < _numHidden; ++j)
                    _ihWeights[i][j] = weights[k++];
            for (var i = 0; i < _numHidden; ++i)
                _hBiases[i] = weights[k++];
            for (var i = 0; i < _numHidden; ++i)
                for (var j = 0; j < _numOutput; ++j)
                    _hoWeights[i][j] = weights[k++];
            for (var i = 0; i < _numOutput; ++i)
                _oBiases[i] = weights[k++];
        }

        public void InitializeWeights()
        {
            // Inicjalizuj wagi i odchylenia niewielkimi lsoowymi wartościami
            var initialWeights = new double[_numWeights];
            const double lo = -0.01;
            const double hi = 0.01;
            for (var i = 0; i < initialWeights.Length; ++i)
                initialWeights[i] = (hi - lo) * _rng.NextDouble() + lo;
            SetWeights(initialWeights);
        }

        public double[] GetWeights()
        {
            // zwróć aktualny zestaw wag
            var result = new double[_numWeights];
            var k = 0;
            for (int i = 0; i < _ihWeights.Length; ++i)
                for (int j = 0; j < _ihWeights[0].Length; ++j)
                    result[k++] = _ihWeights[i][j];
            for (int i = 0; i < _hBiases.Length; ++i)
                result[k++] = _hBiases[i];
            for (int i = 0; i < _hoWeights.Length; ++i)
                for (int j = 0; j < _hoWeights[0].Length; ++j)
                    result[k++] = _hoWeights[i][j];
            for (int i = 0; i < _oBiases.Length; ++i)
                result[k++] = _oBiases[i];
            return result;
        }
        
        public double[] PredictOne(IReadOnlyList<double> xValues)
        {
            if (xValues.Count != _numInput)
                throw new Exception("Niepoprawna długośc tablicy 'xValues'");

            var hSums = new double[_numHidden]; // suma węzłów ukrytych
            var oSums = new double[_numOutput]; // suma węzłów wyjściowych

            for (var i = 0; i < xValues.Count; ++i) // kopiuj wartości x do tablicy wejść
                _input[i] = xValues[i];

            for (var j = 0; j < _numHidden; ++j) // oblicz sumę wag * wejść pomiędzy warstwą wejścia, a ukrytą
                for (var i = 0; i < _numInput; ++i)
                    hSums[j] += _input[i] * _ihWeights[i][j];

            for (var i = 0; i < _numHidden; ++i) // dodaj odchylenia do wyliczonych sum
                hSums[i] += _hBiases[i];

            for (var i = 0; i < _numHidden; ++i) // użyj funkcji aktywacji HyperTan
                _hOutputs[i] = HyperTg(hSums[i]);

            for (var j = 0; j < _numOutput; ++j) // oblicz sumę wag * wyjść pomiędzy warstwą ukrytą, a warstwą wyjścia
                for (var i = 0; i < _numHidden; ++i)
                    oSums[j] += _hOutputs[i] * _hoWeights[i][j];

            for (var i = 0; i < _numOutput; ++i) // dodaj odchylenia
                oSums[i] += _oBiases[i];

            var softOut = SoftMax(oSums); // użyj funkcji aktywacji SoftMax dla wszystkich wyjść jednocześnie przez wzgląd na efektywność

            _output = softOut.Copy();
            return _output.Copy();
        }

        private static double HyperTg(double x)
        {
            return x < -20.0
                ? -1.0
                : (x > 20.0
                    ? 1.0
                    : Math.Tanh(x));
        }

        private static double[] SoftMax(IReadOnlyList<double> oSums)
        {
            // określ największą sumę wyjść
            // oblicza wwzystkie wyjściowe węzły naraz więc skalowanie nie musi być wykonywane za każdym razem
            double max = oSums[0];
            for (int i = 0; i < oSums.Count; ++i)
                if (oSums[i] > max) max = oSums[i];

            // określ wektor skalujący - suma: exp(każda wartość - max)
            double scale = 0.0;
            for (int i = 0; i < oSums.Count; ++i)
                scale += Math.Exp(oSums[i] - max);

            double[] result = new double[oSums.Count];
            for (int i = 0; i < oSums.Count; ++i)
                result[i] = Math.Exp(oSums[i] - max) / scale;

            return result; // wynik jest przeskalowany więc xi sumują się do 1.0
        }
        
        private void UpdateWeights(IReadOnlyList<double> tValues, double learnRate, double momentum, double weightDecay)
        {
            var hGrads = new double[_numHidden]; // ukryte gradienty dla propagacji wstecznej
            var oGrads = new double[_numOutput]; // wyjściowe gradienty dla propagacji wstecznej

            var ihPrevWeightsDelta = MakeMatrix(_numInput, _numHidden);  // dla obliczania kroku w propagacji wstecznej
            var hPrevBiasesDelta = new double[_numHidden]; ;
            var hoPrevWeightsDelta = MakeMatrix(_numHidden, _numOutput);
            var oPrevBiasesDelta = new double[_numOutput];

            // aktualizuj wagi i odchylenia używając propagacji wstecznej, z wartościami wyjściowymi, eta (szybkością uczenia), alpha (momentum).
            // zakładając że 'SetWeights' i 'Predict' zostały wywołane więc wszystkie tablice i macierze zawierają wyliczone wartości inne niż 0.
            if (tValues.Count != _numOutput)
                throw new Exception("Długośc tablicy 'tValues' i liczby zmiennych wyjściowych w UpdateWeights jest różna");

            // 1. oblicz wyjściowe gradienty
            for (var i = 0; i < oGrads.Length; ++i)
            {
                // pochodna z softmax = (1 - y) * y (tak samo jak log-sigmoid), bo ity element output został wcześniej obliczony w funkcji Train
                var derivative = (1 - _output[i]) * _output[i];
                // kwadrat średniogo błędu zawiera pochodną (1-y)(y) 
                oGrads[i] = derivative * (tValues[i] - _output[i]);
            }

            // 2. oblicz ukryte gradienty
            for (var i = 0; i < hGrads.Length; ++i)
            {
                // pochodna z tanh = (1 - y) * (1 + y)
                var derivative = (1 - _hOutputs[i]) * (1 + _hOutputs[i]);
                var sum = 0.0;
                for (var j = 0; j < _numOutput; ++j) // każda różnica w warstwie ukrytej jest sumą termów z gradientów wyjściowych
                {
                    var x = oGrads[j] * _hoWeights[i][j];
                    sum += x;
                }
                hGrads[i] = derivative * sum;
            }

            // 3a. aktualizuj ukryte wagi (gradienty muszą być obliczane od prawej do lewej, a wagi w dowolnej kolejności)
            for (var i = 0; i < _ihWeights.Length; ++i) // 0..2 (3)
            {
                for (var j = 0; j < _ihWeights[0].Length; ++j) // 0..3 (4)
                {
                    var delta = learnRate * hGrads[j] * _input[i]; // oblicz nową różnicę
                    _ihWeights[i][j] += delta; // aktualizuj. używamy '+' zamiast'-'.
                    // dodaj moment (krok) używając poprzedniej różnicy. W pierwszym przejściu stara wartość wynosi 0, nie ma to znaczenia.
                    _ihWeights[i][j] += momentum * ihPrevWeightsDelta[i][j];
                    _ihWeights[i][j] -= weightDecay * _ihWeights[i][j]; // rozłożenie wagi
                    ihPrevWeightsDelta[i][j] = delta; // zapamiętaj różnicę dla kolejnego kroku
                }
            }

            // 3b. aktualizuj ukryte odchylenia
            for (var i = 0; i < _hBiases.Length; ++i)
            {
                var delta = learnRate * hGrads[i] * 1.0; // t1.0 jest stałą dla odchylenia, można pominąć
                _hBiases[i] += delta;
                _hBiases[i] += momentum * hPrevBiasesDelta[i]; // moment
                _hBiases[i] -= weightDecay * _hBiases[i]; // rozłożenie wagi
                hPrevBiasesDelta[i] = delta; // zapamiętaj różnicę
            }

            // 4. aktualizuj wagi pomiędzy warstwą ukrytą, a wyjściową
            for (var i = 0; i < _hoWeights.Length; ++i)
            {
                for (var j = 0; j < _hoWeights[0].Length; ++j)
                {
                    // jak powyżej: hOutputs to wejścia dla wyjść sieci neuronowej
                    var delta = learnRate * oGrads[j] * _hOutputs[i];
                    _hoWeights[i][j] += delta;
                    _hoWeights[i][j] += momentum * hoPrevWeightsDelta[i][j]; // moment
                    _hoWeights[i][j] -= (weightDecay * _hoWeights[i][j]); // rozłożenie wagi
                    hoPrevWeightsDelta[i][j] = delta; // zapamiętaj różnicę
                }
            }

            // 4b. aktualizuj odchylenia wyjść
            for (var i = 0; i < _oBiases.Length; ++i)
            {
                var delta = learnRate * oGrads[i] * 1.0;
                _oBiases[i] += delta;
                _oBiases[i] += momentum * oPrevBiasesDelta[i]; // moment
                _oBiases[i] -= (weightDecay * _oBiases[i]); // rozłożenie wagi
                oPrevBiasesDelta[i] = delta; // zapamiętaj różnicę
            }
        }
        
        public void Train(int maxEpochs, double learnRate, double momentum, double weightDecay)
        {
            InitializeWeights();
            // naucz klasyfikator metodą propagacji wstecznej przy zadanym tempie uczenia i momencie (wielkości kroku wyjścia z lokalnych minimów)
            // rozkład wagi zmniejsza jej wielkość wraz z upływem czasu chyba, że wartość ta jest systematycznie zwiększana
            var epoch = 0;
            var sequence = Enumerable.Range(0, TrainSet.Count).ToArray();

            while (epoch < maxEpochs)
            {
                var mse = MeanSquaredError();
                if (mse < 0.020) break; //0.001 // wyjdź z pętli przy zadanej wartości kwadratu średniego błędu

                Shuffle(sequence); // odwiedzaj wiersze w losowej kolejności
                for (var i = 0; i < TrainSet.Count; ++i)
                {
                    var idx = sequence[i];
                    var xValues = TrainSet[idx].Inputs.Copy();
                    var tValues = TrainSet[idx].Outputs.Copy();
                    PredictOne(xValues); // wczytaj wartości do 'xValues' i oblicz wyjścia (przechowuj wartości lokalnie)
                    UpdateWeights(tValues, learnRate, momentum, weightDecay); // znajdź lepsze wagi
                } // dla każdego uczonego wiersza
                ++epoch;
            }
        } 

        private static void Shuffle(IList<int> sequence)
        {
            for (var i = 0; i < sequence.Count; ++i)
            {
                var r = _rng.Next(i, sequence.Count);
                var tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        }

        private double MeanSquaredError() // metoda używana jako warunek wyjścia z pętli uczącej
        {
            // średni kwadrat błędu w uczonym wierszu
            var sumSquaredError = 0.0;

            // przejdź każdy przypadek - przykład (1 0 0 1 0 ... 0 1) (0 0 0 0 1)
            for (var i = 0; i < TrainSet.Count; ++i)
            {
                var xValues = TrainSet[i].Inputs.Copy();
                var tValues = TrainSet[i].Outputs.Copy();
                var yValues = PredictOne(xValues); // oblicz wyjście używając aktualnych wag
                for (var j = 0; j < _numOutput; ++j)
                {
                    var err = tValues[j] - yValues[j];
                    sumSquaredError += err * err;
                }
            }

            return sumSquaredError / TrainSet.Count;
        }
        
        public void PredictSet(TrainSet testData)
        {
            if (!testData.Any())
            {
                testData.Accuracy = 0;
                return;
            }
            // procent poprawnych wnyików
            var numCorrect = 0;
            var numWrong = 0;

            for (var i = 0; i < testData.Count; ++i)
            {
                var xValues = testData[i].Inputs.Copy(); // parsuj dane testowe do x-values i t-values
                var tValues = testData[i].Outputs.Copy();
                var yValues = PredictOne(xValues); // obliczone odpowiedzi Y
                var maxIndex = MaxIndex(yValues); // sprawdź która komórka w 'yValues' ma największą wartość
                testData[i].Predictions = yValues.Copy();

                if (tValues[maxIndex].Eq(1.0)) // przewidywanie jest poprawne jeżeli indeks tej komórki jest taki sam jak indeks przy komórce zawierającej wartość 1 w wartościach oczemiwanych
                    ++numCorrect;
                else
                    ++numWrong;
            }
            testData.Accuracy = (numCorrect * 1.0) / (numCorrect + numWrong);
        }

        public void PredictTrainSet() => PredictSet(TrainSet);
        public void PredictTestSet() => PredictSet(TestSet);

        private static int MaxIndex(IReadOnlyList<double> vector) // funkcja pomocnicza do obliczania dokładności
        {
            // indeks największej wartości
            var bigIndex = 0;
            var biggestVal = vector[0];
            for (var i = 0; i < vector.Count; ++i)
            {
                if (vector[i] > biggestVal)
                {
                    biggestVal = vector[i];
                    bigIndex = i;
                }
            }
            return bigIndex;
        }

        public void Divide(int trainSetPerc, int testSetPerc)
        {
            if (trainSetPerc + testSetPerc != 100)
                throw new Exception("Nie można podzielić zbioru na podstawie podanych wartości procentowych");

            var trainSetNum = Math.Round((double) trainSetPerc / 100 * DataSet.Count).ToInt();
            TrainSet = DataSet.RandomSubset(trainSetNum, _rng).ToTrainSet();
            TestSet = DataSet.Except(TrainSet).ToTrainSet();
        }

        public static int[] EncodeClassAsBinary(S s)
        {
            const int fromBase = 10;
            const int toBase = 2;
            return Convert.ToString(Convert.ToInt32(((int)s).ToString(), fromBase), toBase).AddTrailingZeroes(2)
                .Select(x => x.ToString().ToInt()).ToArray();
        }

        public static S DecodeClassFromBinary(double[] sArr) // liczby powinny być całkowite
        {
            const int fromBase = 2;
            const int toBase = 10;
            var stArr = sArr.ToDelimitedString("");
            return Convert.ToString(Convert.ToInt32(stArr, fromBase), toBase).ToInt().ToEnum<S>();
        }

        public static int[] EncodeGameResultAsArray(GameResult en)
        {
            var vals = Utilities.GetValues<GameResult>().Cast<int>();
            return vals.Select(v => v == (int) en ? 1 : 0).ToArray();
        }

        public static GameResult DecodeGameResultFromArray(double[] arr)
        {
            return arr.Select((v, i) => new { i, v }).MaxBy(x => x.v).i.ToEnum<GameResult>();
        }

        public static int[] EncodeSAsArray(S en)
        {
            var vals = Utilities.GetValues<S>().Cast<int>();
            return vals.Select(v => v == (int)en ? 1 : 0).ToArray();
        }

        public static S DecodeSFromArray(double[] arr)
        {
            return arr.Select((v, i) => new { i, v }).MaxBy(x => x.v).i.ToEnum<S>();
        }

        public static SLikelihood DecodeSFromArrayWithLikelihood(double[] arr)
        {
            return arr.Select((v, i) => new SLikelihood(i.ToEnum<S>(), v)).MaxBy(x => x.L);
        }

        public static SLikelihood DecodeSFromArrayWithLikelihoodFor(double[] arr, S s)
        {
            return arr.Select((v, i) => new SLikelihood(i.ToEnum<S>(), v)).Single(l => l.S == s);
        }
    }

    public class SLikelihood
    {
        public S S { get; set; }
        public double L { get; set; }

        public SLikelihood(S s, double l)
        {
            S = s;
            L = l;
        }
    }
}
