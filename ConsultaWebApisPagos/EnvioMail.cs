using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Configuration;
using System.Net.Configuration;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using System.Net.Mime;

namespace ConsultaWebApisPagos
{
    class EnvioMail
    {
        public static bool EnviaCorreo(string mailDestinatario, string ShortdId, string Origen, string Destino, string fechaSalida, string fechaLlegada, decimal Total)
        {
            mailDestinatario = mailDestinatario.ToLower();
            mailDestinatario.Replace(" ", "");
            MailMessage correo = new MailMessage();
            //correo.From = new MailAddress("dylan.zamorac@gmail.com", "DYLAN", System.Text.Encoding.UTF8);//Correo de salida
            correo.From = new MailAddress("notificaciones@sagautobuses.com", "Notificaciones (Correo automatico)", System.Text.Encoding.UTF8);//Correo de salida
            try
            {
                correo.To.Add(mailDestinatario); //Correo destino?
                correo.CC.Add("luis.rojas@transportesmedrano.com"); // Agregar una copia (CC)
                //correo.CC.Add("viridiana.fh@transportesmedrano.com"); // Agregar una copia (CC)
                correo.Subject = "Boletos SAG"; //Asunto
                                                //correo.Body = MensajeHtml(ShortdId,Origen,Destino,fechaSalida.ToString("dd/MM/yyyy HH:mm"),fechaLlegada.ToString("dd/MM/yyyy HH:mm"), Total);
                correo.AlternateViews.Add(MensajeHtml(ShortdId, Origen, Destino, fechaSalida, fechaLlegada, Total));
                correo.IsBodyHtml = true;
                correo.Priority = MailPriority.Normal;
                SmtpClient smtp = new SmtpClient();
                smtp.UseDefaultCredentials = false;
                smtp.Host = "mail.sagautobuses.com"; //Host del servidor de correo
                smtp.Port = 26; //Puerto de salida
                smtp.Credentials = new System.Net.NetworkCredential("notificaciones@sagautobuses.com", "Med*96850");//Cuenta de correo
                ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                smtp.EnableSsl = true;//True si el servidor de correo permite ssl
                try
                {
                    smtp.Send(correo);
                    Console.WriteLine("Exito se envio el correo a: " + mailDestinatario);
                }
                catch
                {
                    Console.WriteLine("Error en smtp");
                }
            }
            catch
            {
                Console.WriteLine("Error con el correo");
            }
            return true;
        }
        public static AlternateView MensajeHtml(string shorId, string Origen, string Destino, string fechaSalida, string fechaLLegada, decimal Total)
        {
            string msg = "";

            msg = "<!DOCTYPE html> <html><head><title></title><meta http - equiv = \"Content-Type\" content = \"text/html; charset=utf-8\" /> " +
     "<meta name = \"viewport\" content = \"width=device-width, initial-scale=1\"><meta http - equiv = \"X-UA-Compatible\" content = \"IE=edge\" /><style type = \"text/css\"> " +
              /* CLIENT-SPECIFIC STYLES */
              "body, table, td, a { -webkit - text - size - adjust: 100 %; -ms - text - size - adjust: 100 %; } " +
            "table, td { mso - table - lspace: 0pt; mso - table - rspace: 0pt; } " +
            "img { -ms - interpolation - mode: bicubic; } " +
            /* RESET STYLES */
            "img { border: 0; height: auto; line - height: 100 %; outline: none; text - decoration: none; } " +
            "table { border - collapse: collapse!important; } " +
            "body { height: 100 % !important; margin: 0!important; padding: 0!important; width: 100 % !important; } " +
            /* iOS BLUE LINKS */
            "a[x - apple - data - detectors] { " +
            "color: inherit!important; " +
            "    text - decoration: none!important; " +
            "    font - size: inherit!important; " +
            "    font - family: inherit!important; " +
            "    font - weight: inherit!important; " +
            "    line - height: inherit!important; " +
            "} " +
            /* MEDIA QUERIES */
            "@media screen and(max - width: 480px) { " +
    ".mobile - hide { " +
    "            display: none!important; " +
    "            } " +
    ".mobile - center { " +
    "                text - align: center!important; " +
    "            } " +
    "        } " +

            /* ANDROID CENTER FIX */
    "        div[style *= \"margin: 16px 0; \"] { margin: 0!important; } " +
" </style></head><body style = \"margin: 0 !important; padding: 0 !important; background-color: #eeeeee;\" bgcolor = \"#eeeeee\"> " +
   "<!--HIDDEN PREHEADER TEXT --> " +
   " <div style = \"display: none; font-size: 1px; color: #fefefe; line-height: 1px; font-family: Open Sans, Helvetica, Arial, sans-serif; max-height: 0px; max-width: 0px; opacity: 0; overflow: hidden;\"> " +
    "Gracias por tu compra, ya estamos casi listos para NUESTRO VIAJE</div> " +
    "<table border = \"0\" cellpadding = \"0\" cellspacing = \"0\" width = \"100%\"><tr><td align = \"center\" style = \"background-color: #eeeeee;\" bgcolor = \"#eeeeee\"> " +
       "<table align = \"center\" border = \"0\" cellpadding = \"0\" cellspacing = \"0\" width = \"100%\" style = \"max-width:600px;\"> " +
       "<tr><td align = \"center\" valign = \"top\" style = \"font-size:0; padding: 35px;\" bgcolor = \"#044767\"> " +
       "<div style = \"display:inline-block; max-width:50%; min-width:100px; vertical-align:top; width:100%;\"> " +
       "<table align = \"left\" border = \"0\" cellpadding = \"0\" cellspacing = \"0\" width = \"100%\" style = \"max-width:300px;\"> " +
      "<tr><td align = \"left\" valign = \"top\" style = \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 36px; font-weight: 800; line-height: 48px;\" class=\"mobile-center\"> " +
      "<h1 style = \"font-size: 36px; font-weight: 800; margin: 0; color: #ffffff;\">SAG</h1></td> " +
      "</tr></table></div><div style=\"display:inline-block; max-width:50%; min-width:100px; vertical-align:top; width:100%;\" class=\"mobile-hide\"> " +
      "<table align = \"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"max-width:300px;\"><tr> " +
      "<td align = \"right\" valign=\"top\" style=\"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 48px; font-weight: 400; line-height: 48px;\"> " +
      "<table cellspacing = \"0\" cellpadding=\"0\" border=\"0\" align=\"right\"> " +
      "<tr><td style = \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 18px; font-weight: 400;\"> " +
      "<p style=\"font-size: 18px; font-weight: 400; margin: 0; color: #ffffff;\"><a href = \"https://sagautobuses.com/\" target=\"_blank\" style=\"color: #ffffff; text-decoration: none;\">Comprar &nbsp;</a></p> " +
      "</td><td style = \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 18px; font-weight: 400; line-height: 24px;\"> " +
      "<a href=\"https://sagautobuses.com/\" target=\"_blank\" style=\"color: #ffffff; text-decoration: none;\"><img src = \"https://i.ibb.co/C9SJz95/shop.png\" width=\"27\" height=\"23\" style=\"display: block; border: 0px;\"/></a> " +
      "</td></tr></table></td></tr></table></div></td></tr><tr><td align = \"center\" style=\"padding: 35px 35px 20px 35px; background-color: #ffffff;\" bgcolor=\"#ffffff\"> " +
      "<table align = \"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"max-width:600px;\"><tr> " +
      "<td align = \"center\" style=\"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px; padding-top: 25px;\"> " +
      "<img src = \"https://i.ibb.co/vQZcFdF/hero-image-receipt.png\" 'cid:hero-image-receipt' width=\"125\" height=\"120\" style=\"display: block; border: 0px;\" /><br> " +
      "<h2 style = \"font-size: 30px; font-weight: 800; line-height: 36px; color: #333333; margin: 0;\">¡Vamonos de Viaje!</h2></td></tr><tr> " +
      "<td align = \"left\" style=\"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px; padding-top: 10px;\"> " +
      "<p style = \"font-size: 16px; font-weight: 400; line-height: 24px; color: #777777;\">Este es el comprobante de su pago, ya estamos listos para que viajemos.</p></td></tr><tr> " +
      "<td align = \"left\" style=\"padding-top: 20px;\"><table cellspacing = \"0\" cellpadding=\"0\" border=\"0\" width=\"100%\"><tr> " +
      "<td width = \"75%\" align=\"left\" bgcolor=\"#eeeeee\" style=\"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 800; line-height: 24px; padding: 10px;\"> " +
      "Orden de Confirmación #</td> " +
      "<td width = \"25%\" align=\"left\" bgcolor=\"#eeeeee\" style=\"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 800; line-height: 24px; padding: 10px;\"> " +
      "" + shorId + "</td></tr><tr> " +
      "<td width = \"75%\" align=\"left\" style=\"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px; padding: 15px 10px 5px 10px;\"> " +
      "Viaje con SAG Autobuses</td> " +
      "<td width = \"25%\" align= \"left\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px; padding: 15px 10px 5px 10px;\"> " +
      "$" + Total.ToString() + " </td></tr></table></td></tr><tr><td align = \"left\" style= \"padding-top: 20px;\"><table cellspacing = \"0\" cellpadding= \"0\" border= \"0\" width= \"100%\"><tr> " +
      "<td width = \"75%\" align= \"left\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 800; line-height: 24px; padding: 10px; border-top: 3px solid #eeeeee; border-bottom: 3px solid #eeeeee;\"> " +
      "TOTAL </td> " +
      "<td width = \"25%\" align= \"left\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 800; line-height: 24px; padding: 10px; border-top: 3px solid #eeeeee; border-bottom: 3px solid #eeeeee;\"> " +
      "$" + Total.ToString() + "</td></tr></table></td></tr></table></td></tr><tr> " +
      "<td align = \"center\" height= \"100%\" valign= \"top\" width= \"100%\" style= \"padding: 0 35px 35px 35px; background-color: #ffffff;\" bgcolor= \"#ffffff\"> " +
      "<table align = \"center\" border= \"0\" cellpadding= \"0\" cellspacing= \"0\" width= \"100%\" style= \"max-width:660px;\"> " +
      "<tr><td align = \"center\" valign= \"top\" style= \"font-size:0;\"><div style = \"display:inline-block; max-width:50%; min-width:240px; vertical-align:top; width:100%;\"> " +
      "<table align = \"left\" border= \"0\" cellpadding= \"0\" cellspacing= \"0\" width= \"100%\" style= \"max-width:300px;\"><tr> " +
      "<td align = \"left\" valign= \"top\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px;\"> " +
      "<p style = \"font-weight: 800;\"> Viaje </p><p> De " + Origen + "<br> A " + Destino + "<br></p></td></tr></table></div> " +
      "<div style = \"display:inline-block; max-width:50%; min-width:240px; vertical-align:top; width:100%;\"> " +
      "<table align = \"left\" border= \"0\" cellpadding= \"0\" cellspacing= \"0\" width= \"100%\" style= \"max-width:300px;\"><tr> " +
      "<td align = \"left\" valign= \"top\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px;\"> " +
      "<p style = \"font-weight: 800;\"> Horario </p><p> Salida: " + fechaSalida + " <br> Llegada: " + fechaLLegada + " </p></td></tr></table></div></td></tr></table> " +
      "</td></tr><tr><td align = \"center\" style= \" padding: 35px; background-color: #1b9ba3;\" bgcolor= \"#1b9ba3\"> " +
      "<table align = \"center\" border= \"0\" cellpadding= \"0\" cellspacing= \"0\" width= \"100%\" style= \"max-width:600px;\"><tr> " +
      "<td align = \"center\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 400; line-height: 24px; padding-top: 25px;\"> " +
      "<h2 style = \"font-size: 24px; font-weight: 800; line-height: 30px; color: #ffffff; margin: 0;\">DESCARGA TUS BOLETOS</h2></td></tr><tr> " +
      "<td align = \"center\" style= \"padding: 25px 0 15px 0;\"><table border = \"0\" cellspacing= \"0\" cellpadding= \"0\"><tr> " +
      "<td align = \"center\" style= \"border-radius: 5px;\" bgcolor= \"#66b3b7\"> " +
      "<a href = \"https://sagautobuses.com/Boletos?Boleto=" + shorId + "&version=0\" target= \"_blank\" style= \"font-size: 18px; font-family: Open Sans, Helvetica, Arial, sans-serif; color: #ffffff; text-decoration: none; border-radius: 5px; background-color: #66b3b7; padding: 15px 30px; border: 1px solid #66b3b7; display: block;\"> Nuestros Boletos</a> " +
      "</td></tr></table></td></tr></table></td></tr><tr><td align = \"center\" style= \"padding: 35px; background-color: #ffffff;\" bgcolor= \"#ffffff\"> " +
      "<table align = \"center\" border= \"0\" cellpadding= \"0\" cellspacing= \"0\" width= \"100%\" style= \"max-width:600px;\"><tr><td align = \"center\"> " +
      "<a href = \"https://sagautobuses.com/\" target= \"_blank\" style= \"color: #ffffff; text-decoration: none;\"><img src= \"https://i.ibb.co/KqfxXcx/logo-footer.png\" width= \"120\" height= \"70\" style= \"display: block; border: 0px;\" /></a> " +
      "</td></tr><tr><td align = \"center\" style= \"font-family: Open Sans, Helvetica, Arial, sans-serif; font-size: 14px; font-weight: 400; line-height: 24px; padding: 5px 0 10px 0;\"> " +
      "<p style = \"font-size: 14px; font-weight: 800; line-height: 18px; color: #333333;\">Av.Politécnico Nacional 4912 Col.Maximino<br>Avila Camacho(21, 57 km) <br>07380 Ciudad de México, México " +
      "</p></td></tr></table></td></tr></table></td></tr></table><table border = \"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"> " +
      "<tr><td bgcolor = \"#ffffff\" align=\"center\"></td></tr></table> " +
      "<!--END LITMUS ATTRIBUTION --> " +
      "</body></html>";
            AlternateView htmlView =
            AlternateView.CreateAlternateViewFromString(msg,
                           Encoding.UTF8,
                           MediaTypeNames.Text.Html);
            return htmlView;
        }
    }
}
