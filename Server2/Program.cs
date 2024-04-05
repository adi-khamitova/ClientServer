using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

class Server2 {
    static ConcurrentDictionary<int, string> fileIdMap = new ConcurrentDictionary<int, string>();
    static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    static int currentFileId = 0;
    static int currentClientId = 1;
    static bool isRunning = true;
    static async Task Main(string[] args) { 
        LoadFileIdMap();
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888); 
        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
        socket.Bind(ipPoint);
        socket.Listen();
        Console.WriteLine("Server started!\n");

        while (true) { 
            Socket client = await socket.AcceptAsync(); 
            _ = Task.Run(() => HandleClient(client));
        } 
    }

    static async Task HandleClient(Socket client) { 
        int clientId = currentClientId++;
        Console.WriteLine($"Client {clientId} connected. Client address: {client.RemoteEndPoint}"); 
        byte[] buffer = new byte[512]; 
        int bytesReceived = await client.ReceiveAsync(buffer, SocketFlags.None); 
        string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived); 
        if (data == "exit") {
            Console.WriteLine($"Client {clientId}: Client stopped the server");
            Environment.Exit(0);
        }
        Console.WriteLine($"Client {clientId}: Data received from client: {data}");

        string response = "";
        string[] request = data.Split(' ');
        string filepath = "";
        if (request[0] == "PUT") {
            response = await Put(request, filepath, client);
        }
        else if (request[0] == "GET") {
            response = await Get(request, filepath, client);
        }
        else if (request[0] == "DELETE") {
            response = Delete(request, filepath);
        }
        if (response != "send") {
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await client.SendAsync(responseBytes, SocketFlags.None);
        }
        Console.WriteLine($"Client {clientId}: Result sent to client\n"); 
        client.Shutdown(SocketShutdown.Both); 
        client.Close(); 
    }

    static async Task<string> Put(string[] request, string filepath, Socket client) {
        string response = "";
        string server_filepath = "", filename = "";
        using MemoryStream ms = new();
        if (request.Length == 3) {
            server_filepath = "./data/" + request[2];
            filename = request[2];
        } else {
            filename = GenerateUniqueFilename(request[1]);
            server_filepath = "./data/" + filename;
        }
        var buffer = new byte[1024];
        int b;
        do {
            b = await client.ReceiveAsync(buffer);
            ms.Write(buffer, 0, b);

        } while (b > 0);

        _lock.EnterWriteLock();
        try {
            using (FileStream fs = new(server_filepath, FileMode.CreateNew, FileAccess.Write)) {
                ms.WriteTo(fs);
            }
            currentFileId++;
            fileIdMap.TryAdd(currentFileId, filename);
            SaveFileIdMap(currentFileId, filename);
            response += "200 " + currentFileId;
        } catch {
            response += "403";
        }
        return response;
    }

    static async Task<string> Get(string[] request, string filepath, Socket client) {
        string response = "";
        if (request[1] == "BY_ID") {
            if (fileIdMap.ContainsKey(Convert.ToInt32(request[2]))) {
                filepath = "./data/" + Convert.ToString(fileIdMap[Convert.ToInt32(request[2])]);
            } else {
                response += "404";
                return response;
            }
        } else {
            filepath = "./data/" + request[2];
        }
        FileInfo file = new FileInfo(filepath);
        if (!file.Exists) {
            response += "404";
        } else {
            response += "200";
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await client.SendAsync(responseBytes, SocketFlags.None);
            await client.SendAsync(File.ReadAllBytes(filepath));
        }
        return "send";
    }

    static string Delete(string[] request, string filepath) {
        string response = "";
        string filename = "";
        if (request[1] == "BY_ID") {
            if (fileIdMap.ContainsKey(Convert.ToInt32(request[2]))) {
                filename = Convert.ToString(fileIdMap[Convert.ToInt32(request[2])]);
                filepath = "./data/" + filename;
            } else {
                response += "404";
                return response;
            }
        } else {
            filename = request[2];
            filepath = "./data/" + filename;
        }
        FileInfo file = new FileInfo(filepath);
        if (!file.Exists) {
            response += "404";
        } else {
            File.Delete(filepath);
            UpdateFileIdMapFile(filename);
            response += "200";
        }
        return response;
    }

    static void LoadFileIdMap() {
        string[] lines = File.ReadAllLines("file_id_map.txt");
        foreach (string line in lines) {
            string[] parts = line.Split(',');
            if (parts.Length == 3 && int.TryParse(parts[0], out int fileId)) {
                currentFileId++;
                if (parts[2] == "1")
                    fileIdMap[fileId] = parts[1];
            }
            
        }
    }

    static void SaveFileIdMap(int key, string name) {
        using (StreamWriter writer = new StreamWriter("file_id_map.txt", true)) {
            writer.WriteLine($"{key},{name},1");
        }
    }

    static string GenerateUniqueFilename(string client_filepath) {
        string extension = System.IO.Path.GetExtension(client_filepath);
        string uniqueName = Guid.NewGuid().ToString();
        return uniqueName + extension;
    }

    static void UpdateFileIdMapFile(string filename) {
        string filePath = "file_id_map.txt";
        string[] lines = File.ReadAllLines(filePath);
        int delete_id = 0;
        foreach (var key in fileIdMap.Keys) {
            if (fileIdMap[key] == filename) delete_id = key;
        }
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].StartsWith($"{delete_id},")) {
                lines[i] = $"{delete_id},{filename},0";
                break;
            }
        }
        File.WriteAllLines(filePath, lines);
    }
}