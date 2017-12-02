using System.Text;
using System.Windows;
using System.Net;
using System.IO;

namespace ShortenURL
{
    /// <summary>
    /// Created by neonkid 12/01/17
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string urlTitle = "localhost";

        public MainWindow()
        {
            InitializeComponent();
            new WebServer(@"../../WEB_DIR");
        }

        /// <summary>
        /// 원본 URL 값을 받아,
        /// 짧은 URL로 변경해줍니다.
        /// 
        /// URL 입력시에 다음과 같은 검사를 진행합니다.
        /// 1. http URL 인지 아닌지 검사. (URL 앞에 http 혹은 https 가 있어야 함)
        /// 2. 유효한 페이지인지 검사 (응답이 없거나 유효하지 않은 응답인 경우, 바로 종료)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void translateBtn_Click(object sender, RoutedEventArgs e)
        {
            Transform_URL.Text = "";

            if (Original_URL.Text == null)
                return;

            if (!Original_URL.Text.StartsWith("http"))
            {
                MessageBox.Show("올바른 URL 형식이 아닙니다.. !", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                WebRequest wReq = WebRequest.Create(Original_URL.Text);
                WebResponse wResp = wReq.GetResponse();
                Stream respStream = wResp.GetResponseStream();
                setLog("[SUCCESS] 유효한 URL 입니다: " + Original_URL.Text);
            }
            catch (System.Exception ex)
            {
                setLog("[FAIL] 유효하지 않는 URL 입니다: " + ex.Message);
                return;
            }

            Base62.Base62Converter converter = new Base62.Base62Converter(Base62.Base62Converter.CharacterSet.DEFAULT);
            StringBuilder finalURL = new StringBuilder();

            // MySQL 을 사용해야 합니다.
            DBHelper helper = new DBHelper(@"127.0.0.1", @"ShortenURL_DB", @"url_Table", @"userid", @"password");

            // DB 에 해당 URL이 존재하지 않을 경우에만 생성..
            if (helper.getURL(Original_URL.Text) == null)
            {
                helper.Insert(Original_URL.Text);
                setLog("[INSERT] URL 생성 완료: " + Original_URL.Text);
            }
            else
                setLog("[NOTICE] 해당 URL의 DB가 이미 존재합니다: " + Original_URL.Text);

            finalURL.Append("http://" + urlTitle + "/");
            finalURL.Append(converter.Encode(helper.getIDforURL(Original_URL.Text)));

            Transform_URL.Text = finalURL.ToString();
        }

        private void setLog(string msg)
        {
            logBox.AppendText(msg + "\n");
        }
    }
}
