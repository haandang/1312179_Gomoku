using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = IO.Socket("ws://gomoku-lajosveres.rhcloud.com:8000");
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Console.WriteLine("connected");

            });
            socket.On(Socket.EVENT_MESSAGE, (data) =>
            {
                Console.WriteLine(data);
            });
            socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
            {
                Console.WriteLine(data);
            });
            socket.On("ChatMessage", (data) =>
            {
                Console.WriteLine(data);
                if (((Newtonsoft.Json.Linq.JObject) data)["message"].ToString() == "Welcome!")
                {
                    socket.Emit("MyNameIs", "dotNetConsole");
                    socket.Emit("ConnectToOtherPlayer");
                    
                    //Console.ReadKey(intercept: true);
                    
                }
                
            });
            socket.On(Socket.EVENT_ERROR, (data) =>
            {
                Console.WriteLine(data);
            });
            socket.On("NextStepIs", (data) =>
            {
                Console.WriteLine("NextStepIs: " + data);
            });
            //socket.Connect();
            Console.WriteLine("Enter to begin");
            Console.ReadLine();
            Console.WriteLine("Enter to make your move");
            socket.Emit("MyStepIs", JObject.FromObject(new {row = 5, col = 5}));
            //socket.Emit("MyStepIs", "{ row = 5, col = 5 }");
            Console.ReadLine();
        }
    }
}
