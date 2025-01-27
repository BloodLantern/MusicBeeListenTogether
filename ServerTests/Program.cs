using System;
using MusicBeePlugin;

internal class Program
{
    public static void Main(string[] args)
    {
        ListenTogetherServer server = new ListenTogetherServer();
        server.SetupServer();

        Console.ReadKey();

        server.StopServer();
    }
}
