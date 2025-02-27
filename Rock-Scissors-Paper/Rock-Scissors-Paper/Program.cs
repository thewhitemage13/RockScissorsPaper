using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Rock_Scissors_Paper
{
    class Server
    {
        private static UdpClient udpServer;
        private static IPEndPoint player1Endpoint, player2Endpoint;
        private static Dictionary<string, string> rules = new Dictionary<string, string>
        {
            {"Rock", "Scissors"},
            {"Scissors", "Paper"},
            {"Paper", "Rock"}
        };

        private static string player1Choice = null, player2Choice = null;
        private static int player1Score = 0, player2Score = 0;
        private static int roundsPlayed = 0;
        private const int MaxRounds = 5;

        static void Main()
        {
            try
            {
                udpServer = new UdpClient(8080);
                Console.WriteLine("UDP Server is running on port 8080... Waiting for players...");

                while (player1Endpoint == null || player2Endpoint == null)
                {
                    ReceiveConnection();
                }

                Console.WriteLine("Both players are connected! Let the game begin...");

                while (roundsPlayed < MaxRounds)
                {
                    player1Choice = player2Choice = null;
                    while (player1Choice == null || player2Choice == null)
                    {
                        ReceiveMoves();
                    }

                    string result = DetermineWinner();
                    roundsPlayed++;

                    SendToBothPlayers($"Round {roundsPlayed}: {result}");

                    Thread.Sleep(1000);
                }

                string finalResult = player1Score > player2Score ? "Player 1 wins!" :
                                     player2Score > player1Score ? "Player 2 wins!" : "Tie!";

                SendToBothPlayers($"Game over! {finalResult}");
                Console.WriteLine("Game over. The server is shutting down...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                udpServer?.Close();
            }
        }

        private static void ReceiveConnection()
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (message == "connect")
                {
                    if (player1Endpoint == null)
                    {
                        player1Endpoint = remoteEP;
                        udpServer.Send(Encoding.UTF8.GetBytes("You're Player 1."), 10, player1Endpoint);
                    }
                    else if (player2Endpoint == null && !remoteEP.Equals(player1Endpoint))
                    {
                        player2Endpoint = remoteEP;
                        udpServer.Send(Encoding.UTF8.GetBytes("You're Player 2."), 10, player2Endpoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when connecting a player: {ex.Message}");
            }
        }

        private static void ReceiveMoves()
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref remoteEP);
                string move = Encoding.UTF8.GetString(data).Trim();

                if (!rules.ContainsKey(move))
                {
                    Console.WriteLine($"Player sent an incorrect move: {move}");
                    return;
                }

                if (remoteEP.Equals(player1Endpoint))
                    player1Choice = move;
                else if (remoteEP.Equals(player2Endpoint))
                    player2Choice = move;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when receiving a move: {ex.Message}");
            }
        }

        private static string DetermineWinner()
        {
            if (player1Choice == player2Choice)
                return "Tie!";

            if (rules.ContainsKey(player1Choice) && rules[player1Choice] == player2Choice)
            {
                player1Score++;
                return $"Player 1 wins! ({player1Choice} beats {player2Choice})";
            }
            else
            {
                player2Score++;
                return $"Player 2 wins! ({player2Choice} beats {player1Choice})";
            }
        }

        private static void SendToBothPlayers(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                if (player1Endpoint != null)
                    udpServer.Send(data, data.Length, player1Endpoint);
                if (player2Endpoint != null)
                    udpServer.Send(data, data.Length, player2Endpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when sending data to players: {ex.Message}");
            }
        }
    }
}
