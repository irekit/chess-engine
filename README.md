# chess-engine

This is a chess engine using Monte-Carlo Tree Search (MCTS) combined with static evaluation methods, named after its emergent tendency to play the Wayward Queen Attack + Scholar's Mate. I made this project after being inspired by youtuber Sebastian Lague's video series on creating a chess engine using Minimax Search. I then got interested in chess, and heard of Deepmind's alphazero project, based off the alphago engine which was the first to beat a world champion at go. It uses a neural network combined with monte carlo tree search. I researched into reinforcement learning and neural networks, and tried to make a monte carlo tree search + neural network algorithm. However, it had major flaws. Firstly, I had a fixed number of samples for each legal move, instead of having a fixed number of samples for all legal moves. Secondly, it was my first time training a neural network, and I didn't really have experience, and I messed it up. Thirdly, I used a SoftMax on the outputs of the neural network for each legalmove, and instead of making an output head that makes moves, I only made an evaluation network that evaluates each legalmove, being extremely expensive. I decided to scrap the neural network and instead use a conventional static evaluation function for each legal move.



Download the zip file, then uncompress it, and open the .exe file.



No AI was used to write any code or generate any assets.

