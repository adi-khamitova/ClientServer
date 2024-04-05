using System.Net.Sockets;
using System.Text;

class Client {
    static async Task Main(string[] args) {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try {
            await socket.ConnectAsync("127.0.0.1", 8888);
            Console.WriteLine($"Connection to {socket.RemoteEndPoint} estabilished");

            Console.Write("Enter action (1 - get a file, 2 - save a file, 3 - delete a file): > ");
            string str_action = Console.ReadLine();
            if (str_action == "") return;
            if (str_action == "exit") {
                var requestExitBytes = Encoding.UTF8.GetBytes("exit");
                await socket.SendAsync(requestExitBytes, SocketFlags.None);
                return;
            }
            int action = Convert.ToInt32(str_action);
            string filename = "";

            int by_id = 0;
            string id = "";
            if (action == 1) {
                Console.Write("Do you want to get the file by name or by id (1 - id, 2 - name): > ");
                by_id = Convert.ToInt32(Console.ReadLine());
            }
            if (action == 3) {
                Console.Write("Do you want to delete the file by name or by id (1 - id, 2 - name): > ");
                by_id = Convert.ToInt32(Console.ReadLine());
            }
            if (by_id == 1) {
                Console.Write("Enter id: > ");
                id = Console.ReadLine();
            } else if (by_id == 2) {
                Console.Write("Enter filename: > ");
                filename = Console.ReadLine();
            }

            string client_filename = "";
            string server_filename = "";
            if (action == 2) {
                Console.Write("Enter filename: > ");
                client_filename = Console.ReadLine();
                FileInfo cl_file = new FileInfo("./data/" + client_filename);
                if (!cl_file.Exists) {
                    Console.WriteLine("No such file!");
                    return;
                }
                Console.Write("Enter name of the file to be saved on server: > ");
                server_filename = Console.ReadLine();
            }

            string request = "";
            if (action == 1) {
                if (by_id == 1) {
                    request += "GET BY_ID " + id;
                } else {
                    request += "GET BY_NAME " + filename;
                }
            }
            else if (action == 2) {
                FileInfo file = new FileInfo("./data/" + client_filename);
                if (!file.Exists) {
                    Console.WriteLine("No such file");
                    return;
                }
                request += "PUT " + client_filename;
                if (server_filename != "") {
                request += " " + server_filename;
                }
            }
            else if (action == 3) {
                if (by_id == 1) {
                    request += "DELETE BY_ID " + id;
                } else {
                    request += "DELETE BY_NAME " + filename;
                }
            } else {
                Console.WriteLine("Wrong action number");
                return;
            }

            var requestBytes = Encoding.UTF8.GetBytes(request);
            await socket.SendAsync(requestBytes, SocketFlags.None);

            if (action == 2) {
                var file_send = "./data/" + client_filename;
                await socket.SendAsync(File.ReadAllBytes(file_send));
                socket.Shutdown(SocketShutdown.Send);
            }
            Console.WriteLine("The request was sent.");

            byte[] buffer = new byte[512];
            int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

            Output(action, response, socket);
        }
        catch (SocketException) {
            Console.WriteLine($"Failed to connect to {socket.RemoteEndPoint}");
        }
    }

    static async Task Output(int action, string response, Socket socket) {
        if (action == 1) {
            if (response == "404") {
                Console.WriteLine("The response says that the file was not found!");
            } else {
                using MemoryStream ms = new();
                Console.Write("the file was downloaded! Specify a name for it: > ");
                string newfilename = Console.ReadLine();
                var buffer = new byte[1024];
                int b;
                do {
                    b = await socket.ReceiveAsync(buffer);
                    ms.Write(buffer, 0, b);
                } while (b > 0);
                using (FileStream fs = new("./data/" + newfilename, FileMode.CreateNew, FileAccess.Write)) {
                    ms.WriteTo(fs);
                }
            }
        }
        else if (action == 2) {
            if (response == "403") {
                Console.WriteLine("The response says that creating the file was forbidden");
            } else {
                string[] arr_response = response.Split(' ');
                Console.WriteLine($"Response says that the file was saved! ID = {arr_response[1]}");
            }
        }
        else {
            if (response == "404") {
                Console.WriteLine("The response says that the file was not found!");
            } else {
                Console.WriteLine("The response says that the file was successfully deleted!");
            }
        }
    }
}