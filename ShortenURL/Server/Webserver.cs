using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net;

namespace ShortenURL
{
    /// <summary>
    /// Created by neonkid 12/01/17
    /// </summary>
    public class WebServer
    {
        // 웹 서버로 사용할 소켓입니다.
        private Socket serverSocket;
        private string home_dir;

        /// <summary>
        /// 웹 서버에서 기본적으로 사용할 포트번호 지정
        /// Listener 등과 같은 Polling 방식을 사용하지 않고, AsyncEventArgs로, Callback 처리합니다.
        /// </summary>
        public WebServer(string directory)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 80);    // 80번 포트를 사용합니다.
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);    // 최대 10명까지 받습니다.

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new System.EventHandler<SocketAsyncEventArgs>(accept_Completed);
            serverSocket.AcceptAsync(args);

            home_dir = directory;
        }

        /// <summary>
        /// 클라이언트와 연결되어을 경우 발생하는 Callback 메소드입니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket client = e.AcceptSocket; // 클라이언트 소켓
            Socket server = (Socket)sender; // 서버 소켓

            if (client != null)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                byte[] szBuffer = new byte[4096];
                args.SetBuffer(szBuffer, 0, szBuffer.Length);
                args.UserToken = client;    // 토큰은 client Socket으로 설정..
                args.Completed += new System.EventHandler<SocketAsyncEventArgs>(receive_Completed);
                client.ReceiveAsync(args);
            }
            e.AcceptSocket = null;
            server.AcceptAsync(e);
        }

        /// <summary>
        /// 클라이언트에게 HEADER / DATA 값을 전송할 때 발생하는 Callback 메소드입니다.
        /// 두 가지 receive 방법을 제공합니다. 
        /// 
        /// 첫 번쨰는 HTML 소스를 전송.
        /// 두 번쨰는 URL Redirection..
        /// 
        /// DBMS에서 Shortcut URL이 존재하지 않을 경우, FileRead 메소드 호출
        /// 존재하면, URL Redirection을 하도록 구현하였습니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            byte[] pBuffer = e.Buffer;
            string sHttpText = Encoding.UTF8.GetString(pBuffer);    // URL 주소
            sHttpText = sHttpText.Trim().Replace("\0", "");

            int nPos = sHttpText.IndexOf(" ") + 1;
            int nEPos = sHttpText.IndexOf(" ", nPos);

            if (nEPos < 0 || nPos < 0)
                return;

            string fileName = sHttpText.Substring(nPos, nEPos - nPos);  // URL 주소 뒷편의 파일 이름
            nPos = sHttpText.IndexOf("Accept", nPos);
            nEPos = sHttpText.IndexOf("\n", nPos);
            string acceptType = sHttpText.Substring(nPos, nEPos - nPos);

            // HTTP HEADER 정보
            StringBuilder builder = new StringBuilder();

            // HTTP DATA 정보
            byte[] data = null;

            try
            {
                builder.Append("HTTP/1.1 200 OK\r\n");
                builder.Append("Content-Type: text/html, charset=utf-8\r\n");
                builder.Append("Server: NEONKID ShortcutURL Server \r\n");

                data = IDRead(fileName, ref builder);

                if (data == null)
                    data = FileRead(fileName, ref builder);

                string header = builder.ToString();
                pBuffer = Encoding.Default.GetBytes(header);
            }
            catch (FileNotFoundException)
            {
                builder.Append("HTTP/1.1 100 Bad Request OK\r\n");
                builder.Append("Content-Type: text/html, charset=utf-8\r\n");
                builder.Append("Server: NEONKID Redirect Server \r\n");

                string header = builder.ToString();
                pBuffer = Encoding.Default.GetBytes(header);
            }
            finally
            {
                Socket client = (Socket)sender;
                client.Send(pBuffer);   // HEADER 전송..

                if (data != null && data.Length > 1)
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.SetBuffer(data, 0, data.Length);
                    args.Completed += new System.EventHandler<SocketAsyncEventArgs>(send_Completed);
                    client.SendAsync(args); // DATA 전송..
                }
            }
        }

        /// <summary>
        /// 인코딩된 주소 값을 읽어, 해당 URL로 리다이렉팅 하는 
        /// 바이트 코드를 반환합니다.
        /// </summary>
        /// <param name="ID">인코딩된 주소</param>
        /// <param name="builder">HEADER 빌더</param>
        /// <returns></returns>
        private byte[] IDRead(string ID, ref StringBuilder builder)
        {
            DBHelper helper = new DBHelper(@"127.0.0.1", @"ShortenURL_DB", @"url_Table", @"userid", @"password");
            Base62.Base62Converter converter = new Base62.Base62Converter(Base62.Base62Converter.CharacterSet.DEFAULT);

            string decodeNo = converter.Decode(ID.Replace("/", ""));
            string originalURL = helper.getURLforID(decodeNo);

            if (originalURL == null)
                return null;

            string redirectStr = "<meta http-equiv=\"refresh\" content=\'0;url=" + originalURL + "'/>";

            byte[] pBuffer = new byte[redirectStr.Length];
            pBuffer = Encoding.UTF8.GetBytes(redirectStr);

            builder.AppendFormat("Content-Length: {0}\r\n", pBuffer.Length);
            builder.Append("\r\n");

            return pBuffer;
        }

        /// <summary>
        /// HTML 파일을 읽어, byte 코드로 반환합니다.
        /// </summary>
        /// <param name="fileName">사용자가 입력한 파일 이름</param>
        /// <param name="builder">HEADER 빌더</param>
        /// <returns></returns>
        private byte[] FileRead(string fileName, ref StringBuilder builder)
        {
            fileName.Replace("/", "\\");    // 윈도우 경로 방식으로 처리..
            if (fileName.CompareTo("/") == 0)
                fileName += "index.html";

            try
            {
                FileStream fs = new FileStream(home_dir + fileName, FileMode.Open, FileAccess.Read);
                byte[] pBuffer = new byte[fs.Length];
                fs.Read(pBuffer, 0, pBuffer.Length);    // 소스 코드 읽기...
                fs.Close();

                builder.AppendFormat("Content-Length: {0}\r\n", pBuffer.Length);
                builder.Append("\r\n");

                return pBuffer;
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException();
            }
        }

        /// <summary>
        /// 사용자에게 전송이 완료된 경우 발생하는 Callback 메소드입니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void send_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket client = (Socket)sender;
            client.Disconnect(true);
            client.Close();
        }
    }
}
