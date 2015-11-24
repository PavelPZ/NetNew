using LMNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace LMComLib {
  /// <summary>
  /// Summary description for Emailer
  /// </summary>
  public class Emailer {

    public class Attachment {
      public string fileName;
      public byte[] content;
      public string contentType;

      public Attachment(string FileName, byte[] Content, string ContentType) {
        this.content = (byte[])Content.Clone();
        this.contentType = ContentType;
        this.fileName = FileName;
      }
    }

    public Emailer(string toEMails, string fromEMail, string subject, string body) {
      HTML = body;
      Subject = subject;
      From = fromEMail;
      foreach (string mail in toEMails.Split(new char[] { ';', ',' })) AddTo(mail);
    }

    public string HTML;
    public string From;
    public string Subject;
    HashSet<string> to = new HashSet<string>();
    List<Attachment> attachments = new List<Attachment>();

    public static string SendEMail(string toEMails, string fromEMail, string subject, string body, Emailer.Attachment att = null, string cc = null) {
      Emailer em = new Emailer(toEMails, fromEMail, subject, body);
      if (cc != null) foreach (string mail in cc.Split(new char[] { ';', ',' })) em.AddTo("+" + mail);
      if (att != null) em.AddAttachment(att);
      return em.sendMail();
    }

    void AddAttachment(Attachment att) {
      attachments.Add(att);
    }

    public void AddTo(string email) {
      to.Add(email);
    }
    
    public string sendMail() {
      MailMessage mailMsg = new MailMessage();
      foreach (string email in to) mailMsg.To.Add(new MailAddress(email, null));
      mailMsg.From = new MailAddress(From, null);

      // Subject and multipart/alternative Body
      mailMsg.Subject = Subject;
      mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(Html2Text(HTML), null, MediaTypeNames.Text.Plain));
      mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(HTML, null, MediaTypeNames.Text.Html));
      foreach (var att in attachments) {
        var ms = new MemoryStream(att.content); ms.Seek(0, SeekOrigin.Begin);
        mailMsg.Attachments.Add(new System.Net.Mail.Attachment(ms, att.fileName, att.contentType));
      }

      using (var sc = new SmtpClient(SmtpHost, SmtpPort)) {
        sc.EnableSsl = SmtpSsl;
        sc.Timeout = 10000;
        sc.DeliveryMethod = SmtpDeliveryMethod.Network;
        sc.UseDefaultCredentials = false;
        sc.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
        try {
          sc.Send(mailMsg);
          return null;
        } catch (Exception e){
          return LowUtils.ExceptionToString(e);
        }
      }
    }

    public bool SendMail() {
      return sendMail() == null;
    }
    static string SmtpHost = System.Configuration.ConfigurationManager.AppSettings["Email.SmtpHost"];
    static string SmtpUsername = System.Configuration.ConfigurationManager.AppSettings["Email.SmtpUsername"];
    static string SmtpPassword = System.Configuration.ConfigurationManager.AppSettings["Email.SmtpPassword"];
    static bool SmtpSsl = System.Configuration.ConfigurationManager.AppSettings["Email.SmtpSsl"] == "true";
    static bool StartTLS = System.Configuration.ConfigurationManager.AppSettings["Email.StartTLS"] == "true";
    static int SmtpPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Email.SmtpPort"] ?? "0");

    public string Html2Text(string source) {
      try {
        string result;
        // Remove HTML Development formatting
        // Replace line breaks with space
        // because browsers inserts space
        result = source.Replace("\r", " ");
        // Replace line breaks with space
        // because browsers inserts space
        result = result.Replace("\n", " ");
        // Remove step-formatting
        result = result.Replace("\t", string.Empty);
        // Remove repeating speces becuase browsers ignore them
        result = Regex.Replace(result,
                                                              @"( )+", " ");

        // Remove the header (prepare first by clearing attributes)
        result = Regex.Replace(result,
                 @"<( )*head([^>])*>", "<head>",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"(<( )*(/)( )*head( )*>)", "</head>",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 "(<head>).*(</head>)", string.Empty,
                 RegexOptions.IgnoreCase);

        // remove all scripts (prepare first by clearing attributes)
        result = Regex.Replace(result,
                 @"<( )*script([^>])*>", "<script>",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"(<( )*(/)( )*script( )*>)", "</script>",
                 RegexOptions.IgnoreCase);
        //result = Regex.Replace(result, 
        //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
        //         string.Empty, 
        //         RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"(<script>).*(</script>)", string.Empty,
                 RegexOptions.IgnoreCase);

        // remove all styles (prepare first by clearing attributes)
        result = Regex.Replace(result,
                 @"<( )*style([^>])*>", "<style>",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"(<( )*(/)( )*style( )*>)", "</style>",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 "(<style>).*(</style>)", string.Empty,
                 RegexOptions.IgnoreCase);

        // insert tabs in spaces of <td> tags
        result = Regex.Replace(result,
                 @"<( )*td([^>])*>", "\t",
                 RegexOptions.IgnoreCase);

        // insert line breaks in places of <BR> and <LI> tags
        result = Regex.Replace(result,
                 @"<( )*br( )*>", "\r",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"<( )*li( )*>", "\r",
                 RegexOptions.IgnoreCase);

        // insert line paragraphs (double line breaks) in place
        // if <P>, <DIV> and <TR> tags
        result = Regex.Replace(result,
                 @"<( )*div([^>])*>", "\r\r",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"<( )*tr([^>])*>", "\r\r",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"<( )*p([^>])*>", "\r\r",
                 RegexOptions.IgnoreCase);

        // Remove remaining tags like <a>, links, images,
        // comments etc - anything thats enclosed inside < >
        result = Regex.Replace(result,
                 @"<[^>]*>", string.Empty,
                 RegexOptions.IgnoreCase);

        // replace special characters:
        result = Regex.Replace(result,
                 @"&nbsp;", " ",
                 RegexOptions.IgnoreCase);

        result = Regex.Replace(result,
                 @"&bull;", " * ",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&lsaquo;", "<",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&rsaquo;", ">",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&trade;", "(tm)",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&frasl;", "/",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&lt;", "<",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&gt;", ">",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&copy;", "(c)",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 @"&reg;", "(r)",
                 RegexOptions.IgnoreCase);
        // Remove all others. More can be added, see
        // http://hotwired.lycos.com/webmonkey/reference/special_characters/
        result = Regex.Replace(result,
                 @"&(.{2,6});", string.Empty,
                 RegexOptions.IgnoreCase);

        // for testng
        //Regex.Replace(result, 
        //       this.txtRegex.Text,string.Empty, 
        //       RegexOptions.IgnoreCase);

        // make line breaking consistent
        result = result.Replace("\n", "\r");

        // Remove extra line breaks and tabs:
        // replace over 2 breaks with 2 and over 4 tabs with 4. 
        // Prepare first to remove any whitespaces inbetween
        // the escaped characters and remove redundant tabs inbetween linebreaks
        result = Regex.Replace(result,
                 "(\r)( )+(\r)", "\r\r",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 "(\t)( )+(\t)", "\t\t",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 "(\t)( )+(\r)", "\t\r",
                 RegexOptions.IgnoreCase);
        result = Regex.Replace(result,
                 "(\r)( )+(\t)", "\r\t",
                 RegexOptions.IgnoreCase);
        // Remove redundant tabs
        result = Regex.Replace(result,
                 "(\r)(\t)+(\r)", "\r\r",
                 RegexOptions.IgnoreCase);
        // Remove multible tabs followind a linebreak with just one tab
        result = Regex.Replace(result,
                 "(\r)(\t)+", "\r\t",
                 RegexOptions.IgnoreCase);
        // Initial replacement target string for linebreaks
        string breaks = "\r\r\r";
        // Initial replacement target string for tabs
        string tabs = "\t\t\t\t\t";
        for (int index = 0; index < result.Length; index++) {
          result = result.Replace(breaks, "\r\r");
          result = result.Replace(tabs, "\t\t\t\t");
          breaks = breaks + "\r";
          tabs = tabs + "\t";
        }
        // Thats it.
        return result;

      } catch {
        return source;
      }
    }
  }
}
