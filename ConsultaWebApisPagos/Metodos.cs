using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.qrcode;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ConsultaWebApisPagos.Metodos;
using static iTextSharp.text.pdf.AcroFields;

namespace ConsultaWebApisPagos
{
    class Metodos
    {
        public static string GetTipoCambio()
        {
            var requestToken = (HttpWebRequest)WebRequest.Create("https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno?token=ec27c3943c4b5364d99bffa92102e028c8c1421b996d438c669f1597067b685e");

            requestToken.Method = "GET";
            using (WebResponse responseToken = requestToken.GetResponse())
            {
                using (Stream strReader = responseToken.GetResponseStream())
                {
                    if (strReader == null) return "";
                    using (StreamReader objReader = new StreamReader(strReader))
                    {
                        string responseBody = objReader.ReadToEnd();

                        //accesos = JsonConvert.DeserializeObject<Token>(responseBody);
                        // Do something with responseBody
                        Console.WriteLine(responseBody);
                    }
                }
            }
            return "";
        }
        public static string SetPedidos()
        {
            DataTable TblCredenciales = new DataTable();
            DataTable TblPagosPendientes = new DataTable();
            DataTable TblTicketId = new DataTable();
            DataTable TblProveedor = new DataTable();
            Token accesos = new Token();
            Ordenes orden = new Ordenes();
            string sqry;
            string[] separatingStrings = { "\"status\":\"", "\",\"amount\":" };
            try
            {
                sqry = string.Format("ExtraeCredenciales {0}", "1");
                TblCredenciales = ExtraeDatos(sqry);
                if (TblCredenciales.Rows.Count > 0)
                {
                    var authData = string.Format("{0}:{1}", TblCredenciales.Rows[0][0].ToString(), TblCredenciales.Rows[0][1].ToString());
                    var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

                    var urlToken = TblCredenciales.Rows[0][3].ToString(); //https:\\putosilolees.com\
                    var requestToken = (HttpWebRequest)WebRequest.Create(urlToken);
                    requestToken.Headers.Add("Authorization", "Basic " + authHeaderValue);
                    requestToken.Method = "POST";
                    //requestToken.ContentType = "application/json";
                    //requestToken.Accept = "application/json";


                    using (WebResponse responseToken = requestToken.GetResponse())
                    {
                        using (Stream strReader = responseToken.GetResponseStream())
                        {
                            if (strReader == null) return "";
                            using (StreamReader objReader = new StreamReader(strReader))
                            {
                                string responseBody = objReader.ReadToEnd();

                                accesos = JsonConvert.DeserializeObject<Token>(responseBody);
                                // Do something with responseBody
                                Console.WriteLine(responseBody);
                            }
                        }
                    }
                }
                sqry = String.Format("ExtraePagosPend {0}", "1");
                TblPagosPendientes = ExtraeDatos(sqry);

                for (int i = 0; i < TblPagosPendientes.Rows.Count; i++)
                {

                    var url = TblCredenciales.Rows[0][4].ToString() + TblPagosPendientes.Rows[i][0].ToString();
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    //request.Headers.Add("Authorization", "Bearer " + accesos.access_token);
                    //request.ContentType = "application/json";
                    //request.Accept = "application/json";
                    string[] str;
                    orden = new Ordenes();
                    using (WebResponse response = request.GetResponse())
                    {
                        using (Stream strReader = response.GetResponseStream())
                        {
                            if (strReader == null) return "";
                            using (StreamReader objReader = new StreamReader(strReader))
                            {
                                string responseBody = objReader.ReadToEnd();
                                // Do something with responseBody
                                str = responseBody.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                                // Do something with responseBody
                                Console.WriteLine(responseBody);
                            }
                        }
                    }
                    if (str.Length == 6)
                    {
                        if (str[3].ToUpper() == "COMPLETED")
                        {
                            sqry = string.Format("ActualizaEstadoPago {0},'{1}'", "1", TblPagosPendientes.Rows[i][0].ToString());
                            TblTicketId = ExtraeDatos(sqry);
                            crearBoletosPdf(TblTicketId);

                            Console.WriteLine("Exitoso");
                            return "Exitoso";
                        }
                        else
                        {
                            Console.WriteLine("Pendiente");
                            return "Pendiente";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error " + ex.Message);
                return "";
            }
            return "";
        }

        public static string setPedidosOpenPay()
        {
            DataTable TblCredenciales = new DataTable();
            DataTable TblPagosPendientes = new DataTable();
            DataTable TblTicketId = new DataTable();
            DataTable TblProveedor = new DataTable();
            openpay respuestas = new openpay();

            string sqry;
            string[] separatingStrings = { "\"status\":\"", "\",\"amount\":" };

            sqry = String.Format("ExtraePagosPend {0}", "2");
            TblPagosPendientes = ExtraeDatos(sqry);
            sqry = string.Format("ExtraeCredenciales {0}", "2");
            TblCredenciales = ExtraeDatos(sqry);
            for (int i = 0; i < TblPagosPendientes.Rows.Count; i++)
            {

                if (TblCredenciales.Rows.Count > 0)
                {
                    var authData = string.Format("{0}:{1}", TblCredenciales.Rows[0][1].ToString(), TblCredenciales.Rows[0][5].ToString());
                    var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

                    var urlConsulta = TblCredenciales.Rows[0][4].ToString() + TblPagosPendientes.Rows[i][0].ToString();
                    var requestToken = (HttpWebRequest)WebRequest.Create(urlConsulta);
                    requestToken.Headers.Add("Authorization", "Basic " + authHeaderValue);
                    requestToken.Method = "GET";

                    using (WebResponse responseToken = requestToken.GetResponse())
                    {
                        using (Stream strReader = responseToken.GetResponseStream())
                        {
                            if (strReader == null) return "";
                            using (StreamReader objReader = new StreamReader(strReader))
                            {
                                string responseBody = objReader.ReadToEnd();

                                respuestas = JsonConvert.DeserializeObject<openpay>(responseBody);
                                // Do something with responseBody
                                Console.WriteLine(responseBody);

                            }
                        }
                    }

                    if (respuestas.status.ToUpper() == "COMPLETED")
                    {
                        sqry = string.Format("ActualizaEstadoPago {0},'{1}'", "2", TblPagosPendientes.Rows[i][0].ToString());
                        TblTicketId = ExtraeDatos(sqry);
                        crearBoletosPdf(TblTicketId);

                        Console.WriteLine("Exitoso");
                        //return "Exitoso";
                    }
                    if (respuestas.status.ToUpper() == "CANCELLED")
                    {
                        sqry = string.Format("CancelaEstadoPago {0},'{1}'", "2", TblPagosPendientes.Rows[i][0].ToString());
                        TblTicketId = ExtraeDatos(sqry);
                        Console.WriteLine("Pendiente");
                        //return "Pendiente";
                    }

                }
            }

            return "";
        }

        public static string crearBoletosPdf(DataTable TblData)
        {
            string sqry;
            Bitmap bm;
            DateTime FechaSalida = DateTime.Parse(TblData.Rows[0][12].ToString());
            DateTime FechaLlegada = DateTime.Parse(TblData.Rows[0][13].ToString());
            int dine = 80;//-- ancho y alto de la img resultante

            Document doc = new Document(PageSize.LETTER);
            iTextSharp.text.Font _standardFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 8, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
            string path = @"C:\ServicioPayPalOpenPay\pdf\Tickets" + TblData.Rows[0][9] + ".pdf";
            string nombre = @"Tickets" + TblData.Rows[0][9];
            // Indicamos donde vamos a guardar el documento
            PdfWriter writer = PdfWriter.GetInstance(doc,
                                        new FileStream(path, FileMode.Create));
            // Le colocamos el título y el autor
            // **Nota: Esto no será visible en el documento
            doc.AddTitle("Boletos SAG");
            doc.AddCreator("SAG Autobuses del Golfo");

            // Abrimos el archivo
            doc.Open();

            // Escribimos el encabezamiento en el documento
            doc.Add(new Paragraph("Mis Pases SAG"));
            doc.Add(Chunk.NEWLINE);

            doc.Add(new Phrase("Asegurate de imprimir este documento o llevarlo en tu smartphone, ya que deberas presentarlo con una identificación oficial y tu voucher de pago antes de abordar para nuestro viaje.", _standardFont));
            doc.Add(Chunk.NEWLINE);
            doc.Add(new Phrase("Te agradecemos que viajes con nosotros.", _standardFont));

            doc.Add(Chunk.NEWLINE);
            doc.Add(new Phrase("Origen: " + TblData.Rows[0][6].ToString() + "     Destino:  " + TblData.Rows[0][7].ToString(), _standardFont));
            doc.Add(Chunk.NEWLINE);
            doc.Add(new Phrase("Salida: " + FechaSalida.ToString("dd/MM/yyyy HH:mm") + "     Llegada:  " + FechaLlegada.ToString("dd/MM/yyyy HH:mm"), _standardFont));

            //doc.Add(pdfImage);

            // Creamos una tabla que contendrá el nombre, apellido y país
            // de nuestros visitante.
            PdfPTable tblPrueba = new PdfPTable(5);
            tblPrueba.WidthPercentage = 100;

            // Configuramos el título de las columnas de la tabla
            PdfPCell clNombre = new PdfPCell(new Phrase("Nombre Pasajero", _standardFont));
            clNombre.BorderWidth = 0;
            clNombre.BorderWidthBottom = 0.75f;

            PdfPCell clOrigen = new PdfPCell(new Phrase("Origen", _standardFont));
            clOrigen.BorderWidth = 0;
            clOrigen.BorderWidthBottom = 0.75f;

            PdfPCell clDestino = new PdfPCell(new Phrase("Destino", _standardFont));
            clDestino.BorderWidth = 0;
            clDestino.BorderWidthBottom = 0.75f;

            PdfPCell clAsiento = new PdfPCell(new Phrase("Asiento", _standardFont));
            clAsiento.BorderWidth = 0;
            clAsiento.BorderWidthBottom = 0.75f;

            PdfPCell clQrCode = new PdfPCell(new Phrase("QR", _standardFont));
            clQrCode.BorderWidth = 0;
            clQrCode.BorderWidthBottom = 0.75f;

            // Añadimos las celdas a la tabla
            tblPrueba.AddCell(clNombre);
            tblPrueba.AddCell(clOrigen);
            tblPrueba.AddCell(clDestino);
            tblPrueba.AddCell(clAsiento);
            tblPrueba.AddCell(clQrCode);

            for (int i = 0; i < TblData.Rows.Count; i++)
            {
                BarcodeQRCode datam = new BarcodeQRCode(TblData.Rows[i][3].ToString(), dine, dine, null);
                iTextSharp.text.Image pdfImage = datam.GetImage();
                // Llenamos la tabla con información
                clNombre = new PdfPCell(new Phrase(TblData.Rows[i][0].ToString(), _standardFont)); //Nombre
                clNombre.BorderWidth = 0;

                clOrigen = new PdfPCell(new Phrase(TblData.Rows[i][6].ToString(), _standardFont));//Origen
                clOrigen.BorderWidth = 0;

                clDestino = new PdfPCell(new Phrase(TblData.Rows[i][7].ToString(), _standardFont));//Destino
                clDestino.BorderWidth = 0;

                clAsiento = new PdfPCell(new Phrase(TblData.Rows[i][1].ToString(), _standardFont));//Asiento
                clAsiento.BorderWidth = 0;

                clQrCode = new PdfPCell(pdfImage);//QR
                clQrCode.BorderWidth = 0;

                // Añadimos las celdas a la tabla
                tblPrueba.AddCell(clNombre);
                tblPrueba.AddCell(clOrigen);
                tblPrueba.AddCell(clDestino);
                tblPrueba.AddCell(clAsiento);
                tblPrueba.AddCell(clQrCode);


            }

            doc.Add(tblPrueba);
            doc.Close();
            writer.Close();

            double Total = double.Parse(TblData.Rows[0][14].ToString());
            //bool v=EnvioMail.EnviaCorreo(path,nombre, TblData.Rows[0][8].ToString(),TblData.Rows[0][10].ToString(), TblData.Rows[0][6].ToString(), TblData.Rows[0][7].ToString(),FechaSalida,FechaLlegada ,Total);
            /*
            if (v)
            {
                sqry = string.Format("ActualizaEstadoEnvio '{0}',{1}", TblData.Rows[0][9].ToString(),"2");
                ExtraeDatos(sqry);
            }
            */
            return "Exito";
        }

        private static DataTable ExtraeDatos(string sqry)
        {
            DataTable tbl = new DataTable();
            string cadenaconexion = "Server = 192.168.0.245;Database = DB_TurismoOmnibus; Connection Timeout = 2; Pooling = false;User ID = sa; Password = Med*8642; Trusted_Connection = False";
            SqlConnection conexion = new SqlConnection(cadenaconexion);
            SqlCommand comando;
            SqlDataAdapter adaptador;
            if (!string.IsNullOrWhiteSpace(cadenaconexion))
            {

                comando = new SqlCommand(sqry, conexion);
                comando.CommandType = CommandType.Text;
                conexion.Open();
                comando.ExecuteNonQuery();
                conexion.Close();
                tbl = new DataTable();
                adaptador = new SqlDataAdapter(comando);
                adaptador.Fill(tbl);
            }

            return tbl;
        }
        #region DatosBoleto
        public static string DatosBoleto(string internet_sale_id)
        {
            string Origen = "", Destino = "";
            string res = "";
            int minutosSalida = 0, minutosLlegada = 0;
            DataTable tbl = new DataTable();
            bool Result = true;
            string sqry = "SELECT starting_stop_id,ending_stop_id FROM trip_seat WHERE internet_sale_id='" + internet_sale_id + "' LIMIT 1";
            tbl = mandaQry(sqry);
            foreach (DataRow row in tbl.Rows)
            {
                string sqry1 = "SELECT t1.name as Origen,t2.name as Destino FROM stop_off t1,stop_off t2 WHERE t1.id='" + row["starting_stop_id"] + "' AND t2.id='" + row["ending_stop_id"] + "' LIMIT 1";
                tbl = mandaQry(sqry1);
                foreach (DataRow row1 in tbl.Rows)
                {
                    Origen = Convert.ToString(row1["Origen"]);
                    Destino = Convert.ToString(row1["Destino"]);
                    string sqry2 = "SELECT route_id FROM stop_off WHERE id='" + row["starting_stop_id"] + "' LIMIT 1";
                    tbl = mandaQry(sqry2);
                    foreach (DataRow row2 in tbl.Rows)
                    {
                        string sqry3 = "SELECT * FROM stop_off WHERE route_id='" + row2["route_id"] + "'";
                        tbl = mandaQry(sqry3);
                        foreach (DataRow row3 in tbl.Rows)
                        {
                            minutosSalida += Convert.ToInt32(row3["travel_minutes"]);
                            minutosLlegada += Convert.ToInt32(row3["travel_minutes"]);
                            if ((Convert.ToString(row3["waiting_minutes"]) != "") || (Convert.ToString(row3["waiting_minutes"]) == "0"))
                            {
                                minutosSalida += Convert.ToInt32(row3["waiting_minutes"]);
                                minutosLlegada += Convert.ToInt32(row3["waiting_minutes"]);
                            }
                        }
                    }
                }
            }
            return res;
        }
        #endregion DatosBoleto
        #region MasterCard
        public static bool BuscarPagosMasterCard(string starting_day, string ending_day, string op)
        {
            Console.WriteLine("################----MasterCard----#######################");
            //2022-12-06 23:24:15.2540000
            string concepto = "";
            string correo_envio = "", short_id = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime hora_actual = DateTime.Now;
            hora_actual = hora_actual.AddMinutes(-30);
            DateTime time = DateTime.Now;
            //Console.WriteLine(starting_day); 
            string dia_actual = Convert.ToInt32(hora_actual.Day) < 10 ? "0" + (hora_actual.Day).ToString() : (hora_actual.Day).ToString();
            string hora_inicio = hora_actual.Year + "-" + hora_actual.Month + "-" + dia_actual + " " + hora_actual.TimeOfDay.Hours + ":" + hora_actual.TimeOfDay.Minutes + ":00";
            bool verificacion = false;
            DataTable tbl = new DataTable();
            bool Result = true;
            Console.WriteLine("Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            //string sqry = "SELECT * FROM internet_sale WHERE id='41bb5a65-4a0e-4a84-a219-e0787312a856'";
            //string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%mastercard%' AND date_created > '2022-12-07 23:37:00' ORDER BY date_created ASC";
            string sqry = "SELECT payed,id,short_id,email,total_amount, date_created,source_meta,payment_provider FROM internet_sale WHERE payment_provider LIKE '%aster%ard%' AND date_created >= now() - interval '70 minutes' AND source_meta IS NULL ORDER BY date_created ASC";
            //string sqry = "select payed,id,short_id,email,total_amount, date_created,source_meta,payment_provider from internet_sale is2 where \r\nextract (month from date_created)=extract(month from now()) and extract(year from date_created)=extract(year from now())\r\nand source_meta is null and upper(payment_provider) like upper('%aster%ard%')";
            //string sqry = "select payed,id,short_id,email,total_amount, date_created,source_meta,payment_provider \r\nfrom internet_sale \r\nwhere date_created >='2023-04-01' \r\nand payment_provider like '%aster%ard%'\r\nand source_meta is null";
            try { tbl = mandaQry(sqry); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (tbl.Rows.Count == 0)
                Result = false;
            else
            {
                foreach (DataRow row in tbl.Rows)
                {
                    if (Convert.ToString(row["payed"]) == "False")
                    {
                        DateTime creacion = Convert.ToDateTime(row["date_created"]);
                        verificacion = VerificarMasterCard(Convert.ToString(row["short_id"]));
                        if (verificacion == true)
                        {
                            try
                            {
                                GenerarBoleto(Convert.ToString(row["id"]));
                                Console.WriteLine("Se genero el ticket correctamente");
                                origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                                destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                                fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                                var temp_fechas = fechas.Split(',');
                                fecha_salida = temp_fechas[0];
                                fecha_llegada = temp_fechas[1];
                                Console.WriteLine("Exito se verifico el pago: " + short_id);
                                try
                                {
                                    EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                                    concepto = "Validado y envio de correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                }
                                catch
                                {
                                    concepto = "Error al enviar el correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                }
                            }
                            catch
                            {
                                if (op == "1")
                                {
                                    concepto = "Error al generar ticket para: " + short_id.ToString();
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Console.WriteLine("Error al generar ticket para: " + short_id);
                                }
                                EnvioMail.EnviaCorreo("luis.rojas@transportesmedrano.com", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                EnvioMail.EnviaCorreo("viridiana.fh@transportesmedrano.com", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                            }
                        }
                        else
                        {
                            string idinternetsale = row["id"].ToString();
                            DateTime limitinf = time.AddHours(5).AddMinutes(-10);
                            DateTime limitsup = time.AddHours(5).AddMinutes(1);
                            DateTime fecbolet = creacion.AddHours(1);
                            if (op == "1" && fecbolet >= limitinf && fecbolet < limitsup)
                            {
                                string sqryf = "UPDATE internet_sale SET source_meta='No se valido el boleto en 1 hr' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                mandaQry(sqryf);
                            }
                        }
                    }
                    else if (Convert.ToString(row["payed"]) == "True" && Convert.ToString(row["source_meta"]) == "")
                    {
                        origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                        destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                        fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                        if (fechas != "0")
                        {
                            var temp_fechas = fechas.Split(',');
                            fecha_salida = temp_fechas[0];
                            fecha_llegada = temp_fechas[1];
                            Console.WriteLine("Exito solo se enviara el correo");
                            EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                            concepto = "Envio de correo";
                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                            mandaQry(sqryf);
                        }
                    }
                }
            }
            Console.WriteLine("Eh terminado prrrrro");
            return true;
        }
        public static string ObtenerFecha(string short_id, string internet_sale_id, string tipo, string origen, string destino)
        {
            DateTime temp_departure, temp_ending;
            int aux_ruta = 0;
            int acum_minutes_starting = 0, acum_minutes_ending = 0;
            string fecha = "";
            string departure_date = "";
            string reverse = "";
            string descripcion = "";
            DataTable tbl = new DataTable();
            string sqry = "SELECT reverse,departure_date FROM trip t1 INNER JOIN trip_seat t2 ON t1.id=t2.trip_id WHERE t2.internet_sale_id='" + internet_sale_id + "' LIMIT 1";
            tbl = mandaQry(sqry);
            if (tbl.Rows.Count > 0)
            {
                foreach (DataRow row in tbl.Rows)
                {
                    departure_date = Convert.ToString(row["departure_date"]);
                    reverse = Convert.ToString(row["reverse"]);
                }
            }
            DataTable tbl2 = new DataTable();
            string sqry2 = "select * from stop_off where route_id like (select t2.route_id from trip_seat t1 inner join stop_off t2 on t1.starting_stop_id=t2.id inner join stop_off t3 on t1.ending_stop_id=t3.id where  t1.internet_sale_id like '" + internet_sale_id + "' LIMIT 1) order by order_index asc";
            tbl2 = mandaQry(sqry2);
            if (tbl2.Rows.Count > 0)
            {
                foreach (DataRow row in tbl2.Rows)
                {
                    if (reverse == "False")
                    {
                        if (Convert.ToString(row["name"]) == origen)
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                            aux_ruta = 1;
                        }
                        else if (aux_ruta == 1)
                        {
                            if (Convert.ToString(row["name"]) == destino)
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                aux_ruta = 0;
                                break;
                            }
                            else
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                acum_minutes_ending += Convert.ToInt32(row["waiting_minutes"]);
                            }
                        }
                        else
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                        }
                    }
                    else if (reverse == "True")
                    {
                        if (Convert.ToString(row["name"]) == destino)
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                            aux_ruta = 1;
                        }
                        else if (aux_ruta == 1)
                        {
                            if (Convert.ToString(row["name"]) == origen)
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                aux_ruta = 0;
                                break;
                            }
                            else
                            {
                                acum_minutes_ending += Convert.ToInt32(row["travel_minutes"]);
                                acum_minutes_ending += Convert.ToInt32(row["waiting_minutes"]);
                            }
                        }
                        else
                        {
                            acum_minutes_starting += Convert.ToInt32(row["travel_minutes"]);
                            acum_minutes_starting += Convert.ToInt32(row["waiting_minutes"]);
                        }
                    }
                }
                temp_departure = Convert.ToDateTime(departure_date).AddMinutes(acum_minutes_starting);
                temp_ending = Convert.ToDateTime(temp_departure).AddMinutes(acum_minutes_ending);
                temp_departure = temp_departure.AddHours(-6);
                temp_ending = temp_ending.AddHours(-6);
                fecha = temp_departure.ToString("dd-MM-yyyy HH:mm:ss") + "," + temp_ending.ToString("dd-MM-yyyy HH:mm:ss");
            }
            else
            {
                fecha = "0";
            }
            return fecha;
        }
        public static string ObtenerLugar(string shorId, string internetSaleId, string tipo)
        {
            string descripcion = "";
            if (tipo == "destino")
            {
                DataTable tbl = new DataTable();
                string sqry = "select so.name from trip_seat ts inner join stop_off so on ts.ending_stop_id = so.id where ts.internet_sale_id like '" + internetSaleId + "'";
                tbl = mandaQry(sqry);
                string destino = "";
                if (tbl.Rows.Count > 0)
                {
                    foreach (DataRow row in tbl.Rows)
                    {
                        descripcion = Convert.ToString(row["name"]);
                    }
                }
            }
            else if (tipo == "origen")
            {
                string origen = "";
                DataTable tbl = new DataTable();
                string sqry = "SELECT name FROM stop_off t1 INNER JOIN trip_seat t2 ON t1.id=t2.starting_stop_id WHERE t2.internet_sale_id='" + internetSaleId + "'";
                tbl = mandaQry(sqry);
                foreach (DataRow row in tbl.Rows)
                {
                    descripcion = Convert.ToString(row["name"]);
                }
            }
            return descripcion;
        }

        public static bool GenerarBoleto(string internet_sale_id)
        {
            bool res = true;
            DateTime fecha = DateTime.Now;
            //fecha.AddHours(-6);
            /*
            DataTable tbl1 = new DataTable();
            string sqry2 = "UPDATE detail_sale SET status_payment=true,date_updated='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' WHERE id_detail=" + id_detail;
            tbl1 = mandaQry(sqry2);*/
            int tick = 0, cont = 0;
            String GetBol = "SELECT * FROM trip_seat WHERE internet_sale_id='" + internet_sale_id + "'";
            DataTable tbl2 = new DataTable();
            tbl2 = mandaQry(GetBol);
            foreach (DataRow row3 in tbl2.Rows)
            {
                string ticketid = GenShortId();
                while (tick == 0)
                {
                    string qry21 = "SELECT * FROM trip_seat WHERE ticket_id='" + ticketid + "'";
                    DataTable tbl3 = new DataTable();
                    tbl3 = mandaQry(qry21);
                    foreach (DataRow row21 in tbl3.Rows)
                    {
                        ticketid = GenShortId();
                        cont++;
                    }
                    if (cont == 0)
                        tick = 1;
                }
                try
                {
                    string qry2 = "UPDATE trip_seat set status='OCCUPIED',ticket_id='" + ticketid + "',version=1 WHERE id='" + row3["id"] + "'";
                    mandaQry(qry2);
                }
                catch { }
                ticketid = GenShortId();
            }
            try
            {
                string qry3 = "UPDATE internet_sale set version=2,last_updated='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "',payed='true' WHERE id='" + internet_sale_id + "';";
                mandaQry(qry3);
            }
            catch { }
            return res;
        }
        public static string GenShortId()
        {
            var sh = "";
            var characters = "ab0cde1fghi2jkl3mno4pqrs5tuvw6xyz789";
            var Charsarr = new char[8];
            var random = new Random();
            for (int i = 0; i < Charsarr.Length; i++)
            {
                sh = sh + characters[random.Next(characters.Length)];
            }
            return sh;
        }
        public static bool VerificarMasterCard(string short_id)
        {
            string result = "";
            bool res = false;
            var url = "https://banamex.dialectpayments.com/api/rest/version/62/merchant/1079224/order/" + short_id;

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            httpRequest.Headers["Authorization"] = "Basic bWVyY2hhbnQuMTA3OTIyNDpjNGUyYzAwY2JjYjc5NDNmOTEyZGZmZTI1ZDFiNTc1ZQ==";
            try
            {
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    result = (result.Replace("\n", "")).ToString();
                    result = "[" + result + "]";

                }
                var rs = JsonConvert.DeserializeObject<List<ResMasterCard>>(result);
                foreach (var item in rs)
                {
                    if ((Convert.ToString(rs[0].result) == "SUCCESS") && (Convert.ToString(rs[0].status) == "CAPTURED"))
                    {
                        res = true;
                    }
                }
            }
            catch
            {
                res = false;
            }
            return res;
        }
        #endregion MasterCard
        #region Paypal
        public static bool BuscarPagosPayPal(string starting_day, string ending_day, string op)
        {
            Console.WriteLine("################----PayPal----#######################");
            //2022-12-06 23:24:15.2540000
            string correo_envio = "", short_id = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime hora_actual = DateTime.Now;
            string concepto = "";
            hora_actual = hora_actual.AddMinutes(-30);
            Console.WriteLine("Inicio: " + starting_day + "-Fin: " + ending_day + "-OP: " + op);
            //Console.WriteLine(hora_actual);
            string dia_actual = Convert.ToInt32(hora_actual.Day) < 10 ? "0" + (hora_actual.Day).ToString() : (hora_actual.Day).ToString();
            string hora_inicio = hora_actual.Year + "-" + hora_actual.Month + "-" + dia_actual + " " + hora_actual.TimeOfDay.Hours + ":" + hora_actual.TimeOfDay.Minutes + ":00";
            bool verificacion = false;
            DataTable tbl = new DataTable();
            bool Result = true;
            Console.WriteLine("Inicio: " + hora_inicio);
            //string sqry = "SELECT * FROM internet_sale WHERE id='024da958-d486-4d02-b09c-5cd379eaa8fb'";
            //string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%paypal%' AND date_created > '2022-12-07 10:36:00' ORDER BY date_created ASC";
            //string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%paypal%' AND date_created > '" + hora_inicio + "' AND source_meta IS NULL AND bill_pdf is null ORDER BY date_created ASC";
            string sqry = "SELECT * FROM internet_sale WHERE payment_provider LIKE '%ay%al%' AND date_created < '" + starting_day + "' AND date_created > '" + ending_day + "' AND source_meta IS NULL AND bill_pdf is null ORDER BY date_created ASC";
            try
            {
                tbl = mandaQry(sqry);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (tbl.Rows.Count == 0)
                Result = false;
            else
            {
                foreach (DataRow row in tbl.Rows)
                {
                    if (Convert.ToString(row["payed"]) == "False")
                    {
                        DateTime creacion = Convert.ToDateTime(row["date_created"]);
                        verificacion = VerificarPayPal(Convert.ToString(row["full_response"]), Convert.ToString(row["total_amount"]), Convert.ToString(row["short_id"]), Convert.ToString(row["id"]));
                        if (verificacion == true)
                        {
                            try
                            {
                                GenerarBoleto(Convert.ToString(row["id"]));
                                Console.WriteLine("Se genero el ticket correctamente");
                                origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                                destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                                fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                                var temp_fechas = fechas.Split(',');
                                fecha_salida = temp_fechas[0];
                                fecha_llegada = temp_fechas[1];
                                Console.WriteLine("Exito se verifico el pago: " + short_id);
                                try
                                {
                                    EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                                    concepto = "Validado y envio de correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                }
                                catch
                                {
                                    concepto = "Error al enviar el correo";
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                }
                            }
                            catch
                            {
                                if (op == "1")
                                {
                                    concepto = "Error al generar ticket para: " + short_id.ToString();
                                    string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                    mandaQry(sqryf);
                                    Console.WriteLine("Error al generar ticket para: " + short_id);
                                }
                                EnvioMail.EnviaCorreo("julio.jimenez@transportesmedrano.com", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                                EnvioMail.EnviaCorreo("ruben.salas@transportesmedrano.com", Convert.ToString(row["short_id"]), "", "", "", "", 0);
                            }
                        }
                        else
                        {
                            if (op == "1")
                            {
                                concepto = "No se valido el boleto en 1 hr";
                                string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                                mandaQry(sqryf);
                                Console.WriteLine("Error al generar ticket para: " + short_id);
                            }
                        }
                    }
                    else if (Convert.ToString(row["payed"]) == "True")
                    {
                        origen = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "origen");
                        destino = ObtenerLugar(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "destino");
                        fechas = ObtenerFecha(Convert.ToString(row["short_id"]), Convert.ToString(row["id"]), "llegada", origen, destino);
                        if (fechas != "0")
                        {
                            var temp_fechas = fechas.Split(',');
                            fecha_salida = temp_fechas[0];
                            fecha_llegada = temp_fechas[1];
                            Console.WriteLine("Exito solo se enviara el correo");
                            EnvioMail.EnviaCorreo(Convert.ToString(row["email"]), Convert.ToString(row["short_id"]), origen, destino, fecha_salida, fecha_llegada, decimal.Round(Convert.ToDecimal(row["total_amount"])));
                            concepto = "Envio de correo";
                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + Convert.ToString(row["id"]) + "'";
                            mandaQry(sqryf);
                        }
                    }
                }
            }
            Console.WriteLine("Eh terminado prrrrro");
            return true;
        }
        public static bool VerificarPayPal(string full_response, string total, string shortId, string internet_sale_id)
        {
            bool res = false;
            string responseBody = "";
            JObject response = JObject.Parse(full_response);
            if ((response != null) && (full_response.Contains("paypal")))
            {//token=9C06111807057224B
             //Access_token=A21AAOJO48ClEjqvzhmz351mNQ5C-Yg-XQrYWtyWncoPUxkKBhmdzQ1gGREfgNAmZAx5IjBHZZiYoZDFOUwmP-6sTA1rbXJZg
                string acces_token = Convert.ToString(response.Root);
                string token = "";
                var temp_url = acces_token.Split('?');
                temp_url = Convert.ToString(temp_url[1]).Split('&');
                var temp_acces_token = Convert.ToString(temp_url[0]).Split('=');
                acces_token = Convert.ToString(temp_acces_token[1]);
                var tem_token = Convert.ToString(temp_url[1]).Split('=');
                var tem_token2 = Convert.ToString(temp_url[1]).Split('\"');
                var tem_token3 = Convert.ToString(tem_token2[0]).Split('=');
                token = Convert.ToString(tem_token3[1]);
                var requestToken = (HttpWebRequest)WebRequest.Create("http://api.sagautobuses.com/Home/CapturaPaypal?id=" + acces_token + "&access_token=" + token + "&boletos=1&monto=" + total);
                requestToken.ContentType = "application/json";
                requestToken.Method = "POST";
                requestToken.ContentLength = 0;
                try
                {
                    using (WebResponse responseToken = requestToken.GetResponse())
                    {
                        using (Stream strReader = responseToken.GetResponseStream())
                        {
                            if (strReader == null) return res;
                            using (StreamReader objReader = new StreamReader(strReader))
                            {
                                responseBody = objReader.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Error al capturar pago");
                    res = false;
                }
                responseBody = ConfirmarPago(token, acces_token, total, shortId, internet_sale_id);
                Console.WriteLine(responseBody);
                if (responseBody != "")
                    res = true;
                else
                    res = false;
            }
            else
            {
                res = false;
            }
            return res;
        }
        public static string ConfirmarPago(string access_token, string token, string amount, string shortId, string internet_sale_id)
        {
            string responseBody = null;
            var requestToken = (HttpWebRequest)WebRequest.Create("http://transmed.dyndns-web.com:400/Home/RevisarPaypal?id=" + token + "&access_token=" + access_token);
            requestToken.ContentType = "application/json";
            requestToken.Method = "GET";
            requestToken.ContentLength = 0;
            try
            {
                using (WebResponse responseToken = requestToken.GetResponse())
                {
                    using (Stream strReader = responseToken.GetResponseStream())
                    {
                        if (strReader == null) return responseBody;
                        using (StreamReader objReader = new StreamReader(strReader))
                        {
                            responseBody = objReader.ReadToEnd();
                            responseBody = "[" + responseBody + "]";
                            var rs = JsonConvert.DeserializeObject<List<RevPayPal>>(responseBody);
                            if (Convert.ToString(rs[0].status) == "COMPLETED")
                            {
                                responseBody = CapturarPayPal(shortId, internet_sale_id, amount);
                            }
                        }
                    }
                }
            }
            catch
            {
                responseBody = "";
            }
            return responseBody;
        }
        public static string CapturarPayPal(string short_id, string internet_sale_id, string amount)
        {
            string responseBody = null;
            var requestToken = (HttpWebRequest)WebRequest.Create("http://api.sagautobuses.com/Home/UpdateBoletoMC?shortId=" + short_id + "&sale_id=" + internet_sale_id + "&amount=" + amount);
            requestToken.ContentType = "application/json";
            requestToken.Method = "POST";
            requestToken.ContentLength = 0;
            using (WebResponse responseToken = requestToken.GetResponse())
            {
                using (Stream strReader = responseToken.GetResponseStream())
                {
                    if (strReader == null) return responseBody;
                    using (StreamReader objReader = new StreamReader(strReader))
                    {
                        responseBody = objReader.ReadToEnd();
                    }
                }
                return responseBody;
            }
        }
        #region clases
        public class Amount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
            public Breakdown breakdown { get; set; }
        }

        public class Breakdown
        {
            public ItemTotal item_total { get; set; }
        }

        public class Item
        {
            public string name { get; set; }
            public UnitAmount unit_amount { get; set; }
            public string quantity { get; set; }
            public string description { get; set; }
        }

        public class ItemTotal
        {
            public string currency_code { get; set; }
            public string value { get; set; }
        }

        public class Link
        {
            public string href { get; set; }
            public string rel { get; set; }
            public string method { get; set; }
        }

        public class Payee
        {
            public string email_address { get; set; }
            public string merchant_id { get; set; }
        }

        public class PurchaseUnit
        {
            public string reference_id { get; set; }
            public Amount amount { get; set; }
            public Payee payee { get; set; }
            public List<Item> items { get; set; }
        }

        public class RevPayPal
        {
            public string id { get; set; }
            public string intent { get; set; }
            public string status { get; set; }
            public List<PurchaseUnit> purchase_units { get; set; }
            public DateTime create_time { get; set; }
            public List<Link> links { get; set; }
        }

        public class UnitAmount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
        }
        #endregion clases
        #region clasesPaypal
        public class Token
        {
            public string token { get; set; }
            public string access { get; set; }
        }
        #endregion clasesPaypal

        #endregion Paypal
        #region paynet
        public static bool BuscarPagosPaynet()
        {
            try
            {
                Console.WriteLine("################----PayNet----#######################");
                bool verificacion = false;
                DataTable tbl = new DataTable();
                bool Result = true;
                string sqry = "select t1.id_pago,t1.internet_sale_id,t2.short_id,t2.email,t2.total_amount from pagos_procesados t1 \r\ninner join internet_sale t2 on t1.internet_sale_id=t2.id \r\nwhere t1.id_estado = 1";
                tbl = mandaQry(sqry);
                if (tbl.Rows.Count == 0) return false;
                foreach (DataRow row in tbl.Rows)
                {
                    var id_pago = row["id_pago"].ToString();
                    var internet_sale_id = row["internet_sale_id"].ToString();
                    var short_id = row["short_id"].ToString();
                    var email = row["email"].ToString();
                    var total_amount = Convert.ToDecimal(row["total_amount"]);
                    verificacion = VerificarPaynet(id_pago, internet_sale_id, short_id, email, total_amount);
                }
                Console.WriteLine("Eh terminado perrrrrro");
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool VerificarPaynet(string idPago, string internet_sale_id, string short_id, string email, decimal total_amount)
        {
            var url = "https://api.openpay.mx/v1/maktyd38uvrqdhlfi4qz/charges/" + idPago;
            var result = "";
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            string concepto = "";
            string correo_envio = "", origen = "", destino = "", fecha_salida = "", fecha_llegada = "", fechas = "";
            DateTime fecha = DateTime.Now;

            httpRequest.Headers["Authorization"] = "Basic c2tfYjllY2RhNGZiZjhmNGVhZTlmMjIyNmQ5N2NmNjIyNzQ6UzFzdDNtQDU=";
            try
            {
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    result = (result.Replace("\n", "").ToString());
                    result = "[" + result + "]";
                }
                var rs = JsonConvert.DeserializeObject<List<openpay>>(result);
                foreach (var item in rs)
                {
                    if ((Convert.ToString(rs[0].status)).ToUpper() == "COMPLETED")
                    {
                        try
                        {
                            string sqry = "UPDATE pagos_procesados set id_estado=2,enviado_correo=1,estatus='PAGADO',fecha_enviado='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where id_pago='" + idPago + "'";
                            mandaQry(sqry);
                            GenerarBoleto(internet_sale_id);
                            Console.WriteLine("Se genero el ticket correctamente");
                            origen = ObtenerLugar(short_id, internet_sale_id, "origen");
                            destino = ObtenerLugar(short_id, internet_sale_id, "destino");
                            fechas = ObtenerFecha(short_id, internet_sale_id, "llegada", origen, destino);
                            var temp_fechas = fechas.Split(',');
                            fecha_salida = temp_fechas[0];
                            fecha_llegada = temp_fechas[1];
                            Console.WriteLine("Exito se verifico el pago: " + short_id);
                            try
                            {
                                EnvioMail.EnviaCorreo(email, short_id, origen, destino, fecha_salida, fecha_llegada, decimal.Round(total_amount));
                                concepto = "Validado y envio de correo";
                                string sqryf = "UPDATE internet_sale SET payment_provider ='paynet', source_meta='" + concepto + "' WHERE id='" + internet_sale_id + "'";
                                mandaQry(sqryf);
                            }
                            catch
                            {
                                concepto = "Error al enviar el correo";
                                string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + internet_sale_id + "'";
                                mandaQry(sqryf);
                                //string sqry = "UPDATE pagos_procesados set id_estado=2,enviado_correo=1,estatus='PAGADO',fecha_enviado='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where id_pago='" + idPago + "'";
                                //mandaQry(sqry);
                            }
                        }
                        catch
                        {
                            concepto = "Error al generar ticket para: " + short_id.ToString();
                            string sqryf = "UPDATE internet_sale SET source_meta='" + concepto + "' WHERE id='" + internet_sale_id + "'";
                            mandaQry(sqryf);
                            Console.WriteLine("Error al generar ticket para: " + short_id);
                            EnvioMail.EnviaCorreo("luis.rojas@transportesmedrano.com", short_id, "", "", "", "", 0);
                        }
                    }
                    if ((Convert.ToString(rs[0].status)).ToUpper() == "CANCELLED")
                    {
                        string sqry = "UPDATE pagos_procesados set id_estado=3,estatus='CANCELADO' ,fecha_enviado='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where id_pago='" + idPago + "'";
                        mandaQry(sqry);
                        try
                        {
                            sqry = "insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status,F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id,f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name,'Serv_Liberar_Asiento',f.trip_id,f.version,f.date_created,CURRENT_TIMESTAMP from trip_seat f where internet_sale_id = '" + internet_sale_id + "'";
                            mandaQry(sqry);
                        }
                        catch
                        {
                            Console.WriteLine("Error Mover a Cancel_reservation");
                        }
                        try
                        {
                            sqry = "DELETE FROM trip_seat WHERE internet_sale_id = '" + internet_sale_id + "';";
                            mandaQry(sqry);
                        }
                        catch
                        {
                            Console.WriteLine("Error Eliminar");
                        }
                        Console.WriteLine("Pago Cancelado Para" + short_id);
                    }
                    if ((Convert.ToString(rs[0].status)).ToUpper() == "IN_PROGRESS")
                    {
                        DataTable tbl = new DataTable();
                        DataTable tbl2 = new DataTable();
                        DateTime dateTime = DateTime.Now;
                        DateTime departure_date_corrida = DateTime.Now;
                        string sqry = $"SELECT * FROM trip_seat WHERE internet_sale_id='{internet_sale_id}'";
                        tbl = mandaQry(sqry);
                        foreach (DataRow fila in tbl.Rows)
                        {
                            string siu = fila["trip_id"].ToString();
                            sqry = $"SELECT * FROM trip WHERE id='{siu}'";
                            tbl2 = mandaQry(sqry);
                            foreach (DataRow fila2 in tbl2.Rows)
                            {
                                departure_date_corrida = DateTime.Parse(fila2["departure_date"].ToString());
                                dateTime = dateTime.AddHours(2);

                                if (dateTime >= departure_date_corrida)
                                {
                                    try
                                    {
                                        sqry = "update pagos_procesados set id_estado=3,estatus='CANCELADO',fecha_actualizacion='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where internet_sale_id like '" + internet_sale_id + "'";
                                        mandaQry(sqry);
                                    }
                                    catch { }
                                    try
                                    {
                                        sqry = "insert into cancel_reservation(id, status, internet_sale_id, seat_id, starting_stop_id,ending_stop_id, user_id, passenger_type, sold_price, payed_price,seat_name, passenger_name, comments, trip_id, version, date_created, last_updated)select F.id, F.status,F.internet_sale_id,f.seat_id,f.starting_stop_id,f.ending_stop_id,f.user_id,f.passenger_type,f.sold_price,f.payed_price,f.seat_name,f.passenger_name,'Serv_Liberar_Asiento',f.trip_id,f.version,f.date_created,CURRENT_TIMESTAMP from trip_seat f where internet_sale_id = '" + internet_sale_id + "'";
                                        mandaQry(sqry);
                                    }
                                    catch { }
                                    try
                                    {
                                        sqry = "DELETE FROM trip_seat WHERE internet_sale_id = '" + internet_sale_id + "';";
                                        mandaQry(sqry);
                                    }
                                    catch { }

                                }
                                else
                                {
                                    sqry = "update pagos_procesados set id_estado=1,estatus='PENDIENTE',fecha_actualizacion='" + (fecha.Year + "-" + fecha.Month + "-" + fecha.Day + " " + fecha.Hour + ":" + fecha.Minute + ":" + fecha.Second) + "' where internet_sale_id like '" + internet_sale_id + "'";
                                    mandaQry(sqry);
                                    Console.WriteLine("Pago Pendiente Para" + short_id);
                                }
                            }
                        }

                    }
                }
                Console.WriteLine(httpResponse.StatusCode);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static DataTable mandaQry(string sqry)
        {
            var cadenaConexion = "server = medrano-prod.ceaa4imnbtld.us-west-2.rds.amazonaws.com; port = 5432;Database=medherprod;User ID=medherdb;Password=sBMH8fvnPM;";
            NpgsqlConnection conexion = new NpgsqlConnection(cadenaConexion);
            NpgsqlCommand comando;
            NpgsqlDataAdapter adaptador;
            DataTable tbl = new DataTable();

            string Result = string.Empty;

            if (!string.IsNullOrWhiteSpace(cadenaConexion))
            {

                comando = new NpgsqlCommand(sqry, conexion);
                comando.CommandType = CommandType.Text;
                conexion.Open();
                comando.ExecuteNonQuery();//Revisar, se ejecuta de nuevo el query
                conexion.Close();
                tbl = new DataTable();
                adaptador = new NpgsqlDataAdapter(comando);
                adaptador.Fill(tbl);
                Result = "Conectado Exitoso";
            }
            return tbl;
        }
        #endregion paynet
        #region ClassMasterCard
        #region ErrorPago
        public class Error
        {
            public string cause { get; set; }
            public string explanation { get; set; }
        }

        public class ErrorPago
        {
            public Error error { get; set; }
            public string result { get; set; }
        }
        #endregion ErrorPago
        public class _3ds
        {
            public string acsEci { get; set; }
            public string authenticationToken { get; set; }
            public string transactionId { get; set; }
        }

        public class _3ds22
        {
            public string acsTransactionId { get; set; }
            public string directoryServerId { get; set; }
            public string dsTransactionId { get; set; }
            public bool methodCompleted { get; set; }
            public string methodSupported { get; set; }
            public string protocolVersion { get; set; }
            public string requestorId { get; set; }
            public string requestorName { get; set; }
            public string transactionStatus { get; set; }
        }

        public class Acquirer
        {
            public string merchantId { get; set; }
            public int? batch { get; set; }
            public string id { get; set; }
        }

        public class Authentication
        {
            public string acceptVersions { get; set; }
            public string channel { get; set; }
            public string method { get; set; }
            public string payerInteraction { get; set; }
            public string purpose { get; set; }
            public Redirect redirect { get; set; }
            public string version { get; set; }
            public string transactionId { get; set; }
        }

        public class Card
        {
            public string brand { get; set; }
            public Expiry expiry { get; set; }
            public string fundingMethod { get; set; }
            public string issuer { get; set; }
            public string nameOnCard { get; set; }
            public string number { get; set; }
            public string scheme { get; set; }
            public string storedOnFile { get; set; }
        }

        public class CardSecurityCode
        {
            public string gatewayCode { get; set; }
        }

        public class Chargeback
        {
            public int amount { get; set; }
            public string currency { get; set; }
        }

        public class Customer
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
        }

        public class Device
        {
            public string browser { get; set; }
            public string ipAddress { get; set; }
        }

        public class Expiry
        {
            public string month { get; set; }
            public string year { get; set; }
        }

        public class Order
        {
            public double amount { get; set; }
            public string authenticationStatus { get; set; }
            public Chargeback chargeback { get; set; }
            public DateTime creationTime { get; set; }
            public string currency { get; set; }
            public string id { get; set; }
            public DateTime lastUpdatedTime { get; set; }
            public double merchantAmount { get; set; }
            public string merchantCategoryCode { get; set; }
            public string merchantCurrency { get; set; }
            public string status { get; set; }
            public double totalAuthorizedAmount { get; set; }
            public double totalCapturedAmount { get; set; }
            public double totalDisbursedAmount { get; set; }
            public double totalRefundedAmount { get; set; }
            public ValueTransfer valueTransfer { get; set; }
            public string customerReference { get; set; }
            public string description { get; set; }
        }

        public class Provided
        {
            public Card card { get; set; }
        }

        public class Redirect
        {
            public string domainName { get; set; }
        }

        public class Response
        {
            public string gatewayCode { get; set; }
            public string gatewayRecommendation { get; set; }
            public string acquirerCode { get; set; }
            public CardSecurityCode cardSecurityCode { get; set; }
        }

        public class ResMasterCard
        {
            public string _3dsAcsEci { get; set; }
            public double amount { get; set; }
            public Authentication authentication { get; set; }
            public string authenticationStatus { get; set; }
            public string authenticationVersion { get; set; }
            public Chargeback chargeback { get; set; }
            public DateTime creationTime { get; set; }
            public string currency { get; set; }
            public Customer customer { get; set; }
            public string customerReference { get; set; }
            public string description { get; set; }
            public Device device { get; set; }
            public string id { get; set; }
            public DateTime lastUpdatedTime { get; set; }
            public string merchant { get; set; }
            public double merchantAmount { get; set; }
            public string merchantCategoryCode { get; set; }
            public string merchantCurrency { get; set; }
            public string result { get; set; }
            public SourceOfFunds sourceOfFunds { get; set; }
            public string status { get; set; }
            public double totalAuthorizedAmount { get; set; }
            public double totalCapturedAmount { get; set; }
            public double totalDisbursedAmount { get; set; }
            public double totalRefundedAmount { get; set; }
            public List<Transaction> transaction { get; set; }
        }

        public class SourceOfFunds
        {
            public Provided provided { get; set; }
            public string type { get; set; }
        }

        public class Transaction
        {
            public Authentication authentication { get; set; }
            public Customer customer { get; set; }
            public Device device { get; set; }
            public string merchant { get; set; }
            public Order order { get; set; }
            public Response response { get; set; }
            public string result { get; set; }
            public SourceOfFunds sourceOfFunds { get; set; }
            public DateTime timeOfLastUpdate { get; set; }
            public DateTime timeOfRecord { get; set; }
            public Transaction transaction { get; set; }
            public string version { get; set; }
            public string gatewayEntryPoint { get; set; }
            public Acquirer acquirer { get; set; }
            public double amount { get; set; }
            public string authenticationStatus { get; set; }
            public string currency { get; set; }
            public string id { get; set; }
            public string stan { get; set; }
            public string type { get; set; }
            public string authorizationCode { get; set; }
            public string receipt { get; set; }
            public string source { get; set; }
            public double? taxAmount { get; set; }
            public string terminal { get; set; }
        }

        public class ValueTransfer
        {
            public string accountType { get; set; }
        }
        #endregion ClassMasterCard
        public class Ordenes
        {
            public string id { get; set; }
            public string intent { get; set; }
            public string status { get; set; }
            public string processing_instruction { get; set; }
            public purchase_units purchase_units { get; set; }
            public payer payer { get; set; }
            public string update_time { get; set; }

            public List<links> links { get; set; }
        }
        public class purchase_units
        {
            public string reference_id { get; set; }
            public amount amount { get; set; }
            public payee payee { get; set; }
            public string description { get; set; }
            public List<items> items { get; set; }
            public shipping shipping { get; set; }
            public payments payments { get; set; }
        }
        public class amount
        {
            public string currency_code { get; set; }
            public string value { get; set; }
            public breakdown breakdown { get; set; }

        }
        public class breakdown
        {
            public item_total item_total { get; set; }
            public item_total shipping { get; set; }
            public item_total handling { get; set; }
            public item_total insurance { get; set; }
            public item_total shipping_discount { get; set; }
            public item_total discount { get; set; }
        }
    }
    public class item_total
    {
        public string currency_code { get; set; }
        public string value { get; set; }
    }
    public class payee
    {
        public string email_address { get; set; }
        public string merchant_id { get; set; }

    }
    public class items
    {
        public string name { get; set; }
        public item_total unit_amount { get; set; }
        public item_total tax { get; set; }
        public string quantity { get; set; }
        public string description { get; set; }
    }
    public class shipping
    {
        public address address { get; set; }
    }
    public class address
    {
        public string name { get; set; }

    }
    public class payments
    {
        public List<captures> captures { get; set; }
    }
    public class captures
    {
        public string id { get; set; }
        public string status { get; set; }
        public item_total amount { get; set; }
        public string final_capture { get; set; }
        public seller_protection seller_protection { get; set; }
        public seller_receivable_breakdown seller_receivable_breakdown { get; set; }
        public List<links> links { get; set; }
        public string create_time { get; set; }
        public string update_time { get; set; }
    }
    public class seller_protection
    {
        public string status { get; set; }
        public List<string> dispute_categories { get; set; }
    }
    public class seller_receivable_breakdown
    {
        public item_total gross_amount { get; set; }
        public item_total paypal_fee { get; set; }
        public item_total net_amount { get; set; }
    }
    public class links
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }

    }
    public class payer
    {
        public string email_address { get; set; }
        public string payer_id { get; set; }
        public address1 address { get; set; }
    }
    public class address1
    {
        public string country_code { get; set; }
    }
    public class openpay
    {
        public string id { get; set; }
        public string authorization { get; set; }
        public string operation_type { get; set; }
        public string transaction_type { get; set; }
        public string status { get; set; }
        public bool conciliated { get; set; }
        public DateTime creation_date { get; set; }
        public DateTime operation_date { get; set; }
        public string description { get; set; }
        public string error_message { get; set; }
        public string order_id { get; set; }
        public string customer_id { get; set; }
        public DateTime due_date { get; set; }
        public double amount { get; set; }
        public fee fee { get; set; }
        public payment_method payment_method { get; set; }
        public string currency { get; set; }
        public string method { get; set; }
    }
    public class fee
    {
        public double amount { get; set; }
        public double tax { get; set; }
        public string currency { get; set; }
    }
    public class payment_method
    {
        public string type { get; set; }
        public string reference { get; set; }
        public string barcode_url { get; set; }
    }
}
