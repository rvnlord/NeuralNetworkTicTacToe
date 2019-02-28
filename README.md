## Neural Network TicTacToe v1.0

### Download:

[![Download](https://img.shields.io/badge/download-NeuralNetworkTicTacToe--v1.0-blue.svg)](https://github.com/rvnlord/neuralNetowrkTicTacToe/releases/download/v1.0/TicTacToe.exe)
![Calculations](https://img.shields.io/badge/SHA--256-1DD49CED74D8C2E48E1B49F13DCEF4E2985F0C6EA7938FDFA942CA579F7967A7-darkgreen.svg)

   ![Interface](/Images/2019-02-28_141837.png?raw=true)

### How to use:

* "Drop file here to Load" - allows user to load valid file with game states directly into the table.
* "Drag to a folder in order to Save" - allows to save the table as file containing game states.
* first dropdown - controls if newly added game states will replace states already present or merge with them.
* second dropdown - controls mode of the gameboard.
* "Add" - adds the current game state (visible on gameboards) to the table.
* "Save" - replace game state selected in the table with the one present on the board.
* "Remove" - deletes selected game state from the table.
* "New Game" - clears the board.
* "Unique" - removes duplicates from the table
* "Generate" - generates n games and adds them into the table. Games can be venerated as played between random AIs or random AI and Neural Netowrk. Generating games vs Neural Netowrk requires training NN first. Training as its source uses only won games.
* "NN recognize game states" - teaches Neural Netowork how to guess if game was won, lost or otherwise based on results generated in the table. Then the results are presented in the last column (for both training and test set).
* "Only incorrectly classified" - removes correct classifiactions from the table.
* "NN - learn to play with data" - teaches neural netowrk how to win games.
* "NN hint" - shows on the board optimal move for the curent player.
* right section of user interface allows you to control Neural Network parameters (Neural Network in this project uses always single hidden layer appropriately with hyper tangent and soft max activation functions)

Be aware that your results largely depend on the quality of your training sets. 


### Known issues:
* This software currently tries to display game states on the board when generating, this may sometimes cause little responsivenes of the user interface especially when generating large sets.
* Despite async calls there is no feedback during the process of training Neural Netowrk, so for large sets or many neurons you won't know how long it will take.
* Communication between game state model and booard view is an aboslute MESS, I know that I didn't have much time to code it and I didn't go back to this project ever since. So one may ask why did I publish this? Because some people requested it.










