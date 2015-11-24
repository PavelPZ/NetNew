using System.Collections;
using System.Configuration;
using System.IO;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;

namespace LMComLib {

  //http://thoughtfulcode.wordpress.com/2010/09/29/embedding-arbitrary-language-glyphs-in-pdf-with-itextsharp/
  public static class PdfGenerator {

    static BaseFont font;
    static BaseFont fontBold;
    static AsymmetricKeyParameter akp; static Org.BouncyCastle.X509.X509Certificate[] chain;

    public static byte[] createPdf(string template, GeneratePdfItem[] fields) {
      return createPdf(template, (flds, fill) => {
        foreach (GeneratePdfItem fld in fields) fill(flds, fld.Name, fld.Value, fld.FontSize, fld.IsBold);
      });
    }

    public static byte[] createPdf(string template, Action<AcroFields, Action<AcroFields, string, string, int, bool>> setFields) {
      if (font == null)
        lock (typeof(PdfGenerator))
          if (font == null) {
            var path = string.Format(ConfigurationManager.AppSettings["PDF.Font"], Machines.rootPath);
            font = BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            path = string.Format(ConfigurationManager.AppSettings["PDF.FontBold"], Machines.rootPath);
            fontBold = BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            path = string.Format(ConfigurationManager.AppSettings["PDF.Cert"], Machines.rootPath);
            Cert(path, ConfigurationManager.AppSettings["PDF.Cert.Password"], out akp, out chain);
          }
      //PdfReader rdr = new PdfReader(new RandomAccessFileOrArray(template), null);
      PdfReader rdr = new PdfReader(template, null);
      MemoryStream res = new MemoryStream();
      try {
        PdfStamper ps = new PdfStamper(rdr, res);
        try {
          //Dosazeni poli
          AcroFields af = ps.AcroFields;
          setFields(af, setPdfField);
          ps.FormFlattening = true;
          //Priprava na elektronicky podpis
        } finally { ps.Close(); }
      } finally { rdr.Close(); }
      //PdfReader certRdr = new PdfReader(res.ToArray());
      //MemoryStream certRes = new MemoryStream();
      //try {
      //  PdfStamper certPs = PdfStamper.CreateSignature(certRdr, certRes, '\0');
      //  try {
      //    PdfSignatureAppearance sap = certPs.SignatureAppearance;
      //    //LM16
      //    //sap.SetCrypto(akp, chain, null, PdfSignatureAppearance.WINCER_SIGNED);
      //    sap.Reason = "Certify";
      //    sap.Contact = "LANGMaster";
      //    sap.Location = "EU";
      //    certRes.Seek(0, SeekOrigin.Begin);
      //  } finally {
      //    certPs.Close();
      //  }
      //} finally { certRdr.Close(); }
      //return certRes.ToArray();
      return res.ToArray();
    }

    static void Cert(string path, string password, out AsymmetricKeyParameter akp, out Org.BouncyCastle.X509.X509Certificate[] chain) {
      string alias = null;
      Pkcs12Store pk12;
      //First we'll read the certificate file
      pk12 = new Pkcs12Store(new FileStream(path, FileMode.Open, FileAccess.Read), password.ToCharArray());

      //then Iterate throught certificate entries to find the private key entry
      IEnumerator i = pk12.Aliases.GetEnumerator();
      while (i.MoveNext()) {
        alias = ((string)i.Current);
        if (pk12.IsKeyEntry(alias)) break;
      }

      akp = pk12.GetKey(alias).Key;
      X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
      chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
      for (int k = 0; k < ce.Length; ++k)
        chain[k] = ce[k].Certificate;
    }

    static void setPdfField(AcroFields af, string name, string value, int size, bool bold) {
      name = af.GetTranslatedFieldName(name);
      af.SetFieldProperty(name, "textsize", (float)(size == 0 ? 10 : size), null);
      af.SetFieldProperty(name, "textfont", bold ? fontBold : font, null);
      af.SetField(name, value);
    }

  }
}

