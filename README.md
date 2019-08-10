## Neural Network TicTacToe v1.0

### Download:

[![Download](https://img.shields.io/badge/download-NeuralNetworkTicTacToe--v1.0-blue.svg)](https://github.com/rvnlord/NeuralNetworkTicTacToe/releases/download/v1.0/TipTacToe.exe)
![Calculations](https://img.shields.io/badge/SHA--256-1DD49CED74D8C2E48E1B49F13DCEF4E2985F0C6EA7938FDFA942CA579F7967A7-darkgreen.svg)

   ![Interface](/Images/2019-02-28_141837.png?raw=true)

### How to use:

* "Drop file here to Load" - allows user to load a file containing valid game states.
* "Drag to a folder in order to Save" - allows user to save the table of game states to a file.
* first dropdown - controls whether newly added game states will replace an existing table or will be merged to it.
* second dropdown - controls the way user interacts with the game board.
* "Add" - adds the current (visible on the board) game state to the table.
* "Save" - places the game state from the board in the selected table record.
* "Remove" - deletes the selected game state from the table.
* "New Game" - clears the board.
* "Unique" - removes duplicates from the table.
* "Generate" - generates 'n' games and adds them into the table. Games can be generated as played between random AIs or random AI and Neural Network. Generating games vs Neural Network requires prior training. Training as a source uses only won games.
* "NN recognize game states" - teaches the neural network to guess if the game has been won, lost or is in a different state based on the results from the table. Then the results are presented in the last column (for both training and test set).
* "Only incorrectly classified" - removes correct classifiactions from the table.
* "NN - learn to play with data" - teaches Neural Network how to win games.
* "NN hint" - shows the optimal move for the current player on the board.
* right section of user interface - allows user to control the Neural Network parameters (Neural Network in this project always uses single hidden layer appropriately with hyper tangent and soft max activation functions)

You should keep in mind that results are highly dependent on the quality of the datasets.


### Known issues:
* Currently the program will try to display game states during the generation process, this can cause response delays, especially for large data sets
* Despite asynchronous calls there is no feedback during the process of Neural Network training, so for large sets or many neurons there may be an impression of hanging on loading screen.
* Communication between game state model and board view is an absolute MESS - I know that, I just didn't have much time to code it and I didn't go back to this project ever since. So one may ask why did I publish this at all? Because some people requested it.










