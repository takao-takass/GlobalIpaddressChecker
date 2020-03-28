using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Net.Http;
using System.Collections.Generic;
using AngleSharp.Html.Parser;
using System.Linq;
using AngleSharp;

namespace GlobalIpaddressChecker
{
    class Program
    {
        static void Main(string[] args)
        {

            // IPアドレスをチェックし、前回と同じ場合は処理を終了します
            var ipaddress = GetGlobalIpAddress();
            if (ipaddress.Equals(GetPreviousIpAddress()))
            {
                return;
            }

            // SSL証明書の使用を問題なしと判定します
            ServicePointManager.ServerCertificateValidationCallback =
              new RemoteCertificateValidationCallback(
                Program.OnRemoteCertificateValidationCallback);
            
            // メールサーバにSMTPで接続してメール送信します
            using (var smtp = new System.Net.Mail.SmtpClient())
            {
                // SMTP設定
                smtp.EnableSsl = true;
                smtp.Host = Properties.Resources.MailServerHost;
                smtp.Port = Int32.Parse(Properties.Resources.MailServerPort);
                smtp.Credentials = new System.Net.NetworkCredential(
                    Properties.Resources.LoginMailUser, Properties.Resources.LoginMailPassword);

                //送信メッセージ作成します
                var omsg = new System.Net.Mail.MailMessage()
                {
                    Subject = Properties.Resources.SubjectText,
                    Body = Properties.Resources.PlaceName +  "のIPアドレスが変更されました。\r\n新しいIPアドレス： " + ipaddress,
                    From = new MailAddress(Properties.Resources.LoginMailUser, Properties.Resources.FromDispName)
                };

                // 送信
                omsg.To.Add(Properties.Resources.SendToAddress);
                smtp.Send(omsg);
            }

            // 今回取得したIPアドレスを保存します
            WriteGlobalIpAddress(ipaddress);

        }

        /// <summary>
        /// ルータのステータスページにアクセスしてグローバルIPアドレスを取得します
        /// </summary>
        /// <returns>グローバルIPアドレス</returns>
        static string GetGlobalIpAddress()
        {

            // HTTPリクエストを作成します
            var request = WebRequest.Create(Properties.Resources.RouterUrl);
            request.Credentials = new System.Net.NetworkCredential(
                Properties.Resources.RouterUser,
                Properties.Resources.RouterPassword
            );

            // レスポンスを受け取り、IPアドレスを取り出します
            var ipaddress = String.Empty;
            using (var stream = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                var doc = (new HtmlParser()).ParseDocument(stream.ReadLine());
                ipaddress = doc.QuerySelector(Properties.Resources.DomQuery).InnerHtml;
            }

            return ipaddress;
        }

        /// <summary>
        /// DDNSを更新します
        /// </summary>
        /// <param name="ipaddress">新しいIPアドレス</param>
        static void UpdateDdns(string ipaddress)
        {
            // DynDNSにログインしてトークンを取得する

            // DDNSのIPアドレスを変更する

            // 変更を公開する

            // DynDNSからログアウトする

        }

        /// <summary>
        /// IPアドレスをファイルに書き込みます
        /// </summary>
        /// <param name="ipaddress">IPアドレス</param>
        static void WriteGlobalIpAddress(string ipaddress)
        {
            // アクセストークンとシークレットをストレージに保存します
            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "UTF-8", null));
            var root = xml.CreateElement("root");
            xml.AppendChild(root);

            // 書き込みアプリバージョン
            var elementAppver = xml.CreateElement("element");
            elementAppver.InnerText = ipaddress;
            elementAppver.SetAttribute("data", "ipaddress");
            root.AppendChild(elementAppver);

            // 既存ファイルを削除して新規保存します
            try
            {
                System.IO.File.Delete("app.data");
                xml.Save("app.data");
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// ファイルから前回のIPアドレスを取得します
        /// </summary>
        /// <returns>前回のIPアドレス</returns>
        static string GetPreviousIpAddress()
        {

            string ipaddress = String.Empty;

            // アプリデータを読み込みます
            var xml = new XmlDocument();
            try
            {
                xml.Load(System.AppDomain.CurrentDomain.BaseDirectory + "app.data");
            }
            catch (Exception e)
            {
                return ipaddress;
            }

            // アクセスキーとシークレットを取得します
            foreach (XmlElement element in xml.DocumentElement)
            {
                switch (element.GetAttribute("data"))
                {
                    case "ipaddress":
                        ipaddress = element.InnerText;
                        break;
                    default:
                        break;
                }
            }

            return ipaddress;
        }

        /// <summary>
        /// SSL証明書の使用を問題なしと判定します
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool OnRemoteCertificateValidationCallback(
          Object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
