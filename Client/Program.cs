using System.Net.Sockets;
using System.Text;

class Client {
    static async Task Main(string[] args) {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try {
            await socket.ConnectAsync("127.0.0.1", 8888);
            Console.WriteLine($"Connection to {socket.RemoteEndPoint} estabilished");

            Console.Write("Enter action (1 - get a file, 2 - create a file, 3 - delete a file): > ");
            string str_action = Console.ReadLine();
            if (str_action == "exit") {
                var requestExitBytes = Encoding.UTF8.GetBytes("exit");
                await socket.SendAsync(requestExitBytes, SocketFlags.None);
                return;
            }
            int action = Convert.ToInt32(str_action);
            
            Console.Write("Enter filename: > ");
            string filename = Console.ReadLine();

            string content = "";
            if (action == 2) {
                Console.Write("Enter file content: > ");
                content = Console.ReadLine();
            }

            string request = "";
            if (action == 1) {
                request += "GET " + filename;
            }
            else if (action == 2) {
                request += "PUT " + filename + " " + content;
            }
            else if (action == 3) {
                request += "DELETE " + filename;
            } else {
                Console.WriteLine("Wrong action number");
                return;
            }
            var requestBytes = Encoding.UTF8.GetBytes(request);
            await socket.SendAsync(requestBytes, SocketFlags.None);
            Console.WriteLine("The request was sent.");

            byte[] buffer = new byte[512];
            int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

            Output(action, response);
        }
        catch (SocketException) {
            Console.WriteLine($"Failed to connect to {socket.RemoteEndPoint}");
        }
    }

    static void Output(int action, string response) {
        if (action == 1) {
            if (response == "404") {
                Console.WriteLine("The response says that the file was not found!");
            } else {
                Console.WriteLine("The content of the file is:");
                string[] arr_response = response.Split(' ');
                if (arr_response.Length > 1) {
                    for (int i = 1; i < arr_response.Length; i++) {
                        if (i != 1) {
                            Console.Write(" ");
                        }
                        Console.Write(arr_response[i]);
                    }
                }
            }
        }
        else if (action == 2) {
            if (response == "403") {
                Console.WriteLine("The response says that creating the file was forbidden");
            } else {
                Console.WriteLine("The response says that the file was created!");
            }
        }
        else {
            if (response == "403") {
                Console.WriteLine("The response says that the file was not found!");
            } else {
                Console.WriteLine("The response says that the file was successfully deleted!");
            }
        }
    }
}