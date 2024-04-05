using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server {
    static int currentClientId = 1;
    static bool isRunning = true;
    static async Task Main(string[] args) {
        Task serverTask = Task.Run(async () => {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen();
            Console.WriteLine("Server is running\n");

            while (isRunning) {
                // Ожидание входящего подключения или нажатия клавиши Q
                await Task.WhenAny(WaitForConnection(socket), CheckForKeyPress());

                if (Console.KeyAvailable) {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Q) {
                        isRunning = false;
                        break;
                    }
                }
            }
        });

        await serverTask;
    }

    static async Task WaitForConnection(Socket socket) {
        Console.WriteLine("Waiting for connect...");
        using Socket client = await socket.AcceptAsync();
        Console.WriteLine($"Client connected. Client address: {client.RemoteEndPoint}");

        byte[] buffer = new byte[512];
        int bytesReceived = await client.ReceiveAsync(buffer, SocketFlags.None);
        string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
        if (data == "exit") {
            Console.WriteLine("Client stopped the server");
            Environment.Exit(0);
        }
        Console.WriteLine($"Data received from client: {data}");

        string response = "";
        string[] request = data.Split(' ');
        string filepath = "./data/" + request[1];
        if (request[0] == "PUT") {
            Put(request, ref response, filepath);
        }
        else if (request[0] == "GET") {
            Get(request, ref response, filepath);
        }
        else if (request[0] == "DELETE") {
            Delete(request, ref response, filepath);
        }

        // Отправляем результат клиенту
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        await client.SendAsync(responseBytes, SocketFlags.None);
        Console.WriteLine($"Result sent to client: {response}\n");
    }

    static async Task CheckForKeyPress() {
        while (!Console.KeyAvailable) {
            await Task.Delay(100);
        }
    }

    static void Put(string[] request, ref string response, string filepath) {
        FileInfo file = new FileInfo(filepath);
            if (file.Exists) {
                response += "403";
            } else {
                StreamWriter sw = file.CreateText();
                for (int i = 2; i < request.Length; i++) {
                    sw.Write(request[i] + " ");
                }
                sw.Close();
                response += "200";
            }
    }

    static void Get(string[] request, ref string response, string filepath) {
        FileInfo file = new FileInfo(filepath);
            if (!file.Exists) {
                response += "404";
            } else {
                response += "200";
                string[] lines = File.ReadAllLines(filepath);
                response += " ";
                for (int i = 0; i < lines.Length; i++) {
                    if (i != 0) response += "\n";
                    response += lines[i];
                    Console.WriteLine(lines[i]);
                }
            }
    }

    static void Delete(string[] request, ref string response, string filepath) {
        FileInfo file = new FileInfo(filepath);
            if (!file.Exists) {
                response += "404";
            } else {
                File.Delete(filepath);
                response += "200";
            }
    }
}