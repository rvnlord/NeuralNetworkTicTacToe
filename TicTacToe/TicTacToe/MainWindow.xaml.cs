using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Text;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MoreLinq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TipTacToe.Common;
using TipTacToe.Common.UtilityClasses;
using TipTacToe.Models;
using Button = System.Windows.Controls.Button;
using Tile = MahApps.Metro.Controls.Tile;
using WPFColor = System.Windows.Media.Color;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using Path = System.IO.Path;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using Style = System.Windows.Style;
using DataGridCell = System.Windows.Controls.DataGridCell;

namespace TipTacToe
{
    public partial class MainWindow
    {
        #region Constants

        #endregion

        #region Fields

        private NotifyIcon _notifyIcon;
        private List<Button> _buttons = new List<Button>();
        private TicTacToeBoard _gameBoard;
        private bool _deletingRows;
        private WPFColor _mouseOverMainMenuTileColor;
        private WPFColor _defaultMainMenuTileColor;
        private static readonly Random _rng = new Random();

        private NeuralNetwork _nnHowToPlayX;

        #endregion

        #region Properties

        public static string AppDirPath { get; set; }
        public static string ErrorLogPath { get; set; }

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        #endregion

        #region Events

        #region - MainWindow Events

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DisableControls(_buttons);
            ShowLoader(gridDataContainer);
            try
            {
                await Task.Run(() =>
                {
                    AppDirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    ErrorLogPath = $@"{AppDirPath}\ErrorLog.log";

                    SetupNotifyIcon();

                    Dispatcher.Invoke(() =>
                    {
                        SetupButtons();
                        SetupTextBoxes();
                        SetupUpDowns();
                        SetupDropdowns();
                        SetupGridviews();
                        SetupGameBoard();
                        SetupTiles();
                        InitializeControlGroups();
                    });
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Error occured", ex.Message);
            }
            finally
            {
                HideLoader(gridDataContainer);
                EnableControls(_buttons.Except(new[] { btnSave, btnDelete }));
            }
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                //
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Error occured", ex.Message);
            }
        }

        #endregion

        #region - Button Events

        private void btnMinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(1500);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var selectedGameState = gvData.SelectedItems.Cast<TicTacToeGame>().Single();
            var editedGame = new TicTacToeGame(selectedGameState.Id, _gameBoard.GameState.State.DeepClone()); // zmieniony podczas edycji lub gry
            var games = gvData.Items.Cast<TicTacToeGame>().ToList();
            games.Remove(selectedGameState);
            games.Add(editedGame);
            gvData.RefreshWith(games.OrderBy(x => x.Id).ToList());
            gvData.SelectedItems.Clear();
            gvData.SelectedItems.Add(editedGame);
            gvData.ScrollIntoView(editedGame);
            gvData.Focus();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var games = gvData.Items.Cast<TicTacToeGame>().ToList();
            var maxID = games.Any() ? games.Select(g => g.Id).Max() + 1 : 0;
            var newGame = new TicTacToeGame(maxID, _gameBoard.GameState.State.DeepClone());
            games.Add(newGame);
            gvData.RefreshWith(games);
            gvData.SelectedItems.Clear();
            gvData.SelectedItems.Add(newGame);
            gvData.ScrollIntoView(newGame);
            gvData.Focus();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRows();
        }

        private void btnNewGame_Click(object sender, RoutedEventArgs e)
        {
            gvData.SelectedItems.Clear();
            lblGameStatus.Content = _gameBoard.GameState.ResultString;
            _gameBoard.GameState = TicTacToeGame.Empty();
            _gameBoard.NextSymbol();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _deletingRows = true;
            gvData.RefreshWith(new List<TicTacToeGame>());
            btnSave.IsEnabled = false;
            btnDelete.IsEnabled = false;
            _gameBoard.GameState = TicTacToeGame.Empty();
            _gameBoard.NextSymbol();
            lblGameStatus.Content = "";
            lblInfo.SetWrappedText($"All game states have been removed");
            _deletingRows = false;
        }

        private void btnPreserveUnique_Click(object sender, RoutedEventArgs e)
        {
            _deletingRows = true;
            var games = gvData.Items.Cast<TicTacToeGame>().ToList();
            var distinct = games.DistinctBy(x => x.ToString()).ToList();
            gvData.RefreshWith(distinct);
            btnSave.IsEnabled = false;
            btnDelete.IsEnabled = false;
            _gameBoard.GameState = TicTacToeGame.Empty();
            lblGameStatus.Content = "";
            lblInfo.SetWrappedText($"Kept {distinct.Count} of {games.Count} ({(games.Count > 0 ? (double)distinct.Count / games.Count * 100 : 0):0.00}%)");
            _deletingRows = false;
        }

        private async void btnNNRecognizeGameStates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableControls(_buttons);
                ShowLoader(gridDataContainer);

                var games = gvData.Items.Cast<TicTacToeGame>().ToList();
                var numHidden = numHiddenNeurons.Value.ToInt();
                var percTrainSet = numPercTrainSet.Value.ToInt();
                var percTestSet = numPercTestSet.Value.ToInt();

                var maxEpochs = numMaxEpochs.Value.ToInt();
                var learnRate = numLearnRate.Value.ToDouble();
                var momentum = numMomentum.Value.ToDouble();
                var weightDecay = numWeightDecay.Value.ToDouble();

                await Task.Run(() =>
                {
                    if (!games.Any())
                        return;

                    var inputs = games.Select(x => x.State.SelectMany(y => y).Select(NeuralNetwork.EncodeClassAsBinary).SelectMany(z => z).Select(v => v.ToDouble())).ToJagged();
                    var outputs = games.Select(x => NeuralNetwork.EncodeGameResultAsArray(x.Result()).Select(y => y.ToDouble())).ToJagged();

                    var nn = new NeuralNetwork(new TrainSet(inputs, outputs), numHidden); //inputs[0].Length + outputs[0].Length
                    nn.Divide(percTrainSet, percTestSet);
                    nn.Train(maxEpochs, learnRate, momentum, weightDecay);
                    nn.PredictTrainSet();
                    nn.PredictTestSet();

                    var dataSet = nn.TrainSet.AsEnumerable().Concat(nn.TestSet.AsEnumerable()).ToArray();

                    var newGames = new List<TicTacToeGame>();
                    var i = 0;
                    foreach (var v in dataSet)
                    {
                        newGames.Add(new TicTacToeGame
                        {
                            Id = i,
                            State = v.Inputs.SplitInParts(6).Select(x =>
                                x.SplitInParts(2).Select(y =>
                                    NeuralNetwork.DecodeClassFromBinary(y.ToArray())).ToArray()).ToArray(),
                            NNResult = NeuralNetwork.DecodeGameResultFromArray(v.Predictions)
                        });
                        i++;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        gvData.RefreshWith(newGames);
                        gvData.SelectedItems.Clear();
                        gvData.Focus();

                        lblInfo.Content = new TextBlock
                        {
                            Text =
                                "Efficiency:\n" +
                                $"Training Set: {nn.TrainSet.Accuracy * 100:0.00}%\n" +
                                $"Test Set: {nn.TestSet.Accuracy * 100:0.00}%"
                        };
                    });
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Error occured", ex.Message);
            }
            finally
            {
                HideLoader(gridDataContainer);
                EnableControls(_buttons.Except(new[] { btnSave, btnDelete }));
            }
        }

        private void btnKeepOnlyIncorrectlyClassified_Click(object sender, RoutedEventArgs e)
        {
            var games = gvData.Items.Cast<TicTacToeGame>().ToList();
            var wrong = games.Where(g => g.Result() != g.NNResult).ToList();

            gvData.RefreshWith(wrong);
            gvData.SelectedItems.Clear();
            gvData.Focus();

            lblInfo.SetWrappedText($"Zachowano {wrong.Count} z {games.Count} ({(double)wrong.Count / games.Count * 100:0.00}%)");
        }
        
        private async void btnNNLearnHowToPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableControls(_buttons);
                ShowLoader(gridDataContainer);

                var games = gvData.Items.Cast<TicTacToeGame>().ToList();
                var numHidden = numHiddenNeurons.Value.ToInt();

                if (!games.Any() || !games.Any(g => g.Result().EqualsAny(GameResult.XWon, GameResult.OWon)))
                {
                    await this.ShowMessageAsync("Error occured", "Data set doesn't contain any game states (won) to correctly train Neural Network");
                    return;
                }

                var maxEpochs = numMaxEpochs.Value.ToInt();
                var learnRate = numLearnRate.Value.ToDouble();
                var momentum = numMomentum.Value.ToDouble();
                var weightDecay = numWeightDecay.Value.ToDouble();

                await Task.Run(() =>
                {
                    var xWins = games.Where(g => g.Result() == GameResult.XWon).Select(x => x.State).ToArray();
                    var oWinsAsXWins = games.Where(g => g.Result() == GameResult.OWon).Select(x => x.Opposite().State).ToArray();
                    var wins = xWins.Concat(oWinsAsXWins).ToArray();
                    var binWins = wins.Select(x => x.SelectMany(y => y).Select(NeuralNetwork.EncodeClassAsBinary).SelectMany(z => z).Select(v => v.ToDouble())).ToJagged();
                    var arrWins = wins.Select(x => x.SelectMany(y => y).Select(NeuralNetwork.EncodeSAsArray).SelectMany(z => z).Select(v => v.ToDouble())).ToJagged();

                    var inputs = binWins.Copy();
                    var outputs = arrWins.Copy();

                    _nnHowToPlayX = new NeuralNetwork(new TrainSet(inputs, outputs), numHidden);
                    _nnHowToPlayX.Divide(100, 0);
                    _nnHowToPlayX.Train(maxEpochs, learnRate, momentum, weightDecay);
                });

                lblInfo.SetWrappedText("Sieć nauczono grać");
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Error occured", ex.Message);
            }
            finally
            {
                HideLoader(gridDataContainer);
                EnableControls(_buttons.Except(new[] { btnSave, btnDelete }));
            }
        }

        private void btnNNHint_Click(object sender, RoutedEventArgs e)
        {
            if (_nnHowToPlayX == null)
            {
                Dispatcher.Invoke(() => this.ShowMessageAsync("Error occured", "You need to train Neural Network how to play first"));
                return;
            }
            
            var currSymbol = TicTacToeGame.Opposite(_gameBoard.PrevSymbol);

            if (currSymbol != S.X) // trzeba użyć odwrotnej planszy, bo sieć jest nauczona grać iksem
            {
                var invertedBoard = new TicTacToeBoard();
                invertedBoard.Create(new Canvas { Width = cvBoard.Width, Height = cvBoard.Height });
                invertedBoard.GameState = _gameBoard.GameState.Opposite();
                invertedBoard.PlaceNNShadow(_gameBoard.PrevSymbol, _nnHowToPlayX); // prevSymbol to odwrotność obecnego
                //_gameBoard.GameState = invertedBoard.GameState.Opposite();
                var oppositeShadow = invertedBoard.GetShadow();
                _gameBoard.PlaceShadow(ShadowType.CircleShadow, oppositeShadow.I, oppositeShadow.J);
            }
            else
                _gameBoard.PlaceNNShadow(currSymbol, _nnHowToPlayX);
        }

        private async void btnGenerateGames_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableControls(_buttons);
                ShowLoader(gridDataContainer);

                var loadingMode = (LoadingMode)ddlLoadingMode.SelectedDdlItem().Index;
                var merge = loadingMode == LoadingMode.Merge;
                var games = merge ? gvData.Items.Cast<TicTacToeGame>().ToList() : new List<TicTacToeGame>();
                var maxID = games.Any() ? games.Select(g => g.Id).Max() : 0;
                var nGames = numGames.Value ?? 0;
                var gamesType = (GeneratedGamesType)ddlGeneratedGamesType.SelectedDdlItem().Index;
                var tempGames = new List<TicTacToeGame>();

                await Task.Run(() =>
                {
                    if (gamesType == GeneratedGamesType.VsRandomAi)
                    {
                        for (var i = 0; i < nGames; i++)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _gameBoard.GameState = TicTacToeGame.Empty();
                                while (_gameBoard.GameState.Result() == GameResult.Unfinished)
                                    _gameBoard.MakeRandomMove();
                                tempGames.Add(new TicTacToeGame(++maxID, _gameBoard.GameState.State));
                            });
                        }
                        games.AddRange(tempGames);
                    }
                    else if (gamesType == GeneratedGamesType.VsRandomAiWithUnfinished)
                    {
                        for (var i = 0; i < nGames; i++)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var length = _gameBoard.GameState.State.Length * _gameBoard.GameState.State[0].Length;
                                var maxMoves = _rng.Next(1, length);
                                var moveNum = 0;
                                _gameBoard.GameState = TicTacToeGame.Empty();
                                while (_gameBoard.GameState.Result() == GameResult.Unfinished && ++moveNum <= maxMoves)
                                    _gameBoard.MakeRandomMove();
                                tempGames.Add(new TicTacToeGame(++maxID, _gameBoard.GameState.State));
                            });
                        }
                        games.AddRange(tempGames);
                    }
                    else if (gamesType == GeneratedGamesType.VsNNAi)
                    {
                        if (_nnHowToPlayX == null)
                        {
                            Dispatcher.Invoke(() => this.ShowMessageAsync("Error occured", "You need to train Neural Network how to play first"));
                            return;
                        }
                        var prevStartGameSym = _rng.Next(0, 2).ToEnum<S>();
                        for (var i = 0; i < nGames; i++)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _gameBoard.GameState = TicTacToeGame.Empty();
                                var prevSymbol = _gameBoard.MakeRandomMove(prevStartGameSym);
                                
                                while (_gameBoard.GameState.Result() == GameResult.Unfinished)
                                {
                                    var currSym = TicTacToeGame.Opposite(prevSymbol);
                                    if (currSym == S.O)
                                        _gameBoard.MakeRandomMove(S.O);
                                    else if (currSym == S.X)
                                        _gameBoard.MakeNNMove(S.X, _nnHowToPlayX);
                                    else
                                        throw new Exception("Valid move is either 'O' or 'X'");
                                    prevSymbol = currSym;
                                }
                                    
                                tempGames.Add(new TicTacToeGame(++maxID, _gameBoard.GameState.State));
                                prevStartGameSym = TicTacToeGame.Opposite(prevStartGameSym);
                            });
                        }
                        games.AddRange(tempGames);
                    }

                    var ntmp = tempGames.Count;
                    var ntmpOWon = tempGames.Count(g => g.Result() == GameResult.OWon);
                    var ntmpXWon = tempGames.Count(g => g.Result() == GameResult.XWon);
                    var ntmpDraws = tempGames.Count(g => g.Result() == GameResult.Draw);
                    var ptmpOWon = Math.Round(ntmpOWon.ToDouble() / ntmp * 100);
                    var ptmpXWon = Math.Round(ntmpXWon.ToDouble() / ntmp * 100);
                    var ptmpDraws = Math.Round(ntmpDraws.ToDouble() / ntmp * 100);
                    var nall = games.Count;
                    var nallOWon = games.Count(g => g.Result() == GameResult.OWon);
                    var nallXWon = games.Count(g => g.Result() == GameResult.XWon);
                    var nallDraws = games.Count(g => g.Result() == GameResult.Draw);
                    var pallOWOn = Math.Round(nallOWon.ToDouble() / nall * 100);
                    var pallXWOn = Math.Round(nallXWon.ToDouble() / nall * 100);
                    var pallDraws = Math.Round(nallDraws.ToDouble() / nall * 100);

                    Dispatcher.Invoke(() =>
                    {
                        lblInfo.SetWrappedText(
                            $"Generated {nGames} (Total: {games.Count})\n" +
                            $"New - O: {ptmpOWon}%, X: {ptmpXWon}%, Draws: {ptmpDraws}%\n" +
                            $"Total - O: {pallOWOn}%, X: {pallXWOn}%, Draws: {pallDraws}%\n" +
                            "(Neural Network Plays with 'X')");
                        gvData.RefreshWith(games);
                        gvData.SelectedItems.Clear();
                        gvData.Focus();
                        _gameBoard.GameState = TicTacToeGame.Empty();
                    });
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Error occured", ex.Message);
            }
            finally
            {
                HideLoader(gridDataContainer);
                EnableControls(_buttons.Except(new[] { btnSave, btnDelete }));
            }
        }

        #endregion

        #region - Checkbox Events



        #endregion

        #region - Textbox Events

        private static void TxtAll_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.ClearValue();
        }

        private static void TxtAll_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.ResetValue();
        }

        #endregion

        #region - TIle Events

        private void tlSaveToFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var senderElement = sender as FrameworkElement;
            if (senderElement == null) return;

            var strData = gvData.Items.Cast<TicTacToeGame>().Select(x => x.ToString() + "," + x.ResultString.Before("(").Trim()).ToDelimitedString("\n");
            var fd = new FileDescriptor
            {
                Name = "KółkoKrzyżyk.txt",
                Contents = Encoding.UTF8.GetBytes(strData)
            };
            var fullPath = SaveFIleToTemp(fd);
            var dragObj = new DataObject();
            dragObj.SetFileDropList(new StringCollection { fullPath });
            DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);
        }

        private static string SaveFIleToTemp(FileDescriptor file)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), file.Name);
            Stream fs = File.Create(tempFilePath);
            new BinaryWriter(fs).Write(file.Contents);
            fs.Close();
            return tempFilePath;
        }

        private void tlSave_MouseEnter(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            HighlightTile(tile, _mouseOverMainMenuTileColor);
        }

        private void tlSave_MouseLeave(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            UnhighlightTile(tile, _defaultMainMenuTileColor);
        }

        private async void tlLoadFromFile_Drop(object sender, DragEventArgs e)
        {
            try
            {
                DisableControls(_buttons);
                ShowLoader(gridDataContainer);
                await Task.Run(() =>
                {
                    if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                        return;
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files == null)
                        return;
                    var url = files[0];

                    Dispatcher.Invoke(() =>
                    {
                        var tl = (Tile)sender;
                        var tlTemplateUnloaded = tl.ContentTemplate;
                        var tlTemplate = tlTemplateUnloaded.LoadContent();
                        var descrBlock = tlTemplate.FindLogicalChildren<TextBlock>().Single();
                        var prevText = descrBlock.Text.Before("(").Trim();
                        descrBlock.Text = $"{prevText} (.{url.From("\\")})"; // TODO: Nie updatuje ContentTemplate'a nowym napisem
                    });

                    LoadSetFrom(url);
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Error occured", ex.Message);
            }
            finally
            {
                HideLoader(gridDataContainer);
                EnableControls(_buttons.Except(new[] { btnSave, btnDelete }));
            }
        }

        private void tlLoadFromFile_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void tlLoadFromFile_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void tlLoad_MouseEnter(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            HighlightTile(tile, _mouseOverMainMenuTileColor);
        }

        private void tlLoad_MouseLeave(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            UnhighlightTile(tile, _defaultMainMenuTileColor);
        }

        private void tlLoad_PreviewDrop(object sender, DragEventArgs e)
        {
            //e.Handled = true;
        }

        private void tlLoad_PreviewDragEnter(object sender, DragEventArgs e)
        {
            //e.Handled = true;
        }

        #endregion

        #region - Dropdown Events

        #endregion

        #region - Gridview Events

        private void gvData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_deletingRows)
                return;
            // Selection changed jest wywoływane przy usuwaniu też!
            if (gvData.SelectedItems.Count == 1)
            {
                var selectedGameState = gvData.SelectedItems.Cast<TicTacToeGame>().Single();
                _gameBoard.GameState = selectedGameState.DeepClone();
                lblGameStatus.Content = _gameBoard.GameState.ResultString;
                btnSave.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
            else if (gvData.SelectedItems.Count > 1)
            {
                _gameBoard.GameState = TicTacToeGame.Empty();
                lblGameStatus.Content = "";
                btnSave.IsEnabled = false;
                btnDelete.IsEnabled = true;
            }
            else
            {
                _gameBoard.GameState = TicTacToeGame.Empty();
                lblGameStatus.Content = "";
                btnSave.IsEnabled = false;
                btnDelete.IsEnabled = false;
            }
        }

        private void gvData_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                DeleteSelectedRows();
        }

        #endregion

        #region - NumUpDOwns Events

        private void numPercTrainSet_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            numPercTestSet.Value = numPercTestSet.Maximum - e.NewValue;
        }

        private void numPercTestSet_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            numPercTrainSet.Value = numPercTrainSet.Maximum - e.NewValue;
        }

        #endregion

        #region - Notifyicon Events

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            _notifyIcon.Visible = false;
            WindowState = WindowState.Normal;

            if (IsVisible)
                Activate();
            else
                Show();
        }

        #endregion

        #region - Canvas Events

        private void cvBoard_MouseMove(object sender, MouseEventArgs e)
        {
            var mode = (Mode) ddlMode.SelectedDdlItem().Index;
            var pos = e.GetPosition(cvBoard);
            if (mode == Mode.Edit)
            {
                //
            }
            else
            {
                _gameBoard.PlaceShadow(pos);
            }
        }

        private void cvBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mode = (Mode) ddlMode.SelectedDdlItem().Index;
            var pos = e.GetPosition(cvBoard);
            if (mode == Mode.Edit)
            {
                _gameBoard.EditField(pos);
            }
            else if (mode == Mode.TwoPlayers && _gameBoard.IsFieldEmpty(pos))
            {
                _gameBoard.MakeMove(pos);
            }
            else if (mode == Mode.PlayerVsRandomAi && _gameBoard.IsFieldEmpty(pos))
            {
                _gameBoard.MakeMove(pos);
                _gameBoard.MakeRandomMove(); // if (_gameBoard.GameState.Result() == GameResult.Unfinished) wewnątrz
            }
            else if (mode == Mode.PlayerVsNNAi && _gameBoard.IsFieldEmpty(pos))
            {
                if (_nnHowToPlayX == null)
                {
                    Dispatcher.Invoke(() => this.ShowMessageAsync("Error occured", "You need to train Neural network how to play first"));
                    return;
                }

                var prevSymbol = _gameBoard.MakeMove(pos);
                var currSymbol = TicTacToeGame.Opposite(prevSymbol);

                if (currSymbol != S.X) // trzeba użyć odwrotnej planszy, bo sieć jest nauczona grać iksem
                { 
                    var invertedBoard = new TicTacToeBoard();
                    invertedBoard.Create(new Canvas { Width = cvBoard.Width, Height = cvBoard.Height });
                    invertedBoard.GameState = _gameBoard.GameState.Opposite();
                    invertedBoard.MakeNNMove(prevSymbol, _nnHowToPlayX); // prevSymbol to odwrotność obecnego
                    _gameBoard.GameState = invertedBoard.GameState.Opposite();
                    _gameBoard.NextSymbol(); // zmieniamy ręcznie na krzyżyk, bo ruch był zrobiony na odwrotnej planszy, więc normalna go nie zarejestrowała
                }
                else
                    _gameBoard.MakeNNMove(currSymbol, _nnHowToPlayX);
            }
            lblGameStatus.Content = _gameBoard.GameState.ResultString;
        }

        private void cvBoard_MouseLeave(object sender, MouseEventArgs e)
        {
            _gameBoard.ClearShadow();
        }

        #endregion

        #endregion

        #region Methods

        #region - Controls Management

        private void InitializeControlGroups()
        {
            _buttons = this.FindLogicalChildren<Button>().Where(b => b.GetType() != typeof(MahApps.Metro.Controls.Tile)).ToList();
        }

        private void SetupDropdowns()
        {
            ddlMode.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) Mode.Edit, "Edit"),
                new DdlItem((int) Mode.TwoPlayers, "Two Players"),
                new DdlItem((int) Mode.PlayerVsRandomAi, "Player vs Random AI"),
                new DdlItem((int) Mode.PlayerVsNNAi, "Player vs Neural Network"),
            };
            ddlMode.DisplayMemberPath = "Text";
            ddlMode.SelectedValuePath = "Index";
            ddlMode.SelectByCustomId(0);
            //ddlMode.SelectionChanged += ddlMode_SelectionChanged;

            ddlGeneratedGamesType.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) GeneratedGamesType.VsRandomAi, "vs Random AI"),
                new DdlItem((int) GeneratedGamesType.VsRandomAiWithUnfinished, "vs Random AI (Not every game finished)"),
                new DdlItem((int) GeneratedGamesType.VsNNAi, "vs Neural Network")
            };
            ddlGeneratedGamesType.DisplayMemberPath = "Text";
            ddlGeneratedGamesType.SelectedValuePath = "Index";
            ddlGeneratedGamesType.SelectByCustomId(0);

            ddlLoadingMode.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) LoadingMode.Replace, "Replace"),
                new DdlItem((int) LoadingMode.Merge, "Merge")
            };
            ddlLoadingMode.DisplayMemberPath = "Text";
            ddlLoadingMode.SelectedValuePath = "Index";
            ddlLoadingMode.SelectByCustomId(0);
        }

        private void SetupUpDowns()
        {
            numPercTrainSet.ValueChanged += numPercTrainSet_ValueChanged;
            numPercTestSet.ValueChanged += numPercTestSet_ValueChanged;
        }

        private void SetupNotifyIcon()
        {
            var iconHandle = Properties.Resources.NotifyIcon.GetHicon();
            var icon = System.Drawing.Icon.FromHandle(iconHandle);

            _notifyIcon = new NotifyIcon
            {
                BalloonTipTitle = @"Program for Neural Network TipcTacToe Game",
                BalloonTipText = @"is hidden here",
                Icon = icon
            };
            _notifyIcon.Click += notifyIcon_Click;
        }

        private void SetupButtons()
        {
            btnSave.IsEnabled = false;
        }

        private void SetupTextBoxes()
        {
            foreach (var txtB in this.FindLogicalChildren<TextBox>().Where(t => t.Tag != null))
            {
                txtB.GotFocus += TxtAll_GotFocus;
                txtB.LostFocus += TxtAll_LostFocus;

                var currBg = ((SolidColorBrush) txtB.Foreground).Color;
                txtB.FontStyle = FontStyles.Italic;
                txtB.Text = txtB.Tag.ToString();
                txtB.Foreground = new SolidColorBrush(WPFColor.FromArgb(128, currBg.R, currBg.G, currBg.B));
            }
        }

        private void SetupGridviews()
        {
            //
        }

        private void SetupTiles()
        {
            _mouseOverMainMenuTileColor = ((SolidColorBrush)FindResource("MouseOverMainMenuTileBrush")).Color;
            _defaultMainMenuTileColor = ((SolidColorBrush)FindResource("DefaultMainMenuTileBrush")).Color;
            tlSave.Background = new SolidColorBrush(_defaultMainMenuTileColor);
            tlLoad.Background = new SolidColorBrush(_defaultMainMenuTileColor);
        }

        private void SetupGameBoard()
        {
            _gameBoard = new TicTacToeBoard();
            _gameBoard.Create(cvBoard);
        }

        private static void DisableControls(IEnumerable<UIElement> controls)
        {
            foreach (var c in controls)
                c.IsEnabled = false;
        }

        private static void EnableControls(IEnumerable<UIElement> controls)
        {
            foreach (var c in controls)
                c.IsEnabled = true;
        }

        private static void ToggleControls(IEnumerable<UIElement> controls)
        {
            foreach (var c in controls)
                c.IsEnabled = !c.IsEnabled;
        }
        
        private void ShowLoader(Panel control)
        {
            var rect = new Rectangle
            {
                Margin = new Thickness(0),
                Fill = new SolidColorBrush(WPFColor.FromArgb(192, 40, 40, 40)),
                Name = "prLoaderContainer"
            };

            var loader = new ProgressRing
            {
                Foreground = (Brush)FindResource("AccentColorBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 80,
                Height = 80,
                IsActive = true,
                Name = "prLoader"
            };

            Panel.SetZIndex(rect, 10000);
            Panel.SetZIndex(loader, 10001);

            control.Children.Add(rect);
            control.Children.Add(loader);
        }

        private static void HideLoader(Panel control)
        {
            var loaders = control.FindLogicalChildren<ProgressRing>().Where(c => c.Name == "prLoader" ).ToArray();
            var loaderContainers = control.FindLogicalChildren<Rectangle>().Where(c => c.Name == "prLoaderContainer" ).ToArray();

            loaders.ForEach(l => l.IsActive = false);

            loaders.ForEach(l => control.Children.Remove(l));
            loaderContainers.ForEach(r => control.Children.Remove(r));
        }

        private static Style CreateCellStyle()
        {
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Center));
            cellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
            return cellStyle;
        }

        private void DeleteSelectedRows()
        {
            _deletingRows = true;
            var games = gvData.Items.Cast<TicTacToeGame>().ToList();
            var selectedGames = gvData.SelectedItems.Cast<TicTacToeGame>().ToList();
            games.RemoveAll(g => selectedGames.Contains(g));
            gvData.RefreshWith(games);
            btnSave.IsEnabled = false;
            btnDelete.IsEnabled = false;
            _gameBoard.GameState = TicTacToeGame.Empty();
            lblGameStatus.Content = "";
            _deletingRows = false;
        }

        private void HighlightTile(Tile tile, WPFColor color)
        {
            var colorAni = new ColorAnimation(color, new Duration(TimeSpan.FromMilliseconds(500)));
            tile.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAni);
        }

        private void UnhighlightTile(Tile tile, WPFColor defaultColor)
        {
            var colorAni = new ColorAnimation(defaultColor, new Duration(TimeSpan.FromMilliseconds(500)));
            tile.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAni);
        }

        #endregion

        #region - Core

        private void LoadSetFrom(string url)
        {
            var loadingMode = Dispatcher.Invoke(() => (LoadingMode) ddlLoadingMode.SelectedDdlItem().Index);
            var merge = loadingMode == LoadingMode.Merge;
            var games = Dispatcher.Invoke(() => gvData.Items.Cast<TicTacToeGame>().ToList());
            var newGames = new List<TicTacToeGame>();

            using (var fs = File.OpenRead(url))
            using (var sr = new StreamReader(fs, Encoding.UTF8, true, 128))
            {
                string line;
                var lineNr = merge && games.Any() ? games.MaxBy(x => x.Id).Id : 0;
                while (!(line = sr.ReadLine()).EqualsAny(null, string.Empty))
                {
                    lineNr++;
                    var arrGame = line.Split(",").Take(9).Select(w => w.Trim().ToEnumOrDefault(S._)).SplitInParts(3).ToJagged();
                    newGames.Add(new TicTacToeGame(lineNr, arrGame));
                }
            }

            Dispatcher.Invoke(() => gvData.RefreshWith(merge ? games.Concat(newGames).ToList() : newGames));
        }

        #endregion

        #endregion
    }

    #region Enums

    public enum Mode
    {
        Edit,
        TwoPlayers,
        PlayerVsRandomAi,
        PlayerVsNNAi
    }

    public enum LoadingMode
    {
        Replace,
        Merge
    }

    public enum GeneratedGamesType
    {
        VsRandomAi,
        VsNNAi,
        VsRandomAiWithUnfinished
    }

    #endregion
}
