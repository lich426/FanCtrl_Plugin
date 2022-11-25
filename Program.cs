using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using FanCtrl;

class Program
{
    static void Main(string[] args)
    {
        var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            string serverIP = "127.0.0.1";
            int serverPort = 9989;
            var ep = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            sock.Connect(ep);
            var task = send(sock);

            var packet = new PluginPacket(serverIP, serverPort);
            while (true)
            {
                try
                {
                    var buffer = new byte[4096];
                    int recvBytes = sock.Receive(buffer);

                    packet.addRecvData(buffer, recvBytes);

                    while (true)
                    {
                        string jsonString = "";
                        if (packet.processData(ref jsonString) == false)
                            break;

                        processJsonData(jsonString);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    break;
                }
            }
            task.Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            sock.Close();
        }
    }

    static Task send(Socket sock)
    {
        var random = new Random();
        return Task.Factory.StartNew(() => {
            var recvList = new List<byte>();
            while (true)
            {
                try
                {
                    // {
                    //      "list" : [
                    //          {
                    //              "key" : "1",
                    //              "type : 0,
                    //              "value" : 50
                    //          },
                    //          {
                    //              "key" : "2",
                    //              "type : 1,
                    //              "value" : 1500
                    //          },
                    //      ]
                    //  }

                    var rootObject = new JObject();

                    var list = new JArray();

                    // temperature
                    var first = new JObject();
                    first["key"] = "1";
                    first["type"] = 0;    // 0 : temperature, 1 : fan speed
                    first["value"] = random.Next(0, 100);
                    list.Add(first);

                    // fan speed
                    var second = new JObject();
                    second["key"] = "2";
                    second["type"] = 1;
                    second["value"] = random.Next(1000, 2000);
                    list.Add(second);

                    rootObject["list"] = list;

                    Console.WriteLine("Send json ===========================");
                    printJson(rootObject.ToString());
                    Console.WriteLine("=====================================");

                    var sendBuffer = PluginPacket.getSendPacket(rootObject.ToString());
                    int sentBytes = 0;

                    while (sentBytes < sendBuffer.Length)
                    {
                        sentBytes += sock.Send(sendBuffer);
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    break;
                }
            }
        });
    }

    static void processJsonData(string jsonString)
    {
        Console.WriteLine("Recv json ===========================");
        printJson(jsonString);
        Console.WriteLine("=====================================");
    }

    static void printJson(string jsonString)
    {
        try
        {
            var stringReader = new StringReader(jsonString);
            var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            Console.WriteLine(stringWriter.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}
